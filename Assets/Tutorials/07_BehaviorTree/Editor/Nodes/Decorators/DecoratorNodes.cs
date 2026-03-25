using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.BehaviorTree
{
    /// <summary>
    /// 反转节点 — 反转子节点的结果：成功变失败，失败变成功
    /// </summary>
    [Node("Decorator", "")]
    [UseWithGraph(typeof(BehaviorTreeGraph))]
    [System.Serializable]
    internal class InverterNode : DecoratorNode
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddParentPort(context);
            AddChildPort(context);
        }

        public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
        {
            var child = GetChild(graph);
            return new Runtime.InverterNode
            {
                childIndex = child != null ? child.GetNodeIndex(graph) : -1
            };
        }
    }

    /// <summary>
    /// 重复节点 — 重复执行子节点指定次数
    /// </summary>
    [Node("Decorator", "")]
    [UseWithGraph(typeof(BehaviorTreeGraph))]
    [System.Serializable]
    internal class RepeaterNode : DecoratorNode
    {
        private INodeOption m_RepeatCountOption;
        private INodeOption m_InfiniteLoopOption;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddParentPort(context);
            AddChildPort(context);
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_RepeatCountOption  = context.AddOption<int>("Repeat Count").Build();
            m_InfiniteLoopOption = context.AddOption<bool>("Infinite Loop").Build();
        }

        public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
        {
            int repeatCount = 3;
            m_RepeatCountOption?.TryGetValue(out repeatCount);
            repeatCount = Mathf.Max(1, repeatCount);

            bool infiniteLoop = false;
            m_InfiniteLoopOption?.TryGetValue(out infiniteLoop);

            var child = GetChild(graph);
            return new Runtime.RepeaterNode
            {
                repeatCount  = repeatCount,
                infiniteLoop = infiniteLoop,
                childIndex   = child != null ? child.GetNodeIndex(graph) : -1
            };
        }
    }

    /// <summary>
    /// 成功节点 — 总是返回成功（忽略子节点结果）
    /// </summary>
    [Node("Decorator", "")]
    [UseWithGraph(typeof(BehaviorTreeGraph))]
    [System.Serializable]
    internal class SucceederNode : DecoratorNode
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddParentPort(context);
            AddChildPort(context);
        }

        public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
        {
            var child = GetChild(graph);
            return new Runtime.SucceederNode
            {
                childIndex = child != null ? child.GetNodeIndex(graph) : -1
            };
        }
    }

    /// <summary>
    /// 条件装饰节点 — 根据黑板变量决定是否执行子节点
    /// </summary>
    [Node("Conditional", "Behavior Tree/Decorator")]
    [UseWithGraph(typeof(BehaviorTreeGraph))]
    [System.Serializable]
    internal class ConditionalNode : DecoratorNode
    {
        private INodeOption m_BlackboardKeyOption;
        private INodeOption m_ExpectedValueOption;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddParentPort(context);
            AddChildPort(context);
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_BlackboardKeyOption  = context.AddOption<string>("Blackboard Key").Delayed().Build();
            m_ExpectedValueOption  = context.AddOption<bool>("Expected Value").Build();
        }

        public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
        {
            string blackboardKey = "condition";
            m_BlackboardKeyOption?.TryGetValue(out blackboardKey);

            bool expectedValue = true;
            m_ExpectedValueOption?.TryGetValue(out expectedValue);

            var child = GetChild(graph);
            return new Runtime.ConditionalNode
            {
                blackboardKey = blackboardKey ?? "condition",
                expectedValue = expectedValue,
                childIndex    = child != null ? child.GetNodeIndex(graph) : -1
            };
        }
    }
}
