namespace GMLM.AI
{
    public class BehaviorTree
    {
        public Node Root { get; set; }
        public IBlackboard Blackboard { get; }

        public BehaviorTree(Node root, IBlackboard blackboard)
        {
            Root = root;
            Blackboard = blackboard;
        }

        public async void Tick()
        {
            if (Root != null)
            {
                await Root.Execute();
            }
        }
    }
} 