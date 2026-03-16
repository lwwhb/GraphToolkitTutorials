using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.DialogueSystem
{
    /// <summary>
    /// 设置变量节点 - 设置对话变量
    /// </summary>
    [Node("Set Variable", "Dialogue/Action")]
    [UseWithGraph(typeof(DialogueGraph))]
    internal class SetVariableNode : DialogueNode
    {
        [SerializeField]
        private string m_VariableKey = "key";

        [SerializeField]
        private string m_VariableValue = "value";

        private IPort m_OutputPort;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddInputPort(context);

            m_OutputPort = context.AddOutputPort("Out")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        public DialogueNode GetNextNode(DialogueGraph graph)
        {
            var connectedPort = graph.GetConnectedInputPort(m_OutputPort);
            if (connectedPort != null && connectedPort.Node is DialogueNode dialogueNode)
            {
                return dialogueNode;
            }
            return null;
        }

        public override Runtime.DialogueRuntimeNode CreateRuntimeNode(DialogueGraph graph)
        {
            var runtimeNode = new Runtime.SetVariableNode
            {
                variableKey = m_VariableKey,
                variableValue = m_VariableValue
            };

            var nextNode = GetNextNode(graph);
            if (nextNode != null)
            {
                runtimeNode.nextNodeIndex = nextNode.GetNodeIndex(graph);
            }
            else
            {
                runtimeNode.nextNodeIndex = -1;
            }

            return runtimeNode;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Variable Key", () => m_VariableKey, v => m_VariableKey = v)
                .Delayed()
                .Build();

            context.AddOption("Variable Value", () => m_VariableValue, v => m_VariableValue = v)
                .Delayed()
                .Build();
        }
    }

    /// <summary>
    /// 事件节点 - 触发游戏事件
    /// </summary>
    [Node("Event", "Dialogue/Action")]
    [UseWithGraph(typeof(DialogueGraph))]
    internal class EventNode : DialogueNode
    {
        [SerializeField]
        private string m_EventName = "EventName";

        [SerializeField]
        private string m_EventParameter = "";

        private IPort m_OutputPort;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddInputPort(context);

            m_OutputPort = context.AddOutputPort("Out")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        public DialogueNode GetNextNode(DialogueGraph graph)
        {
            var connectedPort = graph.GetConnectedInputPort(m_OutputPort);
            if (connectedPort != null && connectedPort.Node is DialogueNode dialogueNode)
            {
                return dialogueNode;
            }
            return null;
        }

        public override Runtime.DialogueRuntimeNode CreateRuntimeNode(DialogueGraph graph)
        {
            var runtimeNode = new Runtime.EventNode
            {
                eventName = m_EventName,
                eventParameter = m_EventParameter
            };

            var nextNode = GetNextNode(graph);
            if (nextNode != null)
            {
                runtimeNode.nextNodeIndex = nextNode.GetNodeIndex(graph);
            }
            else
            {
                runtimeNode.nextNodeIndex = -1;
            }

            return runtimeNode;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Event Name", () => m_EventName, v => m_EventName = v)
                .Delayed()
                .Build();

            context.AddOption("Event Parameter", () => m_EventParameter, v => m_EventParameter = v)
                .Delayed()
                .Build();
        }
    }
}
