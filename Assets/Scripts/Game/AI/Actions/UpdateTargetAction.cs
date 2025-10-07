using Cysharp.Threading.Tasks;
using UnityEngine;
using GMLM.AI;

namespace GMLM.Game
{
    /// <summary>
    /// Blackboard 키:
    /// - "self": Transform (필수)
    /// - "target": GameObject (출력)
    /// - "teamId": int (선택, 없으면 Mecha 컴포넌트에서 추출)
    /// </summary>
    public class UpdateTargetAction : ActionNode
    {
        private readonly string _selfKey;
        private readonly string _targetKey;
        private readonly string _teamKey;
        private readonly float _hysteresisDistance;

        public UpdateTargetAction(
            IBlackboard blackboard,
            string selfKey = "self",
            string targetKey = "target",
            string teamKey = "teamId",
            float hysteresisDistance = 2.0f
        ) : base(blackboard)
        {
            _selfKey = selfKey;
            _targetKey = targetKey;
            _teamKey = teamKey;
            _hysteresisDistance = Mathf.Max(0f, hysteresisDistance);
        }

        public override UniTask<NodeStatus> Execute()
        {
            var selfTransform = Blackboard.GetTransform(_selfKey);
            if (selfTransform == null)
            {
                return new UniTask<NodeStatus>(NodeStatus.Failure);
            }

            int teamId = Blackboard.GetValue<int>(_teamKey);
            if (teamId == 0)
            {
                var mecha = selfTransform.GetComponent<Mecha>();
                if (mecha == null)
                {
                    return new UniTask<NodeStatus>(NodeStatus.Failure);
                }
                teamId = mecha.TeamId;
                Blackboard.SetValue(_teamKey, teamId);
            }

            var currentTarget = Blackboard.GetGameObject(_targetKey);
            // 필터: 죽은 타겟은 즉시 해제
            if (currentTarget != null)
            {
                var ctMecha = currentTarget.GetComponent<Mecha>();
                if (ctMecha == null || !ctMecha.IsAlive)
                {
                    Blackboard.SetGameObject(_targetKey, null);
                    currentTarget = null;
                }
            }
            var best = MechaRegistry.FindClosestEnemy(selfTransform.position, teamId);

            if (best == null)
            {
                if (currentTarget != null)
                {
                    Blackboard.SetGameObject(_targetKey, null);
                }
                return new UniTask<NodeStatus>(NodeStatus.Success);
            }

            // 히스테리시스: 현재 타겟이 있고, 새 후보가 충분히 더 가깝지 않다면 유지
            if (currentTarget != null)
            {
                float currentD2 = (currentTarget.transform.position - selfTransform.position).sqrMagnitude;
                float bestD2 = (best.transform.position - selfTransform.position).sqrMagnitude;
                float delta = _hysteresisDistance * _hysteresisDistance;
                if (bestD2 + delta >= currentD2)
                {
                    return new UniTask<NodeStatus>(NodeStatus.Success);
                }
            }

            Blackboard.SetGameObject(_targetKey, best.gameObject);
            return new UniTask<NodeStatus>(NodeStatus.Success);
        }
    }
}


