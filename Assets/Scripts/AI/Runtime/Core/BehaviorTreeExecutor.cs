using Cysharp.Threading.Tasks;

namespace GMLM.AI
{
    /// <summary>
    /// BehaviorTree 실행을 담당하는 구체 클래스
    /// IBehaviorTreeExecutor 인터페이스의 기본 구현
    /// </summary>
    public class BehaviorTreeExecutor : IBehaviorTreeExecutor
    {
        private readonly BehaviorTree _tree;
        private bool _isRunning;

        public bool IsRunning => _isRunning;

        public BehaviorTreeExecutor(BehaviorTree tree)
        {
            _tree = tree;
        }

        public async UniTask<NodeStatus> ExecuteAsync()
        {
            if (_tree?.Root == null)
            {
                return NodeStatus.Failure;
            }

            _isRunning = true;
            try
            {
                return await _tree.Root.Execute();
            }
            finally
            {
                _isRunning = false;
            }
        }
    }
}
