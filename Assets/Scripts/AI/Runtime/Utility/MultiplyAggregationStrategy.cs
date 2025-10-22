using System.Collections.Generic;
using System.Linq;

namespace GMLM.AI
{
    /// <summary>
    /// 곱셈 집계 전략
    /// 모든 점수를 곱한 값을 반환
    /// </summary>
    public class MultiplyAggregationStrategy : IScoreAggregationStrategy
    {
        public float Aggregate(IEnumerable<float> scores)
        {
            var scoreList = scores.ToList();
            return scoreList.Count == 0 ? 0f : scoreList.Aggregate(1f, (acc, score) => acc * score);
        }
    }
}
