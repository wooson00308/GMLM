namespace GMLM.AI
{
    /// <summary>
    /// AI 행동의 가치를 판단하는 기준을 정의하는 인터페이스
    /// Utility AI 시스템의 핵심 컴포넌트
    /// </summary>
    public interface IConsideration
    {
        /// <summary>
        /// 현재 상황에 대한 점수를 반환 (0.0 ~ 1.0)
        /// ResponseCurve가 적용된 최종 점수
        /// </summary>
        float GetScore();
    }
}
