using System.Collections.Generic;
using System.Linq;

namespace GMLM.AI
{
    /// <summary>
    /// 평균 집계 전략
    /// 모든 점수의 평균을 계산
    /// </summary>
    public class AverageAggregationStrategy : IScoreAggregationStrategy
    {
        public float Aggregate(IEnumerable<float> scores)
        {
            var scoreList = scores.ToList();
            return scoreList.Count == 0 ? 0f : scoreList.Average();
        }
    }
}
