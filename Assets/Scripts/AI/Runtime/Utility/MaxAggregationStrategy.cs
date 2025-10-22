using System.Collections.Generic;
using System.Linq;

namespace GMLM.AI
{
    /// <summary>
    /// 최대값 집계 전략
    /// 모든 점수 중 최대값을 반환
    /// </summary>
    public class MaxAggregationStrategy : IScoreAggregationStrategy
    {
        public float Aggregate(IEnumerable<float> scores)
        {
            var scoreList = scores.ToList();
            return scoreList.Count == 0 ? 0f : scoreList.Max();
        }
    }
}
