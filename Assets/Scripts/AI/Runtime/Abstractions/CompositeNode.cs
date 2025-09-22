using System.Collections.Generic;

namespace GMLM.AI
{
    public abstract class CompositeNode : Node
    {
        protected readonly List<Node> Children;

        protected CompositeNode(List<Node> children)
        {
            Children = children;
        }
    }
} 