using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace GMLM.AI
{
    public class Selector : CompositeNode
    {
        public Selector(List<Node> children) : base(children) { }

        public override async UniTask<NodeStatus> Execute()
        {
            foreach (var child in Children)
            {
                var status = await child.Execute();
                if (status != NodeStatus.Failure)
                {
                    return status;
                }
            }
            return NodeStatus.Failure;
        }
    }
} 