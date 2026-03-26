using Unity.GraphToolkit.Editor;

namespace GraphToolkitTutorials.BehaviorTree
{
    /// <summary>
    /// 序列节点 — 按顺序执行所有子节点，直到一个失败。
    /// 如果所有子节点成功，返回成功；如果任何一个失败，返回失败。
    /// </summary>
    [Node("Composite", "")]
    [UseWithGraph(typeof(BehaviorTreeGraph))]
    [System.Serializable]
    internal class SequenceNode : CompositeNode
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddParentPort(context);
            AddChildrenPort(context);
        }

        public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
        {
            var children = GetChildren(graph);
            var childIndices = new int[children.Count];
            for (int i = 0; i < children.Count; i++)
                childIndices[i] = children[i].GetNodeIndex(graph);

            return new Runtime.SequenceNode { childIndices = childIndices };
        }
    }

    /// <summary>
    /// 选择节点 — 按顺序执行子节点，直到一个成功。
    /// 如果任何一个子节点成功，返回成功；如果所有失败，返回失败。
    /// </summary>
    [Node("Composite", "")]
    [UseWithGraph(typeof(BehaviorTreeGraph))]
    [System.Serializable]
    internal class SelectorNode : CompositeNode
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddParentPort(context);
            AddChildrenPort(context);
        }

        public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
        {
            var children = GetChildren(graph);
            var childIndices = new int[children.Count];
            for (int i = 0; i < children.Count; i++)
                childIndices[i] = children[i].GetNodeIndex(graph);

            return new Runtime.SelectorNode { childIndices = childIndices };
        }
    }

    /// <summary>
    /// 并行节点 — 同时执行所有子节点。
    /// SuccessPolicy 控制何时视为整体成功。
    /// </summary>
    [Node("Composite", "")]
    [UseWithGraph(typeof(BehaviorTreeGraph))]
    [System.Serializable]
    internal class ParallelNode : CompositeNode
    {
        public enum SuccessPolicy
        {
            RequireAll,    // 所有子节点成功才成功
            RequireOne     // 任意一个子节点成功就成功
        }

        private INodeOption m_SuccessPolicyOption;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddParentPort(context);
            AddChildrenPort(context);
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_SuccessPolicyOption = context.AddOption<SuccessPolicy>("Success Policy").Build();
        }

        public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
        {
            SuccessPolicy policy = SuccessPolicy.RequireAll;
            m_SuccessPolicyOption?.TryGetValue(out policy);

            var children = GetChildren(graph);
            var childIndices = new int[children.Count];
            for (int i = 0; i < children.Count; i++)
                childIndices[i] = children[i].GetNodeIndex(graph);

            return new Runtime.ParallelNode
            {
                successPolicy = (Runtime.ParallelNode.SuccessPolicy)(int)policy,
                childIndices  = childIndices
            };
        }
    }
}
