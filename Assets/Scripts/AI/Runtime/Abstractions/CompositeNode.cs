using System.Collections.Generic;
using System.Linq;

namespace GMLM.AI
{
    public abstract class CompositeNode : Node
    {
        protected readonly INodeContext Context;

        protected CompositeNode(List<Node> children)
        {
            Context = new NodeContext(null, children);
        }

        protected CompositeNode(INodeContext context)
        {
            Context = context;
        }

        protected List<Node> Children => Context.Children.ToList();
    }
} 