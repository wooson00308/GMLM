using Cysharp.Threading.Tasks;

namespace GMLM.AI
{
    public class BehaviorTree
    {
        public Node Root { get; set; }
        public IBlackboard Blackboard { get; }
        private readonly IBehaviorTreeExecutor _executor;

        public BehaviorTree(Node root, IBlackboard blackboard)
        {
            Root = root;
            Blackboard = blackboard;
            _executor = new BehaviorTreeExecutor(this);
        }

        /// <summary>
        /// 기존 API 호환성을 위한 메서드
        /// 내부적으로 TickAsync()를 호출
        /// </summary>
        public async void Tick()
        {
            await TickAsync();
        }

        /// <summary>
        /// 개선된 비동기 실행 메서드
        /// 예외 처리가 가능한 UniTask 반환
        /// </summary>
        public async UniTask TickAsync()
        {
            await _executor.ExecuteAsync();
        }

        /// <summary>
        /// 실행 중인지 확인
        /// </summary>
        public bool IsRunning => _executor.IsRunning;
    }
} 