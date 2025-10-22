using Cysharp.Threading.Tasks;

namespace GMLM.AI
{
    /// <summary>
    /// BehaviorTree 실행을 담당하는 인터페이스
    /// 책임 분리 원칙에 따라 실행 로직을 분리
    /// </summary>
    public interface IBehaviorTreeExecutor
    {
        /// <summary>
        /// BehaviorTree를 한 번 실행
        /// </summary>
        UniTask<NodeStatus> ExecuteAsync();
        
        /// <summary>
        /// BehaviorTree가 실행 중인지 확인
        /// </summary>
        bool IsRunning { get; }
    }
}
