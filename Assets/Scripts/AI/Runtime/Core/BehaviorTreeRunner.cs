using UnityEngine;
using Cysharp.Threading.Tasks;

namespace GMLM.AI
{
    public abstract class BehaviorTreeRunner : MonoBehaviour
    {
        protected BehaviorTree Tree;

        private void Start()
        {
            var blackboard = new Blackboard();
            Tree = new BehaviorTree(InitializeTree(blackboard), blackboard);
        }

        private void Update()
        {
            Tree?.Tick();
        }

        /// <summary>
        /// 개선된 비동기 실행 메서드
        /// 예외 처리가 가능한 UniTask 반환
        /// </summary>
        protected async UniTask TickAsync()
        {
            if (Tree != null)
            {
                await Tree.TickAsync();
            }
        }

        protected abstract Node InitializeTree(IBlackboard blackboard);
    }
} 