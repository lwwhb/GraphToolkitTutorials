using System;
using Unity.GraphToolkit.Editor;

namespace GraphToolkitTutorials.DialogueSystem
{
    /// <summary>
    /// 设置变量节点 - 向 DialogueVariables 写入键值后继续执行
    /// </summary>
    [Node("Action", "")]
    [UseWithGraph(typeof(DialogueGraph))]
    [Serializable]
    internal class SetVariableNode : DialogueNode
    {
        private INodeOption m_KeyOption;
        private INodeOption m_ValueOption;
        private IPort m_OutputPort;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddInputPort(context);
            m_OutputPort = context.AddOutputPort("Out")
                .WithConnectorUI(PortConnectorUI.Arrowhead).Build();
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_KeyOption   = context.AddOption<string>("Variable Key").Delayed().Build();
            m_ValueOption = context.AddOption<string>("Variable Value").Delayed().Build();
        }

        public override Runtime.DialogueRuntimeNode CreateRuntimeNode(DialogueGraph graph)
        {
            string key = "key";
            m_KeyOption?.TryGetValue(out key);
            string value = "value";
            m_ValueOption?.TryGetValue(out value);

            int next = -1;
            var conn = graph.GetConnectedInputPort(m_OutputPort);
            if (conn != null && graph.FindNodeForPort(conn) is DialogueNode n)
                next = n.GetNodeIndex(graph);

            return new Runtime.SetVariableNode
            {
                variableKey   = key   ?? "key",
                variableValue = value ?? "value",
                nextNodeIndex = next
            };
        }
    }

    /// <summary>
    /// 事件节点 - 触发具名游戏事件（通过 UnityEvent 通知外部系统）
    /// </summary>
    [Node("Action", "")]
    [UseWithGraph(typeof(DialogueGraph))]
    [Serializable]
    internal class EventNode : DialogueNode
    {
        private INodeOption m_EventNameOption;
        private INodeOption m_EventParamOption;
        private IPort m_OutputPort;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddInputPort(context);
            m_OutputPort = context.AddOutputPort("Out")
                .WithConnectorUI(PortConnectorUI.Arrowhead).Build();
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_EventNameOption  = context.AddOption<string>("Event Name").Delayed().Build();
            m_EventParamOption = context.AddOption<string>("Event Parameter").Delayed().Build();
        }

        public override Runtime.DialogueRuntimeNode CreateRuntimeNode(DialogueGraph graph)
        {
            string eventName = "EventName";
            m_EventNameOption?.TryGetValue(out eventName);
            string eventParam = "";
            m_EventParamOption?.TryGetValue(out eventParam);

            int next = -1;
            var conn = graph.GetConnectedInputPort(m_OutputPort);
            if (conn != null && graph.FindNodeForPort(conn) is DialogueNode n)
                next = n.GetNodeIndex(graph);

            return new Runtime.EventNode
            {
                eventName      = eventName  ?? "EventName",
                eventParameter = eventParam ?? "",
                nextNodeIndex  = next
            };
        }
    }
}
