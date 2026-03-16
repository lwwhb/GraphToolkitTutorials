using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.DialogueSystem
{
    /// <summary>
    /// 选择节点 - 玩家选择分支
    /// </summary>
    [Node("Choice", "Dialogue")]
    [UseWithGraph(typeof(DialogueGraph))]
    internal class ChoiceNode : DialogueNode
    {
        [System.Serializable]
        public class ChoiceOption
        {
            public string text = "Option";
            public string portName = "Option";
        }

        [SerializeField]
        private List<ChoiceOption> m_Options = new List<ChoiceOption>
        {
            new ChoiceOption { text = "Option 1", portName = "Option 1" },
            new ChoiceOption { text = "Option 2", portName = "Option 2" }
        };

        private List<IPort> m_OutputPorts = new List<IPort>();

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddInputPort(context);

            m_OutputPorts.Clear();
            foreach (var option in m_Options)
            {
                var port = context.AddOutputPort(option.portName)
                    .WithConnectorUI(PortConnectorUI.Arrowhead)
                    .Build();
                m_OutputPorts.Add(port);
            }
        }

        public List<DialogueNode> GetNextNodes(DialogueGraph graph)
        {
            var nextNodes = new List<DialogueNode>();

            foreach (var outputPort in m_OutputPorts)
            {
                var connectedPort = graph.GetConnectedInputPort(outputPort);
                if (connectedPort != null && connectedPort.Node is DialogueNode dialogueNode)
                {
                    nextNodes.Add(dialogueNode);
                }
                else
                {
                    nextNodes.Add(null);
                }
            }

            return nextNodes;
        }

        public override Runtime.DialogueRuntimeNode CreateRuntimeNode(DialogueGraph graph)
        {
            var runtimeNode = new Runtime.ChoiceNode();

            // 复制选项文本
            runtimeNode.optionTexts = new string[m_Options.Count];
            for (int i = 0; i < m_Options.Count; i++)
            {
                runtimeNode.optionTexts[i] = m_Options[i].text;
            }

            // 获取每个选项的下一个节点索引
            var nextNodes = GetNextNodes(graph);
            runtimeNode.nextNodeIndices = new int[nextNodes.Count];

            for (int i = 0; i < nextNodes.Count; i++)
            {
                if (nextNodes[i] != null)
                {
                    runtimeNode.nextNodeIndices[i] = nextNodes[i].GetNodeIndex(graph);
                }
                else
                {
                    runtimeNode.nextNodeIndices[i] = -1;
                }
            }

            return runtimeNode;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            // 注意：动态选项列表在当前GraphToolkit版本中可能不完全支持
            // 这里提供基本的显示
            for (int i = 0; i < m_Options.Count; i++)
            {
                int index = i; // 闭包捕获
                context.AddOption($"Option {i + 1}",
                    () => m_Options[index].text,
                    v => m_Options[index].text = v)
                    .Delayed()
                    .Build();
            }
        }
    }

    /// <summary>
    /// 条件分支节点 - 根据条件选择路径
    /// </summary>
    [Node("Branch", "Dialogue")]
    [UseWithGraph(typeof(DialogueGraph))]
    internal class BranchNode : DialogueNode
    {
        [SerializeField]
        private string m_ConditionKey = "condition";

        [SerializeField]
        private string m_ExpectedValue = "true";

        private IPort m_TruePort;
        private IPort m_FalsePort;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddInputPort(context);

            m_TruePort = context.AddOutputPort("True")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            m_FalsePort = context.AddOutputPort("False")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        public DialogueNode GetTrueNode(DialogueGraph graph)
        {
            var connectedPort = graph.GetConnectedInputPort(m_TruePort);
            if (connectedPort != null && connectedPort.Node is DialogueNode dialogueNode)
            {
                return dialogueNode;
            }
            return null;
        }

        public DialogueNode GetFalseNode(DialogueGraph graph)
        {
            var connectedPort = graph.GetConnectedInputPort(m_FalsePort);
            if (connectedPort != null && connectedPort.Node is DialogueNode dialogueNode)
            {
                return dialogueNode;
            }
            return null;
        }

        public override Runtime.DialogueRuntimeNode CreateRuntimeNode(DialogueGraph graph)
        {
            var runtimeNode = new Runtime.BranchNode
            {
                conditionKey = m_ConditionKey,
                expectedValue = m_ExpectedValue
            };

            var trueNode = GetTrueNode(graph);
            if (trueNode != null)
            {
                runtimeNode.trueNodeIndex = trueNode.GetNodeIndex(graph);
            }
            else
            {
                runtimeNode.trueNodeIndex = -1;
            }

            var falseNode = GetFalseNode(graph);
            if (falseNode != null)
            {
                runtimeNode.falseNodeIndex = falseNode.GetNodeIndex(graph);
            }
            else
            {
                runtimeNode.falseNodeIndex = -1;
            }

            return runtimeNode;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Condition Key", () => m_ConditionKey, v => m_ConditionKey = v)
                .Delayed()
                .Build();

            context.AddOption("Expected Value", () => m_ExpectedValue, v => m_ExpectedValue = v)
                .Delayed()
                .Build();
        }
    }
}
