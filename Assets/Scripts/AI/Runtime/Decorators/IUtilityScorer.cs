using System.Collections.Generic;

namespace GMLM.AI
{
    /// <summary>
    /// AI 행동의 가치를 계산하고 실행하는 인터페이스
    /// Utility AI 시스템에서 행동 선택의 핵심 컴포넌트
    /// </summary>
    public interface IUtilityScorer
    {
        /// <summary>
        /// 현재 상황에서의 행동 점수 (0.0 ~ 1.0)
        /// </summary>
        float GetScore();
        
        /// <summary>
        /// 행동 실행
        /// </summary>
        System.Threading.Tasks.Task<NodeStatus> Execute();
        
        /// <summary>
        /// 스코어 이름 (디버깅 및 로깅용)
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// 고려사항 목록
        /// </summary>
        IReadOnlyList<IConsideration> Considerations { get; }
    }
}
