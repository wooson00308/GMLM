using Cysharp.Threading.Tasks;

namespace GMLM.AI
{
    public abstract class Node
    {
        public abstract UniTask<NodeStatus> Execute();
    }
} 