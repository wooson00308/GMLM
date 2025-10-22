namespace GMLM.AI
{
    public abstract class ActionNode : Node
    {
        protected readonly INodeContext Context;

        protected ActionNode(IBlackboard blackboard)
        {
            Context = new NodeContext(blackboard);
        }

        protected ActionNode(INodeContext context)
        {
            Context = context;
        }

        protected IBlackboard Blackboard => Context.Blackboard;
    }
} 