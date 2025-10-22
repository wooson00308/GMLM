using System.Collections.Generic;
using UnityEngine;

namespace GMLM.AI
{
    /// <summary>
    /// 노드가 실행에 필요한 컨텍스트 정보를 제공하는 인터페이스
    /// 컴포지션 기반 노드 구조의 핵심 컴포넌트
    /// </summary>
    public interface INodeContext
    {
        /// <summary>
        /// 노드가 접근할 수 있는 Blackboard 인스턴스
        /// </summary>
        IBlackboard Blackboard { get; }
        
        /// <summary>
        /// 자식 노드들 (CompositeNode, DecoratorNode에서 사용)
        /// </summary>
        IReadOnlyList<Node> Children { get; }
        
        /// <summary>
        /// 단일 자식 노드 (DecoratorNode에서 사용)
        /// </summary>
        Node Child { get; }
        
        /// <summary>
        /// 노드의 고유 식별자 (디버깅 및 로깅용)
        /// </summary>
        string NodeId { get; }
    }
}
