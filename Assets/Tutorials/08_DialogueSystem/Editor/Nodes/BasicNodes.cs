using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.DialogueSystem
{
    /// <summary>
    /// 起始对话节点 - 对话的起点，有且只有一个
    /// </summary>
    [Node("Dialogue", "")]
    [UseWithGraph(typeof(DialogueGraph))]
    [Serializable]
    internal class StartDialogueNode : DialogueNode
    {
        private IPort m_OutputPort;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_OutputPort = context.AddOutputPort("Out")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        public DialogueNode GetNextNode(DialogueGraph graph)
        {
            var connectedPort = graph.GetConnectedInputPort(m_OutputPort);
            if (connectedPort != null && graph.FindNodeForPort(connectedPort) is DialogueNode dialogueNode)
                return dialogueNode;
            return null;
        }

        public override Runtime.DialogueRuntimeNode CreateRuntimeNode(DialogueGraph graph)
        {
            var nextNode = GetNextNode(graph);
            return new Runtime.StartNode
            {
                nextNodeIndex = nextNode != null ? nextNode.GetNodeIndex(graph) : -1
            };
        }
    }

    /// <summary>
    /// 对话文本节点 - 显示说话人名称、头像和对话内容
    /// </summary>
    [Node("Dialogue", "")]
    [UseWithGraph(typeof(DialogueGraph))]
    [Serializable]
    internal class DialogueTextNode : DialogueNode
    {
        private INodeOption m_SpeakerNameOption;
        private INodeOption m_DialogueTextOption;
        private INodeOption m_PortraitOption;
        private IPort m_OutputPort;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddInputPort(context);
            m_OutputPort = context.AddOutputPort("Out")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_SpeakerNameOption  = context.AddOption<string>("Speaker Name").Delayed().Build();
            m_DialogueTextOption = context.AddOption<string>("Dialogue Text").AsTextArea().Delayed().Build();
            m_PortraitOption     = context.AddOption<Sprite>("Portrait").Build();
        }

        public DialogueNode GetNextNode(DialogueGraph graph)
        {
            var connectedPort = graph.GetConnectedInputPort(m_OutputPort);
            if (connectedPort != null && graph.FindNodeForPort(connectedPort) is DialogueNode dialogueNode)
                return dialogueNode;
            return null;
        }

        public override Runtime.DialogueRuntimeNode CreateRuntimeNode(DialogueGraph graph)
        {
            string speakerName = "Character";
            m_SpeakerNameOption?.TryGetValue(out speakerName);

            string dialogueText = "";
            m_DialogueTextOption?.TryGetValue(out dialogueText);

            Sprite portrait = null;
            m_PortraitOption?.TryGetValue(out portrait);

            var nextNode = GetNextNode(graph);
            return new Runtime.DialogueTextNode
            {
                speakerName     = speakerName  ?? "Character",
                dialogueText    = dialogueText ?? "",
                speakerPortrait = portrait,
                nextNodeIndex   = nextNode != null ? nextNode.GetNodeIndex(graph) : -1
            };
        }
    }

    /// <summary>
    /// 结束节点 - 对话结束
    /// </summary>
    [Node("Dialogue", "")]
    [UseWithGraph(typeof(DialogueGraph))]
    [Serializable]
    internal class EndDialogueNode : DialogueNode
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddInputPort(context);
        }

        public override Runtime.DialogueRuntimeNode CreateRuntimeNode(DialogueGraph graph)
        {
            return new Runtime.EndNode();
        }
    }
}
