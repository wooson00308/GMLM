using Cysharp.Threading.Tasks;
using UnityEngine;
using GMLM.AI;

namespace GMLM.Game
{
    public class EvadeToggleAction : ActionNode
    {
        private readonly string _selfKey;
        private readonly MaintainRangeStrafeAction _strafeAction;
        // Hysteresis thresholds
        private readonly float _ttiEnter;
        private readonly float _ttiExit;
        // Cooldown to prevent rapid re-trigger
        private readonly float _retriggerCooldown;
        private float _cooldownTimer = 0f;
        private bool _isThreatActive = false;

        public EvadeToggleAction(IBlackboard blackboard, string selfKey, MaintainRangeStrafeAction strafeAction, float ttiThreshold = 0.35f)
            : base(blackboard)
        {
            _selfKey = selfKey;
            _strafeAction = strafeAction;
            _ttiEnter = Mathf.Max(0.05f, ttiThreshold);
            // default exit is a bit larger than enter to create hysteresis window
            _ttiExit = Mathf.Max(_ttiEnter + 0.2f, _ttiEnter + 0.2f);
            _retriggerCooldown = 0.6f;
        }

        // Advanced constructor allowing explicit exit threshold and cooldown (kept distinct arity to avoid overload ambiguity)
        public EvadeToggleAction(IBlackboard blackboard, string selfKey, MaintainRangeStrafeAction strafeAction, float ttiEnter, float ttiExit, float retriggerCooldown, float reserved)
            : base(blackboard)
        {
            _selfKey = selfKey;
            _strafeAction = strafeAction;
            _ttiEnter = Mathf.Max(0.05f, ttiEnter);
            _ttiExit = Mathf.Max(_ttiEnter + 0.05f, ttiExit);
            _retriggerCooldown = Mathf.Max(0f, retriggerCooldown);
        }

        public override UniTask<NodeStatus> Execute()
        {
            var self = Blackboard.GetTransform(_selfKey);
            if (self == null || _strafeAction == null)
            {
                return new UniTask<NodeStatus>(NodeStatus.Failure);
            }

            var sensor = self.GetComponent<MechaProjectileSensor>();
            if (sensor == null)
            {
                return new UniTask<NodeStatus>(NodeStatus.Failure);
            }

			// update cooldown
			if (_cooldownTimer > 0f)
			{
				_cooldownTimer -= Time.deltaTime;
				if (_cooldownTimer < 0f) _cooldownTimer = 0f;
			}

			// Predict desired movement direction from strafe action
			Vector2 desiredDir;
			bool hasDesired = _strafeAction.TryPredictDesiredDirection(out desiredDir);
			float moveSpeed = 0f;
			var mecha = self.GetComponent<Mecha>();
			if (mecha != null) moveSpeed = mecha.MoveSpeed;

			bool willCross = false;
			float crossTime = 0f;
			if (hasDesired && sensor.IncomingVelocity.sqrMagnitude > 0f)
			{
				// Use path-crossing within horizon with clearance
				float horizon = Mathf.Max(0.1f, _ttiExit);
				float clearance = 0.6f; // mecha+projectile 합 반경에 대한 러프 마진
				float minDist;
				willCross = sensor.WillPathCross(desiredDir, moveSpeed, horizon, clearance, out crossTime, out minDist);
			}

			if (!_isThreatActive)
			{
				if (willCross && crossTime < _ttiEnter)
				{
					if (_cooldownTimer <= 0f)
					{
						_strafeAction.TriggerEvadeToggle();
						_cooldownTimer = _retriggerCooldown;
					}
					_isThreatActive = true;
				}
			}
			else
			{
				if (!willCross || crossTime > _ttiExit)
				{
					_isThreatActive = false;
				}
			}

			return new UniTask<NodeStatus>(NodeStatus.Running);
        }
    }
}


