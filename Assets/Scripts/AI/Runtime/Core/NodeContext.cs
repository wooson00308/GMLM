using System.Collections.Generic;
using System.Linq;

namespace GMLM.AI
{
    /// <summary>
    /// INodeContext의 기본 구현체
    /// 노드들이 공통으로 사용하는 컨텍스트 정보를 관리
    /// </summary>
    public class NodeContext : INodeContext
    {
        public IBlackboard Blackboard { get; }
        public IReadOnlyList<Node> Children { get; }
        public Node Child { get; }
        public string NodeId { get; }

        public NodeContext(IBlackboard blackboard, string nodeId = null)
        {
            Blackboard = blackboard;
            NodeId = nodeId ?? GetType().Name;
            Children = new List<Node>();
            Child = null;
        }

        public NodeContext(IBlackboard blackboard, List<Node> children, string nodeId = null)
        {
            Blackboard = blackboard;
            NodeId = nodeId ?? GetType().Name;
            Children = children?.AsReadOnly() ?? new List<Node>().AsReadOnly();
            Child = children?.FirstOrDefault();
        }

        public NodeContext(IBlackboard blackboard, Node child, string nodeId = null)
        {
            Blackboard = blackboard;
            NodeId = nodeId ?? GetType().Name;
            Children = child != null ? new List<Node> { child }.AsReadOnly() : new List<Node>().AsReadOnly();
            Child = child;
        }
    }
}
