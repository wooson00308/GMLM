using Cysharp.Threading.Tasks;
using UnityEngine;
using GMLM.AI;

namespace GMLM.Game
{
    public class MoveToTargetAction : ActionNode
    {
        private readonly string _selfKey;
        private readonly string _targetKey;
        private readonly float _stopDistance;

        public MoveToTargetAction(IBlackboard blackboard, string selfKey = "self", string targetKey = "target", float stopDistance = 1.5f)
            : base(blackboard)
        {
            _selfKey = selfKey;
            _targetKey = targetKey;
            _stopDistance = Mathf.Max(0f, stopDistance);
        }

        public override UniTask<NodeStatus> Execute()
        {
            var self = Blackboard.GetTransform(_selfKey);
            var target = Blackboard.GetGameObject(_targetKey);
            if (self == null || target == null)
            {
                return new UniTask<NodeStatus>(NodeStatus.Failure);
            }

            // XY 평면에서 거리 체크만 수행, 실제 이동은 Mecha가 담당
            Vector3 selfPos = self.position; selfPos.z = 0f;
            Vector3 tgtPos = target.transform.position; tgtPos.z = 0f;
            float distance = Vector3.Distance(selfPos, tgtPos);
            // 무기 사거리 유지: 가장 긴 무기 사거리(없으면 기본 정지 거리) 기준
            float stop = _stopDistance;
            var mecha = self.GetComponent<Mecha>();
            if (mecha != null)
            {
                float longest = 0f;
                var weapons = mecha.WeaponsAll;
                if (weapons != null)
                {
                    foreach (var w in weapons)
                    {
                        if (w == null) continue;
                        if (w.AttackRange > longest) longest = w.AttackRange;
                    }
                }
                if (longest > 0f) stop = Mathf.Max(stop, longest);
            }
            float maintainEps = Mathf.Max(0.1f, stop * 0.05f); // 약간의 히스테리시스 마진
            if (distance <= stop - maintainEps)
            {
                return new UniTask<NodeStatus>(NodeStatus.Success);
            }

            mecha = self.GetComponent<Mecha>();
            if (mecha == null)
            {
                return new UniTask<NodeStatus>(NodeStatus.Failure);
            }
            mecha.MoveTowards(tgtPos);
            return new UniTask<NodeStatus>(NodeStatus.Running);
        }
    }
}


