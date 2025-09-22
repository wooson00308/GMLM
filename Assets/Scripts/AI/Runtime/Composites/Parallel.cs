using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace GMLM.AI
{
    public class Parallel : CompositeNode
    {
        public enum Policy
        {
            RequireOne,
            RequireAll
        }

        private readonly Policy _successPolicy;

        public Parallel(Policy successPolicy, List<Node> children) : base(children)
        {
            _successPolicy = successPolicy;
        }

        public override async UniTask<NodeStatus> Execute()
        {
            var tasks = Children.Select(child => child.Execute()).ToList();
            var results = await UniTask.WhenAll(tasks);
            var statuses = results.ToList();

            if (_successPolicy == Policy.RequireOne && statuses.Any(s => s == NodeStatus.Success))
            {
                return NodeStatus.Success;
            }

            if (_successPolicy == Policy.RequireAll && statuses.All(s => s == NodeStatus.Success))
            {
                return NodeStatus.Success;
            }

            if (statuses.Any(s => s == NodeStatus.Running))
            {
                return NodeStatus.Running;
            }

            return NodeStatus.Failure;
        }
    }
} 