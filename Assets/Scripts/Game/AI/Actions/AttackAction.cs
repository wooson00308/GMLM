using Cysharp.Threading.Tasks;
using UnityEngine;
using GMLM.AI;

namespace GMLM.Game
{
    public class AttackAction : ActionNode
    {
        private readonly string _selfKey;
        private readonly string _targetKey;
        private readonly float _maxConsideredRange; // 액션 진입 허용 최대 사거리 (0이면 무기 사거리 사용)
        private float _cooldownTimer;

        public AttackAction(IBlackboard blackboard, string selfKey = "self", string targetKey = "target", float maxConsideredRange = 10f)
            : base(blackboard)
        {
            _selfKey = selfKey;
            _targetKey = targetKey;
            _maxConsideredRange = Mathf.Max(0f, maxConsideredRange);
        }

        public override UniTask<NodeStatus> Execute()
        {
            var selfTr = Blackboard.GetTransform(_selfKey);
            var targetGo = Blackboard.GetGameObject(_targetKey);
            if (selfTr == null || targetGo == null)
            {
                return new UniTask<NodeStatus>(NodeStatus.Failure);
            }

            var selfMecha = selfTr.GetComponent<Mecha>();
            var targetMecha = targetGo.GetComponent<Mecha>();
            if (selfMecha == null || targetMecha == null || !targetMecha.IsAlive)
            {
                return new UniTask<NodeStatus>(NodeStatus.Failure);
            }

            float distance = Vector3.Distance(selfTr.position, targetGo.transform.position);
            float maxRange = _maxConsideredRange;
            if (maxRange <= 0f)
            {
                // 무기가 여러 개면 가장 긴 사거리 사용
                float longest = 0f;
                var weapons = selfMecha.WeaponsAll;
                if (weapons != null)
                {
                    foreach (var w in weapons)
                    {
                        if (w == null) continue;
                        if (w.AttackRange > longest) longest = w.AttackRange;
                    }
                }
                maxRange = longest;
            }
            // 약간의 허용 오차를 둬서 궤도 상태에서도 공격이 끊기지 않게 함
            float rangeTolerance = 0.25f;
            if (maxRange > 0f && distance > (maxRange + rangeTolerance))
            {
                return new UniTask<NodeStatus>(NodeStatus.Failure);
            }

            // 예측 조준: 타겟의 이동을 고려한 조준
            Vector3 selfPos = selfTr.position; selfPos.z = 0f;
            Vector3 tgtPos = targetGo.transform.position; tgtPos.z = 0f;
            Vector3 desiredAim = ComputePredictiveAim(selfPos, tgtPos, selfMecha, targetMecha);

            if (desiredAim.sqrMagnitude > 1e-6f)
            {
                desiredAim.Normalize();
                // 예측 벡터를 직접 사용하여 조준 (방향 벡터를 위치로 변환하지 않음)
                selfMecha.FaceTowards(selfPos + desiredAim * 2f);
            }

            // 병렬 사격: 사용 가능한 모든 무기 시도 (무기는 머즐.right으로 발사)
            var all = selfMecha.WeaponsAll;
            if (all != null && all.Count > 0)
            {
                foreach (var w in all)
                {
                    if (w == null) continue;
                    w.TryAttack(selfMecha, targetMecha);
                }
            }
            
            // 사거리 안에서는 쿨다운 동안에도 액션을 Running으로 유지해 병렬 동작을 지속
            return new UniTask<NodeStatus>(NodeStatus.Running);
        }

        // [예측 조준 유틸] 고급 AI/플레이어 수동 조준용 - 향후 옵션으로 활성화 가능
        private Vector3 ComputePredictiveAim(Vector3 selfPos, Vector3 targetPos, Mecha selfMecha, Mecha targetMecha)
        {
            // 기본값: 현재 위치 조준
            Vector3 desiredAim = (targetPos - selfPos);

            // 예측 조준은 "비유도 탄" 기준으로만 판단한다.
            // 혼합 장비(유도+비유도)에서도 비유도 무기를 위해 항상 예측을 시도해야 한다.
            var weaponsRO = selfMecha.WeaponsAll;
            float bestNonHomingSpeed = 0f;
            if (weaponsRO != null)
            {
                for (int i = 0; i < weaponsRO.Count; i++)
                {
                    var w = weaponsRO[i];
                    if (w == null) continue;
                    if (w.IsProjectileHoming) continue; // 유도탄은 자체 추적, 예측 불필요
                    bestNonHomingSpeed = Mathf.Max(bestNonHomingSpeed, w.ProjectileSpeed);
                }
            }

            if (bestNonHomingSpeed > 0.01f)
            {
                Vector2 aimDir;
                float tHit;
                bool solved = PredictionUtils.TryFirstOrderIntercept(
                    (Vector2)selfPos,
                    bestNonHomingSpeed,
                    (Vector2)targetPos,
                    targetMecha.WorldVelocity2D,
                    out aimDir,
                    out tHit
                );
                if (solved && aimDir.sqrMagnitude > 1e-6f)
                {
                    desiredAim = new Vector3(aimDir.x, aimDir.y, 0f);
                }
            }
            return desiredAim;
        }
    }
}


