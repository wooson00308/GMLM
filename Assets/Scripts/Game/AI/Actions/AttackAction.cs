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

            // 병렬 사격: 사용 가능한 모든 무기 시도
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
    }
}


