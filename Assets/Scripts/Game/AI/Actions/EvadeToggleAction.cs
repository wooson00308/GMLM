using Cysharp.Threading.Tasks;
using UnityEngine;
using GMLM.AI;

namespace GMLM.Game
{
    public class EvadeToggleAction : ActionNode
    {
        private readonly string _selfKey;
        private readonly MaintainRangeStrafeAction _strafeAction;
        private readonly float _ttiThreshold;

        public EvadeToggleAction(IBlackboard blackboard, string selfKey, MaintainRangeStrafeAction strafeAction, float ttiThreshold = 0.35f)
            : base(blackboard)
        {
            _selfKey = selfKey;
            _strafeAction = strafeAction;
            _ttiThreshold = Mathf.Max(0.05f, ttiThreshold);
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

            if (sensor.IncomingTTI < _ttiThreshold)
            {
                _strafeAction.TriggerEvadeToggle();
            }

            return new UniTask<NodeStatus>(NodeStatus.Running);
        }
    }
}


