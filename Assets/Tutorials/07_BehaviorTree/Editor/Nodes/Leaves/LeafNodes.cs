using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.BehaviorTree
{
    /// <summary>
    /// 等待节点 - 等待指定时间
    /// </summary>
    [Node("Wait", "Behavior Tree/Action")]
    [UseWithGraph(typeof(BehaviorTreeGraph))]
    internal class WaitNode : LeafNode
    {
        [SerializeField]
        private float m_Duration = 1f;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddParentPort(context);
        }

        public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
        {
            return new Runtime.WaitNode
            {
                duration = m_Duration
            };
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Duration", () => m_Duration, v => m_Duration = Mathf.Max(0f, v)).Build();
        }
    }

    /// <summary>
    /// 日志节点 - 输出日志信息
    /// </summary>
    [Node("Log", "Behavior Tree/Action")]
    [UseWithGraph(typeof(BehaviorTreeGraph))]
    internal class LogNode : LeafNode
    {
        [SerializeField]
        private string m_Message = "Behavior Tree Log";

        [SerializeField]
        private LogType m_LogType = LogType.Log;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddParentPort(context);
        }

        public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
        {
            return new Runtime.LogNode
            {
                message = m_Message,
                logType = m_LogType
            };
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Message", () => m_Message, v => m_Message = v)
                .Delayed()
                .Build();

            context.AddOption("Log Type", () => m_LogType, v => m_LogType = v).Build();
        }
    }

    /// <summary>
    /// 设置黑板值节点 - 设置黑板变量
    /// </summary>
    [Node("Set Blackboard Value", "Behavior Tree/Action")]
    [UseWithGraph(typeof(BehaviorTreeGraph))]
    internal class SetBlackboardValueNode : LeafNode
    {
        [SerializeField]
        private string m_Key = "key";

        [SerializeField]
        private string m_Value = "value";

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddParentPort(context);
        }

        public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
        {
            return new Runtime.SetBlackboardValueNode
            {
                key = m_Key,
                value = m_Value
            };
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Key", () => m_Key, v => m_Key = v)
                .Delayed()
                .Build();

            context.AddOption("Value", () => m_Value, v => m_Value = v)
                .Delayed()
                .Build();
        }
    }

    /// <summary>
    /// 检查黑板值节点 - 检查黑板变量是否满足条件
    /// </summary>
    [Node("Check Blackboard Value", "Behavior Tree/Condition")]
    [UseWithGraph(typeof(BehaviorTreeGraph))]
    internal class CheckBlackboardValueNode : LeafNode
    {
        [SerializeField]
        private string m_Key = "key";

        [SerializeField]
        private string m_ExpectedValue = "value";

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddParentPort(context);
        }

        public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
        {
            return new Runtime.CheckBlackboardValueNode
            {
                key = m_Key,
                expectedValue = m_ExpectedValue
            };
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Key", () => m_Key, v => m_Key = v)
                .Delayed()
                .Build();

            context.AddOption("Expected Value", () => m_ExpectedValue, v => m_ExpectedValue = v)
                .Delayed()
                .Build();
        }
    }

    /// <summary>
    /// 随机成功节点 - 随机返回成功或失败
    /// </summary>
    [Node("Random Success", "Behavior Tree/Condition")]
    [UseWithGraph(typeof(BehaviorTreeGraph))]
    internal class RandomSuccessNode : LeafNode
    {
        [SerializeField]
        [Range(0f, 1f)]
        private float m_SuccessProbability = 0.5f;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddParentPort(context);
        }

        public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
        {
            return new Runtime.RandomSuccessNode
            {
                successProbability = m_SuccessProbability
            };
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Success Probability", () => m_SuccessProbability,
                v => m_SuccessProbability = Mathf.Clamp01(v)).Build();
        }
    }
}
