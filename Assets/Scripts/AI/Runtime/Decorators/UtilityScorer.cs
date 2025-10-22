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
    
    public class UtilityScorer : DecoratorNode, IUtilityScorer
    {
        public string Name { get; }
        public List<Consideration> Considerations { get; }
        private readonly ScoreAggregationType _aggregationType;
        private readonly IScoreAggregationStrategy _aggregationStrategy;

        // 기존 API 호환성을 위한 생성자
        public UtilityScorer(Node child, List<Consideration> considerations, ScoreAggregationType aggregationType = ScoreAggregationType.Average, string name = "") : base(child)
        {
            Name = string.IsNullOrEmpty(name) ? child.GetType().Name : name;
            Considerations = considerations;
            _aggregationType = aggregationType;
            _aggregationStrategy = CreateStrategyFromType(aggregationType);
        }

        // 새로운 전략 패턴 기반 생성자
        public UtilityScorer(Node child, List<Consideration> considerations, IScoreAggregationStrategy aggregationStrategy, string name = "") : base(child)
        {
            Name = string.IsNullOrEmpty(name) ? child.GetType().Name : name;
            Considerations = considerations;
            _aggregationType = ScoreAggregationType.Average; // 기본값
            _aggregationStrategy = aggregationStrategy;
        }

        public float GetScore()
        {
            if (Considerations == null || Considerations.Count == 0)
            {
                return 0f;
            }

            var scores = Considerations.Select(c => c.GetScore()).ToList();
            return _aggregationStrategy.Aggregate(scores);
        }

        private IScoreAggregationStrategy CreateStrategyFromType(ScoreAggregationType type)
        {
            switch (type)
            {
                case ScoreAggregationType.Average:
                    return new AverageAggregationStrategy();
                case ScoreAggregationType.Min:
                    return new MinAggregationStrategy();
                case ScoreAggregationType.Max:
                    return new MaxAggregationStrategy();
                case ScoreAggregationType.Multiply:
                    return new MultiplyAggregationStrategy();
                default:
                    return new AverageAggregationStrategy();
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

        // IUtilityScorer 인터페이스 구현
        IReadOnlyList<IConsideration> IUtilityScorer.Considerations => Considerations?.AsReadOnly();
        
        System.Threading.Tasks.Task<NodeStatus> IUtilityScorer.Execute()
        {
            return Execute().AsTask();
        }
    }
} 