namespace GMLM.AI
{
    public abstract class ActionNode : Node
    {
        protected readonly IBlackboard Blackboard;

        protected ActionNode(IBlackboard blackboard)
        {
            Blackboard = blackboard;
        }
    }
} 