using System.Collections.Generic;

namespace GMLM.AI
{
    /// <summary>
    /// 점수 집계 전략을 정의하는 인터페이스
    /// 전략 패턴을 통해 다양한 집계 방식을 지원
    /// </summary>
    public interface IScoreAggregationStrategy
    {
        /// <summary>
        /// 여러 점수를 하나의 최종 점수로 집계
        /// </summary>
        /// <param name="scores">집계할 점수들</param>
        /// <returns>집계된 최종 점수 (0.0 ~ 1.0)</returns>
        float Aggregate(IEnumerable<float> scores);
    }
}
