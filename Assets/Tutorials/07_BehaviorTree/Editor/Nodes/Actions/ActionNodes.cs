using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.BehaviorTree
{
    /// <summary>
    /// 等待节点 — 等待指定秒数后成功
    /// </summary>
    [Node("Action", "")]
    [UseWithGraph(typeof(BehaviorTreeGraph))]
    [System.Serializable]
    internal class WaitNode : LeafNode
    {
        private INodeOption m_DurationOption;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddParentPort(context);
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_DurationOption = context.AddOption<float>("Duration").Build();
        }

        public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
        {
            float duration = 1f;
            m_DurationOption?.TryGetValue(out duration);
            return new Runtime.WaitNode { duration = Mathf.Max(0f, duration) };
        }
    }

    /// <summary>
    /// 日志节点 — 输出日志信息后成功
    /// </summary>
    [Node("Action", "")]
    [UseWithGraph(typeof(BehaviorTreeGraph))]
    [System.Serializable]
    internal class LogNode : LeafNode
    {
        private INodeOption m_MessageOption;
        private INodeOption m_LogTypeOption;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddParentPort(context);
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_MessageOption = context.AddOption<string>("Message").AsTextArea().Delayed().Build();
            m_LogTypeOption = context.AddOption<LogType>("Log Type").Build();
        }

        public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
        {
            string message = "Behavior Tree Log";
            m_MessageOption?.TryGetValue(out message);

            LogType logType = LogType.Log;
            m_LogTypeOption?.TryGetValue(out logType);

            return new Runtime.LogNode
            {
                message = message ?? "Behavior Tree Log",
                logType = logType
            };
        }
    }

    /// <summary>
    /// 设置黑板值节点 — 设置黑板变量后成功
    /// </summary>
    [Node("Action", "")]
    [UseWithGraph(typeof(BehaviorTreeGraph))]
    [System.Serializable]
    internal class SetBlackboardValueNode : LeafNode
    {
        private INodeOption m_KeyOption;
        private INodeOption m_ValueOption;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddParentPort(context);
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_KeyOption   = context.AddOption<string>("Key").Delayed().Build();
            m_ValueOption = context.AddOption<string>("Value").Delayed().Build();
        }

        public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
        {
            string key = "key";
            m_KeyOption?.TryGetValue(out key);

            string value = "value";
            m_ValueOption?.TryGetValue(out value);

            return new Runtime.SetBlackboardValueNode
            {
                key   = key   ?? "key",
                value = value ?? "value"
            };
        }
    }

    /// <summary>
    /// 检查黑板值节点 — 检查黑板变量是否等于期望值
    /// </summary>
    [Node("Action", "")]
    [UseWithGraph(typeof(BehaviorTreeGraph))]
    [System.Serializable]
    internal class CheckBlackboardValueNode : LeafNode
    {
        private INodeOption m_KeyOption;
        private INodeOption m_ExpectedValueOption;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddParentPort(context);
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_KeyOption           = context.AddOption<string>("Key").Delayed().Build();
            m_ExpectedValueOption = context.AddOption<string>("Expected Value").Delayed().Build();
        }

        public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
        {
            string key = "key";
            m_KeyOption?.TryGetValue(out key);

            string expectedValue = "value";
            m_ExpectedValueOption?.TryGetValue(out expectedValue);

            return new Runtime.CheckBlackboardValueNode
            {
                key           = key           ?? "key",
                expectedValue = expectedValue ?? "value"
            };
        }
    }

    /// <summary>
    /// 随机成功节点 — 以给定概率随机返回成功或失败
    /// </summary>
    [Node("Action", "")]
    [UseWithGraph(typeof(BehaviorTreeGraph))]
    [System.Serializable]
    internal class RandomSuccessNode : LeafNode
    {
        private INodeOption m_SuccessProbabilityOption;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddParentPort(context);
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_SuccessProbabilityOption = context.AddOption<float>("Success Probability").Build();
        }

        public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
        {
            float probability = 0.5f;
            m_SuccessProbabilityOption?.TryGetValue(out probability);
            return new Runtime.RandomSuccessNode { successProbability = Mathf.Clamp01(probability) };
        }
    }
}
