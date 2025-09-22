using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace GMLM.AI
{
    public class UtilitySelector : CompositeNode
    {
        public UtilitySelector(List<UtilityScorer> children) : base(children.Cast<Node>().ToList()) { }

        public override async UniTask<NodeStatus> Execute()
        {
            var scoredChildren = Children
                .Cast<UtilityScorer>()
                .Select(child => new { Score = child.GetScore(), Node = child })
                .OrderByDescending(x => x.Score)
                .ToList();

            if (scoredChildren.Count == 0)
            {
                return NodeStatus.Failure;
            }

            // 가장 점수가 높은 노드를 실행
            var bestChoice = scoredChildren.First();
            
            // 디버깅을 위해 선택된 노드와 점수를 로그로 남기는 것도 좋은 방법
            // Debug.Log($"UtilitySelector chose {bestChoice.Node.Child.GetType().Name} with score: {bestChoice.Score}");

            return await bestChoice.Node.Execute();
        }
    }
} 