using Cysharp.Threading.Tasks;
using UnityEngine;
using GMLM.AI;

namespace GMLM.Game
{
    public class EvadeDashAction : ActionNode
    {
        private readonly string _selfKey;
        private readonly float _ttiThreshold;
        private readonly MaintainRangeStrafeAction _strafeAction;

        public EvadeDashAction(IBlackboard blackboard, string selfKey = "self", float ttiThreshold = 0.25f, MaintainRangeStrafeAction strafeAction = null)
            : base(blackboard)
        {
            _selfKey = selfKey;
            _ttiThreshold = Mathf.Max(0.05f, ttiThreshold);
            _strafeAction = strafeAction;
        }

        public override UniTask<NodeStatus> Execute()
        {
            var self = Blackboard.GetTransform(_selfKey);
            if (self == null) return new UniTask<NodeStatus>(NodeStatus.Failure);

            var mecha = self.GetComponent<Mecha>();
            var sensor = self.GetComponent<MechaProjectileSensor>();
            if (mecha == null || sensor == null) return new UniTask<NodeStatus>(NodeStatus.Failure);

            // 고위협 투사체만 대시 발동 (바주카/미사일 등)
            // 일반 투사체는 스트레이프로 자연 회피
            
            // 호밍 투사체는 더 빠른 반응 필요 (곡선 추적으로 회피 시간이 짧음)
            float effectiveTTI = sensor.IncomingIsHoming ? _ttiThreshold * 1.5f : _ttiThreshold;
            
            if (sensor.IncomingIsHighThreat && sensor.IncomingTTI < effectiveTTI && mecha.CanDash())
            {
                // 투사체 진행방향에 수직으로 회피
                Vector2 d = sensor.IncomingDir;
                if (d.sqrMagnitude > 0f)
                {
                    // 스마트한 회피 방향 계산
                    Vector3 smartEvadeDir = CalculateSmartEvadeDirection(self.position, mecha, sensor, d);
                    
                    bool dashSuccess = mecha.TryDash(smartEvadeDir);
                    
                    // 대시 성공 시 스트레이프 방향 업데이트
                    if (dashSuccess && _strafeAction != null)
                    {
                        _strafeAction.UpdateDirectionFromDash(smartEvadeDir);
                    }
                }
                return new UniTask<NodeStatus>(NodeStatus.Running);
            }

            return new UniTask<NodeStatus>(NodeStatus.Failure);
        }

        /// <summary>
        /// 투사체 발사원점, 타겟 위치, 예측 안전성 등을 고려하여 최적의 회피 방향을 계산한다.
        /// </summary>
        private Vector3 CalculateSmartEvadeDirection(Vector3 selfPos, Mecha mecha, MechaProjectileSensor sensor, Vector2 incomingDir)
        {
            // 기본 수직 방향 두 개
            Vector2 perpRight = new Vector2(-incomingDir.y, incomingDir.x);
            Vector2 perpLeft = -perpRight;
            
            // 실제 메카의 대시 파라미터 사용
            float dashDistance = mecha.DashDistance;
            float dashSpeed = mecha.DashSpeed;
            float dashDuration = dashDistance / dashSpeed; // 대시 완료까지 걸리는 시간
            
            // 0순위: 예측 기반 안전성 검증 (대시 완료 시점에서 충분히 안전한가?)
            float rightSafety = CalculateFutureSafety((Vector2)selfPos, perpRight, dashDistance, dashDuration, sensor);
            float leftSafety = CalculateFutureSafety((Vector2)selfPos, perpLeft, dashDistance, dashDuration, sensor);
            
            // 호밍 투사체는 더 민감한 안전성 판단 (안전 마진 더 크게)
            float safetyThreshold = sensor.IncomingIsHoming ? 0.8f : 1.5f;
            
            // 안전성 차이가 크면 더 안전한 쪽 선택
            if (Mathf.Abs(rightSafety - leftSafety) > safetyThreshold)
            {
                return (rightSafety > leftSafety) ? (Vector3)perpRight : (Vector3)perpLeft;
            }
            
            // 1순위: 호밍 vs 직선 투사체에 따른 회피 전략 구분
            Vector2 escapeReference;
            float escapeThreshold;
            
            if (sensor.IncomingIsHoming)
            {
                // 호밍 투사체: 현재 위치에서 멀어지는 것이 더 중요
                escapeReference = ((Vector2)selfPos - sensor.IncomingOrigin).normalized;
                escapeThreshold = 0.2f; // 더 민감한 반응
            }
            else
            {
                // 직선 투사체: 발사원점에서 멀어지는 방향 선호
                escapeReference = ((Vector2)selfPos - sensor.IncomingOrigin).normalized;
                escapeThreshold = 0.3f;
            }
            
            float rightFromReference = Vector2.Dot(perpRight, escapeReference);
            float leftFromReference = Vector2.Dot(perpLeft, escapeReference);
            
            // 명확한 차이가 있으면 기준점에서 멀어지는 방향 선택
            if (Mathf.Abs(rightFromReference - leftFromReference) > escapeThreshold)
            {
                return (rightFromReference > leftFromReference) ? (Vector3)perpRight : (Vector3)perpLeft;
            }
            
            // 2순위: 타겟과의 적정 거리 유지 고려 (호밍 투사체일 때는 생존 우선으로 가중치 감소)
            var target = Blackboard.GetGameObject("target");
            if (target != null && !sensor.IncomingIsHoming) // 호밍 투사체일 때는 거리 고려 생략
            {
                Vector2 rightEndPos = (Vector2)selfPos + perpRight * dashDistance;
                Vector2 leftEndPos = (Vector2)selfPos + perpLeft * dashDistance;
                
                float rightDistToTarget = Vector2.Distance(rightEndPos, (Vector2)target.transform.position);
                float leftDistToTarget = Vector2.Distance(leftEndPos, (Vector2)target.transform.position);
                
                // 적정 사거리 - 메카의 실제 무기 사거리 사용
                float optimalRange = GetOptimalWeaponRange(mecha);
                float rightRangeError = Mathf.Abs(rightDistToTarget - optimalRange);
                float leftRangeError = Mathf.Abs(leftDistToTarget - optimalRange);
                
                // 사거리 유지에 더 유리한 쪽 선택
                if (Mathf.Abs(rightRangeError - leftRangeError) > 1f)
                {
                    return (rightRangeError < leftRangeError) ? (Vector3)perpRight : (Vector3)perpLeft;
                }
            }
            
            // 3순위: 현재 스트레이프 방향과의 일관성 고려 (기존 움직임 패턴 유지)
            if (_strafeAction != null)
            {
                Vector2 currentDesiredDir;
                if (_strafeAction.TryPredictDesiredDirection(out currentDesiredDir))
                {
                    float rightConsistency = Vector2.Dot(perpRight, currentDesiredDir);
                    float leftConsistency = Vector2.Dot(perpLeft, currentDesiredDir);
                    
                    if (Mathf.Abs(rightConsistency - leftConsistency) > 0.2f)
                    {
                        return (rightConsistency > leftConsistency) ? (Vector3)perpRight : (Vector3)perpLeft;
                    }
                }
            }
            
            // 마지막: 기본 선택 (안전 우선, 호밍 투사체는 더 공격적으로)
            if (sensor.IncomingIsHoming)
            {
                // 호밍 투사체: 투사체 현재 위치에서 최대한 멀어지는 방향
                Vector2 currentThreatDir = ((Vector2)selfPos - sensor.IncomingOrigin).normalized;
                float rightFromThreat = Vector2.Dot(perpRight, currentThreatDir);
                float leftFromThreat = Vector2.Dot(perpLeft, currentThreatDir);
                return (rightFromThreat > leftFromThreat) ? (Vector3)perpRight : (Vector3)perpLeft;
            }
            else
            {
                // 직선 투사체: 기존 로직 유지
                return (rightFromReference > leftFromReference) ? (Vector3)perpRight : (Vector3)perpLeft;
            }
        }
        
        /// <summary>
        /// 특정 방향으로 대시했을 때 대시 완료 시점에서의 안전성을 계산한다.
        /// 호밍 투사체의 곡선 추적과 직선 투사체의 예측을 모두 고려한다.
        /// </summary>
        private float CalculateFutureSafety(Vector2 startPos, Vector2 dashDirection, float dashDistance, float dashDuration, MechaProjectileSensor sensor)
        {
            // 대시 완료 시점에서의 자신의 위치
            Vector2 futureSelfPos = startPos + dashDirection.normalized * dashDistance;
            
            // 투사체의 실제 속도 벡터 사용 (크기와 방향 모두 반영)
            Vector2 projectileVelocity = sensor.IncomingVelocity;
            float projectileSpeed = projectileVelocity.magnitude;
            
            // 투사체 속도가 0에 가까우면 정적 위험으로 간주
            if (projectileSpeed < 0.1f)
            {
                return Vector2.Distance(futureSelfPos, sensor.IncomingOrigin);
            }
            
            float safetyDistance;
            
            // 호밍 투사체와 직선 투사체 구분 처리
            if (sensor.IncomingIsHoming)
            {
                safetyDistance = CalculateHomingSafety(startPos, futureSelfPos, dashDuration, sensor, projectileSpeed);
            }
            else
            {
                safetyDistance = CalculateLinearSafety(futureSelfPos, dashDuration, sensor, projectileVelocity, projectileSpeed);
            }
            
            return safetyDistance;
        }
        
        /// <summary>
        /// 호밍 투사체에 대한 안전성 계산 - 곡선 추적을 고려한 보수적 접근
        /// </summary>
        private float CalculateHomingSafety(Vector2 startPos, Vector2 futureSelfPos, float dashDuration, MechaProjectileSensor sensor, float projectileSpeed)
        {
            Vector2 currentProjectilePos = sensor.IncomingOrigin;
            
            // 호밍 투사체는 대시하는 동안 계속 플레이어를 추적하므로 
            // 단순 직선 예측 대신 "최악의 경우"를 가정한 보수적 계산
            
            // 1. 현재 투사체와 대시 완료 지점 간의 거리
            float finalDistance = Vector2.Distance(futureSelfPos, currentProjectilePos);
            
            // 2. 대시 동안 투사체가 이동할 수 있는 최대 거리
            float projectileMaxTravel = projectileSpeed * dashDuration;
            
            // 3. 호밍 효과 고려: 투사체가 플레이어를 향해 방향을 바꿀 수 있음
            // 대시 중간 지점에서의 거리도 고려 (곡선 추적 시뮬레이션)
            Vector2 midDashPos = startPos + (futureSelfPos - startPos) * 0.5f;
            float midDistance = Vector2.Distance(midDashPos, currentProjectilePos);
            
            // 최소 안전 거리 = 투사체가 중간 지점까지 도달할 수 있는지 확인
            float minSafeDistance = Mathf.Min(finalDistance, midDistance);
            
            // 4. 호밍 투사체 보정: 더 보수적으로 계산 (안전 마진 증가)
            float homingPenalty = 0.7f; // 호밍으로 인한 30% 안전도 감소
            minSafeDistance *= homingPenalty;
            
            // 5. 속도 차이 고려: 대시가 투사체보다 빠르면 도망칠 수 있음
            var self = Blackboard.GetTransform(_selfKey);
            var mecha = self.GetComponent<Mecha>();
            float dashSpeed = mecha.DashSpeed;
            if (dashSpeed > projectileSpeed)
            {
                float speedAdvantage = (dashSpeed - projectileSpeed) / dashSpeed;
                minSafeDistance *= (1f + speedAdvantage * 0.5f); // 속도 우위 보너스
            }
            
            // 6. 투사체가 이미 너무 가깝다면 패널티
            float currentThreatDistance = Vector2.Distance(startPos, currentProjectilePos);
            if (currentThreatDistance < 2f) // 2m 이내는 위험
            {
                minSafeDistance *= 0.5f;
            }
            
            return minSafeDistance;
        }
        
        /// <summary>
        /// 직선 투사체에 대한 안전성 계산 - 기존 로직 유지
        /// </summary>
        private float CalculateLinearSafety(Vector2 futureSelfPos, float dashDuration, MechaProjectileSensor sensor, Vector2 projectileVelocity, float projectileSpeed)
        {
            // 대시 완료 시점에서의 투사체 위치 예측 (직선 이동)
            Vector2 futureProjectilePos = sensor.IncomingOrigin + projectileVelocity * dashDuration;
            
            // 투사체와의 최종 거리 = 기본 안전성 지표
            float safetyDistance = Vector2.Distance(futureSelfPos, futureProjectilePos);
            
            // 추가 안전성 평가
            Vector2 projDirection = projectileVelocity.normalized;
            Vector2 futureRelativePos = futureSelfPos - futureProjectilePos;
            
            // 1. 투사체 뒤쪽에 있으면 보너스 안전도
            float behindProjectile = Vector2.Dot(futureRelativePos, -projDirection);
            if (behindProjectile > 0f)
            {
                safetyDistance += behindProjectile * 2f;
            }
            
            // 2. 투사체 경로에서 수직 거리가 멀수록 안전
            float perpendicularDistance = Mathf.Abs(Vector2.Dot(futureRelativePos, new Vector2(-projDirection.y, projDirection.x)));
            safetyDistance += perpendicularDistance * 0.5f;
            
            // 3. 빠른 투사체일수록 회피 어려움 보정
            float speedFactor = Mathf.Clamp01(projectileSpeed / 30f); // 30m/s를 기준으로 정규화
            safetyDistance *= (2f - speedFactor); // 빠를수록 안전도 감소
            
            return safetyDistance;
        }
        
        /// <summary>
        /// 메카의 무기 중 가장 긴 사거리를 반환한다. (적정 교전 거리로 사용)
        /// </summary>
        private float GetOptimalWeaponRange(Mecha mecha)
        {
            float longestRange = 8f; // 기본값
            var weapons = mecha.WeaponsAll;
            
            if (weapons != null && weapons.Count > 0)
            {
                float maxRange = 0f;
                foreach (var weapon in weapons)
                {
                    if (weapon != null && weapon.AttackRange > maxRange)
                    {
                        maxRange = weapon.AttackRange;
                    }
                }
                
                if (maxRange > 0f)
                {
                    longestRange = maxRange;
                }
            }
            
            return longestRange;
        }
    }
}


