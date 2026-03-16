using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.DialogueSystem
{
    /// <summary>
    /// 起始对话节点 - 对话的起点
    /// </summary>
    [Node("Start", "Dialogue")]
    [UseWithGraph(typeof(DialogueGraph))]
    internal class StartDialogueNode : DialogueNode
    {
        private IPort m_OutputPort;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // 起始节点只有输出端口
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
            var runtimeNode = new Runtime.StartNode();

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
    }

    /// <summary>
    /// 对话文本节点 - 显示对话内容
    /// </summary>
    [Node("Dialogue", "Dialogue")]
    [UseWithGraph(typeof(DialogueGraph))]
    internal class DialogueTextNode : DialogueNode
    {
        [SerializeField]
        private string m_SpeakerName = "Character";

        [SerializeField]
        [TextArea(3, 10)]
        private string m_DialogueText = "Hello!";

        [SerializeField]
        private Sprite m_SpeakerPortrait;

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
            var runtimeNode = new Runtime.DialogueTextNode
            {
                speakerName = m_SpeakerName,
                dialogueText = m_DialogueText,
                speakerPortrait = m_SpeakerPortrait
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
            context.AddOption("Speaker Name", () => m_SpeakerName, v => m_SpeakerName = v)
                .Delayed()
                .Build();

            context.AddOption("Dialogue Text", () => m_DialogueText, v => m_DialogueText = v)
                .Delayed()
                .Build();

            context.AddOption("Portrait", () => m_SpeakerPortrait, v => m_SpeakerPortrait = v)
                .Build();
        }
    }

    /// <summary>
    /// 结束节点 - 对话结束
    /// </summary>
    [Node("End", "Dialogue")]
    [UseWithGraph(typeof(DialogueGraph))]
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
