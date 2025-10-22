using System.Collections.Generic;
using GMLM.AI;

namespace GMLM.Game
{
    /// <summary>
    /// Behavior Tree 구성용 빌더 패턴 클래스
    /// new List<Node> 반복을 제거하고 가독성을 향상시킵니다.
    /// </summary>
    public static class BehaviorTreeBuilder
    {
        public static SequenceBuilder Sequence()
        {
            return new SequenceBuilder();
        }

        public static SelectorBuilder Selector()
        {
            return new SelectorBuilder();
        }

        public static ParallelBuilder Parallel(Parallel.Policy policy)
        {
            return new ParallelBuilder(policy);
        }

        public static UtilitySelectorBuilder UtilitySelector()
        {
            return new UtilitySelectorBuilder();
        }
    }

    /// <summary>
    /// CompositeNode 빌더들의 공통 베이스 클래스
    /// Template Method Pattern을 적용하여 중복 코드를 제거합니다.
    /// </summary>
    public abstract class CompositeBuilder<T> where T : CompositeBuilder<T>
    {
        protected readonly List<Node> _children = new List<Node>();

        /// <summary>
        /// Template method: 공통 Add 로직
        /// </summary>
        public T Add(Node node)
        {
            if (node != null)
            {
                _children.Add(node);
            }
            return (T)this;
        }

        /// <summary>
        /// Template method: 다중 노드 추가
        /// </summary>
        public T Add(params Node[] nodes)
        {
            foreach (var node in nodes)
            {
                Add(node);
            }
            return (T)this;
        }

        /// <summary>
        /// Hook method: 자식 클래스가 구현
        /// </summary>
        public abstract CompositeNode Build();
    }

    /// <summary>
    /// Sequence 노드 빌더
    /// </summary>
    public class SequenceBuilder : CompositeBuilder<SequenceBuilder>
    {
        public override CompositeNode Build()
        {
            return new Sequence(_children);
        }
    }

    /// <summary>
    /// Selector 노드 빌더
    /// </summary>
    public class SelectorBuilder : CompositeBuilder<SelectorBuilder>
    {
        public override CompositeNode Build()
        {
            return new Selector(_children);
        }
    }

    /// <summary>
    /// Parallel 노드 빌더
    /// </summary>
    public class ParallelBuilder : CompositeBuilder<ParallelBuilder>
    {
        private readonly Parallel.Policy _policy;

        public ParallelBuilder(Parallel.Policy policy)
        {
            _policy = policy;
        }

        public override CompositeNode Build()
        {
            return new Parallel(_policy, _children);
        }
    }

    /// <summary>
    /// UtilitySelector 노드 빌더
    /// UtilitySelector는 List<UtilityScorer>를 사용하므로 별도 구현
    /// </summary>
    public class UtilitySelectorBuilder
    {
        private readonly List<UtilityScorer> _children = new List<UtilityScorer>();

        public UtilitySelectorBuilder Add(UtilityScorer scorer)
        {
            if (scorer != null)
            {
                _children.Add(scorer);
            }
            return this;
        }

        public UtilitySelectorBuilder Add(params UtilityScorer[] scorers)
        {
            foreach (var scorer in scorers)
            {
                Add(scorer);
            }
            return this;
        }

        public UtilitySelector Build()
        {
            return new UtilitySelector(_children);
        }
    }
}