using UnityEngine;

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

        protected abstract Node InitializeTree(IBlackboard blackboard);
    }
} 