using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace GMLM.AI
{
    public class Sequence : CompositeNode
    {
        public Sequence(List<Node> children) : base(children) { }

        public override async UniTask<NodeStatus> Execute()
        {
            foreach (var child in Children)
            {
                var status = await child.Execute();
                if (status != NodeStatus.Success)
                {
                    return status;
                }
            }
            return NodeStatus.Success;
        }
    }
} 