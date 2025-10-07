using Cysharp.Threading.Tasks;
using GMLM.AI;

namespace GMLM.Game
{
    public class IdleAction : ActionNode
    {
        public IdleAction(IBlackboard blackboard) : base(blackboard) { }

        public override UniTask<NodeStatus> Execute()
        {
            // 간단 프로토타입: 즉시 성공 반환 (다음 프레임에 재평가)
            return new UniTask<NodeStatus>(NodeStatus.Success);
        }
    }
}


