namespace GMLM.AI
{
    public abstract class DecoratorNode : Node
    {
        protected readonly Node Child;

        protected DecoratorNode(Node child)
        {
            Child = child;
        }
    }
} 