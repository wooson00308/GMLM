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
            var combatStyle = mecha.Pilot?.CombatStyle ?? CombatStyle.Ranged;
            var mTarget = target.GetComponent<Mecha>();
            var sortedWeapons = mecha.GetWeaponsSortedByCombatStyle(combatStyle, mTarget);

            // 성향별 사거리 선택 로직
            if (sortedWeapons != null && sortedWeapons.Count > 0)
            {
                Weapon firstRangeKeepingWeapon = null;
                bool foundValidWeapon = false;
                
                // 발사 가능한 사거리 유지 무기 중 첫 번째 선택
                foreach (var w in sortedWeapons)
                {
                    if (w == null) continue;
                    if (!w.IsRangeKeeping) continue; // 사거리 유지 대상 아님 (로켓런처 등)
                    
                    // 첫 번째 사거리 유지 무기 기록 (장전 중이라도)
                    if (firstRangeKeepingWeapon == null && w.AttackRange > 0f)
                    {
                        firstRangeKeepingWeapon = w;
                    }
                    
                    // 장전 중이 아닌 무기 발견 시 즉시 사용
                    if (!w.IsReloading && w.AttackRange > 0f)
                    {
                        desiredRange = w.AttackRange;
                        foundValidWeapon = true;
                        break; // 첫 번째 발사 가능한 사거리 유지 무기 발견
                    }
                }
                
                // 모든 무기가 장전 중일 때: 첫 번째 사거리 유지 무기 사거리 사용
                // sortedWeapons는 이미 성향별로 정렬되어 있음 (Melee=오름차순, Ranged=내림차순)
                if (!foundValidWeapon && firstRangeKeepingWeapon != null)
                {
                    desiredRange = firstRangeKeepingWeapon.AttackRange;
                }
            }

            Vector3 selfPos = self.position; selfPos.z = 0f;
            Vector3 tgtPos = target.transform.position; tgtPos.z = 0f;
            Vector3 toTarget = (tgtPos - selfPos);
            float dist = toTarget.magnitude;
            if (dist <= Mathf.Epsilon)
            {
                return new UniTask<NodeStatus>(NodeStatus.Running);
            }
            
            
            // 거리 좁히기: 어썰트 부스트 우선, 불가능하면 퀵 대시
            if (dist > desiredRange)
            {
                float gap = dist - desiredRange;
                
                // 1. 어썰트 부스트 활성화 조건: 중거리~원거리 간격 (1.5m 이상)
                if (gap > 1.5f)
                {
                    if (!mecha.IsAssaultBoosting && mecha.CanAssaultBoost())
                    {
                        mecha.TryStartAssaultBoost();
                    }
                }
                else
                {
                    // 2. 적정 거리 도달 시 어썰트 해제
                    if (mecha.IsAssaultBoosting)
                    {
                        mecha.StopAssaultBoost();
                    }
                }
                
                // 3. 퀵 대시는 어썰트 부스트가 불가능할 때만 사용 (매우 긴 거리)
                if (gap > mecha.DashDistance * 0.5f && mecha.CanDash() && !mecha.IsAssaultBoosting && !mecha.CanAssaultBoost())
                {
                    var sensor = self.GetComponent<MechaProjectileSensor>();
                    Vector3 dashDir = DashUtils.CalculateApproachDirection(
                        selfPos, tgtPos, mecha, sensor, Blackboard);
                    
                    mecha.TryDash(dashDir);
                }
            }
            else
            {
                // 4. 사거리 안쪽이면 어썰트 해제
                if (mecha.IsAssaultBoosting)
                {
                    mecha.StopAssaultBoost();
                }
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
            
            Vector3 desired;
            // 기본 궤도 비중(_orbitWeight)을 반영해 균형 조정 (오차 클수록 방사성↑)
            float wTangent = Mathf.Lerp(1f, 1f - _orbitWeight, wRadial);
            desired = (tangentN * wTangent) + (radialDir * wRadial);
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

            // 주기적으로 방향 전환해 원 형태 유지 (비활성화)
            
            _toggleTimer += Time.deltaTime;
			_sinceLastEvade += Time.deltaTime;
            if (_toggleTimer > _toggleWaitTime)
            {
                ToggleDirection();
            }

            return new UniTask<NodeStatus>(NodeStatus.Running);
        }

        /// <summary>
        /// 대시 방향에 따라 스트레이프 방향 업데이트
        /// 대시 방향 ≈ 현재 방향 → 유지
        /// 대시 방향 ≠ 현재 방향 → 반전
        /// </summary>
        public void UpdateDirectionFromDash(Vector3 dashDirection)
        {
            var target = Blackboard.GetGameObject(_targetKey);
            var self = Blackboard.GetTransform(_selfKey);
            if (target == null || self == null) return;
            
            // 타겟으로의 방향 벡터
            Vector3 toTarget = (target.transform.position - self.position);
            toTarget.z = 0f;
            if (toTarget.sqrMagnitude <= Mathf.Epsilon) return;
            toTarget.Normalize();
            
            // 오른쪽 접선 방향 (시계방향 기준)
            Vector3 rightTangent = new Vector3(-toTarget.y, toTarget.x, 0f);
            
            // 대시 방향과 오른쪽 접선의 내적
            Vector3 dashDir = dashDirection;
            dashDir.z = 0f;
            if (dashDir.sqrMagnitude <= Mathf.Epsilon) return;
            dashDir.Normalize();
            
            float dot = Vector3.Dot(dashDir, rightTangent);
            
            // 임계값(0.1f)으로 미세한 각도 차이 무시
            // dot > 0 → 오른쪽 대시, dot < 0 → 왼쪽 대시
            if (dot > 0.1f && _strafeSign < 0f)
            {
                // 오른쪽으로 대시했는데 현재 왼쪽 선회 중 → 오른쪽으로 전환
                _strafeSign = 1f;
            }
            else if (dot < -0.1f && _strafeSign > 0f)
            {
                // 왼쪽으로 대시했는데 현재 오른쪽 선회 중 → 왼쪽으로 전환
                _strafeSign = -1f;
            }
            // else: 같은 방향이면 유지
        }

        public void ToggleDirection()
        {
            _strafeSign *= -1f;
            _toggleTimer = 0f;
            _toggleWaitTime = Random.Range(_minToggleTime, _maxToggleTime);
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
			// 파일럿 성향에 따른 사거리 유지 무기 사거리 사용
			var mecha = self.GetComponent<Mecha>();
			if (mecha != null)
			{
				var combatStyle = mecha.Pilot?.CombatStyle ?? CombatStyle.Ranged;
				var targetGameObject = Blackboard.GetGameObject(_targetKey);
				var mTarget = targetGameObject != null ? targetGameObject.GetComponent<Mecha>() : null;
				var sortedWeapons = mecha.GetWeaponsSortedByCombatStyle(combatStyle, mTarget);
				if (sortedWeapons != null && sortedWeapons.Count > 0)
				{
					Weapon firstRangeKeepingWeapon = null;
					
					// 발사 가능한 사거리 유지 무기 중 첫 번째 선택
					bool foundValidWeapon = false;
					foreach (var w in sortedWeapons)
					{
						if (w == null) continue;
						if (!w.IsRangeKeeping) continue; // 사거리 유지 대상 아님
						
						// 첫 번째 사거리 유지 무기 기록 (장전 중이라도)
						if (firstRangeKeepingWeapon == null && w.AttackRange > 0f)
						{
							firstRangeKeepingWeapon = w;
						}
						
						// 장전 중이 아닌 무기 발견 시 즉시 사용
						if (!w.IsReloading && w.AttackRange > 0f)
						{
							desiredRange = w.AttackRange;
							foundValidWeapon = true;
							break; // 첫 번째 발사 가능한 사거리 유지 무기 발견
						}
					}
					
					// 모든 무기가 장전 중일 때: 첫 번째 사거리 유지 무기 사거리 사용
					// sortedWeapons는 이미 성향별로 정렬되어 있음 (Melee=오름차순, Ranged=내림차순)
					if (!foundValidWeapon && firstRangeKeepingWeapon != null)
					{
						desiredRange = firstRangeKeepingWeapon.AttackRange;
					}
				}
			}
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


