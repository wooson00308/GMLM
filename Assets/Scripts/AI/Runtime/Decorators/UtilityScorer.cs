using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace GMLM.AI
{
    public enum ScoreAggregationType
    {
        Average,
        Min,
        Max,
        Multiply
    }
    
    public class UtilityScorer : DecoratorNode
    {
        public string Name { get; }
        public List<Consideration> Considerations { get; }
        private readonly ScoreAggregationType _aggregationType;

        public UtilityScorer(Node child, List<Consideration> considerations, ScoreAggregationType aggregationType = ScoreAggregationType.Average, string name = "") : base(child)
        {
            Name = string.IsNullOrEmpty(name) ? child.GetType().Name : name;
            Considerations = considerations;
            _aggregationType = aggregationType;
        }

        public float GetScore()
        {
            if (Considerations == null || Considerations.Count == 0)
            {
                return 0f;
            }

            var scores = Considerations.Select(c => c.GetScore()).ToList();

            switch (_aggregationType)
            {
                case ScoreAggregationType.Average:
                    return scores.Average();
                case ScoreAggregationType.Min:
                    return scores.Min();
                case ScoreAggregationType.Max:
                    return scores.Max();
                case ScoreAggregationType.Multiply:
                    return scores.Aggregate(1f, (acc, score) => acc * score);
                default:
                    return 0f;
            }
        }

        public void AddConsideration(Consideration consideration)
        {
            Considerations.Add(consideration);
        }

        public override UniTask<NodeStatus> Execute()
        {
            return Child.Execute();
        }
    }
} 