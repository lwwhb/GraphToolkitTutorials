using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.BehaviorTree
{
    /// <summary>
    /// 序列节点 - 按顺序执行所有子节点，直到一个失败
    /// 如果所有子节点成功，返回成功；如果任何一个失败，返回失败
    /// </summary>
    [Node("Sequence", "Behavior Tree/Composite")]
    [UseWithGraph(typeof(BehaviorTreeGraph))]
    internal class SequenceNode : CompositeNode
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddParentPort(context);
            AddChildrenPort(context);
        }

        public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
        {
            var runtimeNode = new Runtime.SequenceNode();

            var children = GetChildren(graph);
            runtimeNode.childIndices = new int[children.Count];

            for (int i = 0; i < children.Count; i++)
            {
                runtimeNode.childIndices[i] = children[i].GetNodeIndex(graph);
            }

            return runtimeNode;
        }
    }

    /// <summary>
    /// 选择节点 - 按顺序执行子节点，直到一个成功
    /// 如果任何一个子节点成功，返回成功；如果所有失败，返回失败
    /// </summary>
    [Node("Selector", "Behavior Tree/Composite")]
    [UseWithGraph(typeof(BehaviorTreeGraph))]
    internal class SelectorNode : CompositeNode
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddParentPort(context);
            AddChildrenPort(context);
        }

        public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
        {
            var runtimeNode = new Runtime.SelectorNode();

            var children = GetChildren(graph);
            runtimeNode.childIndices = new int[children.Count];

            for (int i = 0; i < children.Count; i++)
            {
                runtimeNode.childIndices[i] = children[i].GetNodeIndex(graph);
            }

            return runtimeNode;
        }
    }

    /// <summary>
    /// 并行节点 - 同时执行所有子节点
    /// </summary>
    [Node("Parallel", "Behavior Tree/Composite")]
    [UseWithGraph(typeof(BehaviorTreeGraph))]
    internal class ParallelNode : CompositeNode
    {
        public enum SuccessPolicy
        {
            RequireAll,    // 所有子节点成功才成功
            RequireOne     // 任意一个子节点成功就成功
        }

        [SerializeField]
        private SuccessPolicy m_SuccessPolicy = SuccessPolicy.RequireAll;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddParentPort(context);
            AddChildrenPort(context);
        }

        public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
        {
            var runtimeNode = new Runtime.ParallelNode
            {
                successPolicy = m_SuccessPolicy
            };

            var children = GetChildren(graph);
            runtimeNode.childIndices = new int[children.Count];

            for (int i = 0; i < children.Count; i++)
            {
                runtimeNode.childIndices[i] = children[i].GetNodeIndex(graph);
            }

            return runtimeNode;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Success Policy", () => m_SuccessPolicy, v => m_SuccessPolicy = v).Build();
        }
    }
}
