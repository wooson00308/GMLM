using Cysharp.Threading.Tasks;
using UnityEngine;
using GMLM.AI;

namespace GMLM.Game
{
    public class MaintainRangeStrafeAction : ActionNode
    {
        private readonly string _selfKey;
        private readonly string _targetKey;
        private readonly float _orbitWeight; // 접선 비중
        private readonly float _margin;      // 사거리 유지 마진
        private float _strafeSign = 1f;
        private float _toggleTimer = 0f;
        private float _toggleWaitTime = 0f;
        private float _minToggleTime = 1f;
        private float _maxToggleTime = 2.5f;
        private bool _forceEvadeToggle = false;
		private float _sinceLastEvade = 0f;
		private float _evadeMinInterval = 0.4f;

        public MaintainRangeStrafeAction(IBlackboard blackboard, string selfKey = "self", string targetKey = "target", 
            float orbitWeight = 0.8f, float margin = 0.5f, 
            float minToggleTime = 1f, float maxToggleTime = 2.5f)
            : base(blackboard)
        {
            _selfKey = selfKey;
            _targetKey = targetKey;
            _orbitWeight = Mathf.Clamp01(orbitWeight);
            _margin = Mathf.Max(0f, margin);
            _minToggleTime = minToggleTime;
            _maxToggleTime = maxToggleTime;
            _toggleWaitTime = Random.Range(_minToggleTime, _maxToggleTime);
			_sinceLastEvade = 999f;
        }

        // 외부에서 회피 토글 트리거
		public void TriggerEvadeToggle()
        {
			// Gate external toggles by minimum interval to avoid thrashing
			if (_sinceLastEvade >= _evadeMinInterval)
			{
				_forceEvadeToggle = true;
			}
        }

        public override UniTask<NodeStatus> Execute()
        {
            var self = Blackboard.GetTransform(_selfKey);
            var target = Blackboard.GetGameObject(_targetKey);
            if (self == null || target == null)
            {
                return new UniTask<NodeStatus>(NodeStatus.Failure);
            }
            var mecha = self.GetComponent<Mecha>();
            if (mecha == null) return new UniTask<NodeStatus>(NodeStatus.Failure);

            float desiredRange = 1.5f;
            var weapons = mecha.WeaponsAll;
            if (weapons != null)
            {
                float longest = 0f;
                foreach (var w in weapons)
                {
                    if (w == null) continue;
                    if (w.AttackRange > longest) longest = w.AttackRange;
                }
                if (longest > 0f) desiredRange = longest;
            }

            Vector3 selfPos = self.position; selfPos.z = 0f;
            Vector3 tgtPos = target.transform.position; tgtPos.z = 0f;
            Vector3 toTarget = (tgtPos - selfPos);
            float dist = toTarget.magnitude;
            if (dist <= Mathf.Epsilon)
            {
                return new UniTask<NodeStatus>(NodeStatus.Running);
            }
            Vector3 dirToTarget = toTarget / dist;
            // 접선(좌/우)
            Vector3 tangent = new Vector3(-dirToTarget.y, dirToTarget.x, 0f) * _strafeSign;
            Vector3 tangentN = tangent.sqrMagnitude > 0f ? tangent.normalized : tangent;

            // 안쪽 과민 반응 완화: 안쪽 데드존 설정(사거리의 10% 또는 margin 중 큰 값)
            float inside = Mathf.Max(0f, desiredRange - dist);
            float insideDeadzone = Mathf.Max(_margin, desiredRange * 0.10f);

            Vector3 radialDir;
            float radialErr;
            if (dist >= desiredRange)
            {
                // 사거리 밖: 타겟 쪽으로 접근
                radialDir = dirToTarget;
                radialErr = dist - desiredRange;
            }
            else
            {
                // 사거리 안: 충분히 가까워졌을 때만 이탈, 데드존 내에서는 0 처리
                radialDir = -dirToTarget;
                radialErr = (inside > insideDeadzone) ? (inside - insideDeadzone) : 0f;
            }

            float wRadial = Mathf.Clamp01(radialErr / Mathf.Max(0.001f, _margin));
            // 기본 궤도 비중(_orbitWeight)을 반영해 균형 조정 (오차 클수록 방사성↑)
            float wTangent = Mathf.Lerp(1f, 1f - _orbitWeight, wRadial);
            Vector3 desired = (tangentN * wTangent) + (radialDir * wRadial);
            if (desired.sqrMagnitude <= 1e-6f)
            {
                desired = tangentN;
            }

            desired = desired.normalized;

            // 기본적으로 궤도 이동은 대치 상태이므로 시선은 적으로 고정
			mecha.MoveInDirection(desired, false);
            mecha.FaceTowards(tgtPos);

			// 회피 토글 즉시 반영 (with min interval gate)
            if (_forceEvadeToggle)
            {
                _forceEvadeToggle = false;
				_strafeSign *= -1f;
                _toggleTimer = 0f;
                _toggleWaitTime = Random.Range(_minToggleTime, _maxToggleTime);
				_sinceLastEvade = 0f;
            }

            // 주기적으로 방향 전환해 원 형태 유지
            _toggleTimer += Time.deltaTime;
			_sinceLastEvade += Time.deltaTime;
            if (_toggleTimer > _toggleWaitTime)
            {
                _toggleTimer = 0f;
                _strafeSign *= -1f;

                _toggleWaitTime = Random.Range(_minToggleTime, _maxToggleTime);
            }

            return new UniTask<NodeStatus>(NodeStatus.Running);
        }

		// 외부에서 경로 예측에 사용할 현재 프레임의 기대 이동 방향을 노출
		public bool TryPredictDesiredDirection(out Vector2 desiredDirection)
		{
			desiredDirection = Vector2.zero;
			var self = Blackboard.GetTransform(_selfKey);
			var targetGo = Blackboard.GetGameObject(_targetKey);
			if (self == null || targetGo == null) return false;
			Vector3 selfPos = self.position; selfPos.z = 0f;
			Vector3 tgtPos = targetGo.transform.position; tgtPos.z = 0f;
			Vector3 toTarget = (tgtPos - selfPos);
			float dist = toTarget.magnitude;
			if (dist <= Mathf.Epsilon) return false;
			Vector3 dirToTarget = toTarget / dist;
			Vector3 tangent = new Vector3(-dirToTarget.y, dirToTarget.x, 0f) * _strafeSign;
			Vector3 tangentN = tangent.sqrMagnitude > 0f ? tangent.normalized : tangent;
			// inside/outside blend 동일 계산 재사용
			float desiredRange = 1.5f;
			// margin 기반 간단화
			float inside = Mathf.Max(0f, desiredRange - dist);
			float insideDeadzone = Mathf.Max(_margin, desiredRange * 0.10f);
			Vector3 radialDir;
			float radialErr;
			if (dist >= desiredRange)
			{
				radialDir = dirToTarget;
				radialErr = dist - desiredRange;
			}
			else
			{
				radialDir = -dirToTarget;
				radialErr = (inside > insideDeadzone) ? (inside - insideDeadzone) : 0f;
			}
			float wRadial = Mathf.Clamp01(radialErr / Mathf.Max(0.001f, _margin));
			float wTangent = Mathf.Lerp(1f, 1f - _orbitWeight, wRadial);
			Vector3 desired = (tangentN * wTangent) + (radialDir * wRadial);
			if (desired.sqrMagnitude <= 1e-6f) desired = tangentN;
			desired = desired.normalized;
			desiredDirection = desired;
			return true;
		}
    }
}


