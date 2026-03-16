using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.BehaviorTree
{
    /// <summary>
    /// 反转节点 - 反转子节点的结果
    /// 成功变失败，失败变成功
    /// </summary>
    [Node("Inverter", "Behavior Tree/Decorator")]
    [UseWithGraph(typeof(BehaviorTreeGraph))]
    internal class InverterNode : DecoratorNode
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddParentPort(context);
            AddChildPort(context);
        }

        public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
        {
            var runtimeNode = new Runtime.InverterNode();

            var child = GetChild(graph);
            if (child != null)
            {
                runtimeNode.childIndex = child.GetNodeIndex(graph);
            }
            else
            {
                runtimeNode.childIndex = -1;
            }

            return runtimeNode;
        }
    }

    /// <summary>
    /// 重复节点 - 重复执行子节点指定次数
    /// </summary>
    [Node("Repeater", "Behavior Tree/Decorator")]
    [UseWithGraph(typeof(BehaviorTreeGraph))]
    internal class RepeaterNode : DecoratorNode
    {
        [SerializeField]
        private int m_RepeatCount = 3;

        [SerializeField]
        private bool m_InfiniteLoop = false;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddParentPort(context);
            AddChildPort(context);
        }

        public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
        {
            var runtimeNode = new Runtime.RepeaterNode
            {
                repeatCount = m_RepeatCount,
                infiniteLoop = m_InfiniteLoop
            };

            var child = GetChild(graph);
            if (child != null)
            {
                runtimeNode.childIndex = child.GetNodeIndex(graph);
            }
            else
            {
                runtimeNode.childIndex = -1;
            }

            return runtimeNode;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Repeat Count", () => m_RepeatCount, v => m_RepeatCount = Mathf.Max(1, v)).Build();
            context.AddOption("Infinite Loop", () => m_InfiniteLoop, v => m_InfiniteLoop = v).Build();
        }
    }

    /// <summary>
    /// 成功节点 - 总是返回成功
    /// </summary>
    [Node("Succeeder", "Behavior Tree/Decorator")]
    [UseWithGraph(typeof(BehaviorTreeGraph))]
    internal class SucceederNode : DecoratorNode
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddParentPort(context);
            AddChildPort(context);
        }

        public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
        {
            var runtimeNode = new Runtime.SucceederNode();

            var child = GetChild(graph);
            if (child != null)
            {
                runtimeNode.childIndex = child.GetNodeIndex(graph);
            }
            else
            {
                runtimeNode.childIndex = -1;
            }

            return runtimeNode;
        }
    }

    /// <summary>
    /// 条件装饰节点 - 根据黑板变量决定是否执行子节点
    /// </summary>
    [Node("Conditional", "Behavior Tree/Decorator")]
    [UseWithGraph(typeof(BehaviorTreeGraph))]
    internal class ConditionalNode : DecoratorNode
    {
        [SerializeField]
        private string m_BlackboardKey = "condition";

        [SerializeField]
        private bool m_ExpectedValue = true;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddParentPort(context);
            AddChildPort(context);
        }

        public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
        {
            var runtimeNode = new Runtime.ConditionalNode
            {
                blackboardKey = m_BlackboardKey,
                expectedValue = m_ExpectedValue
            };

            var child = GetChild(graph);
            if (child != null)
            {
                runtimeNode.childIndex = child.GetNodeIndex(graph);
            }
            else
            {
                runtimeNode.childIndex = -1;
            }

            return runtimeNode;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Blackboard Key", () => m_BlackboardKey, v => m_BlackboardKey = v)
                .Delayed()
                .Build();

            context.AddOption("Expected Value", () => m_ExpectedValue, v => m_ExpectedValue = v).Build();
        }
    }
}
