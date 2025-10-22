namespace GMLM.AI
{
    public abstract class DecoratorNode : Node
    {
        protected readonly INodeContext Context;

        protected DecoratorNode(Node child)
        {
            Context = new NodeContext(null, child);
        }

        protected DecoratorNode(INodeContext context)
        {
            Context = context;
        }

        protected Node Child => Context.Child;
    }
} 