using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;

namespace GraphToolkitTutorials.DialogueSystem
{
    /// <summary>
    /// 选择节点 - 向玩家显示选项，根据选择跳转不同分支
    /// 固定两个选项（Option 1 / Option 2），端口名与选项文本绑定
    /// </summary>
    [Node("Branch", "")]
    [UseWithGraph(typeof(DialogueGraph))]
    [Serializable]
    internal class ChoiceNode : DialogueNode
    {
        private INodeOption m_Option1TextOption;
        private INodeOption m_Option2TextOption;
        private IPort m_OutputPort1;
        private IPort m_OutputPort2;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddInputPort(context);
            m_OutputPort1 = context.AddOutputPort("Option 1")
                .WithConnectorUI(PortConnectorUI.Arrowhead).Build();
            m_OutputPort2 = context.AddOutputPort("Option 2")
                .WithConnectorUI(PortConnectorUI.Arrowhead).Build();
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_Option1TextOption = context.AddOption<string>("Option 1 Text").Delayed().Build();
            m_Option2TextOption = context.AddOption<string>("Option 2 Text").Delayed().Build();
        }

        public override Runtime.DialogueRuntimeNode CreateRuntimeNode(DialogueGraph graph)
        {
            string opt1 = "Option 1";
            m_Option1TextOption?.TryGetValue(out opt1);
            string opt2 = "Option 2";
            m_Option2TextOption?.TryGetValue(out opt2);

            int next1 = -1, next2 = -1;
            var conn1 = graph.GetConnectedInputPort(m_OutputPort1);
            if (conn1 != null && graph.FindNodeForPort(conn1) is DialogueNode n1)
                next1 = n1.GetNodeIndex(graph);
            var conn2 = graph.GetConnectedInputPort(m_OutputPort2);
            if (conn2 != null && graph.FindNodeForPort(conn2) is DialogueNode n2)
                next2 = n2.GetNodeIndex(graph);

            return new Runtime.ChoiceNode
            {
                optionTexts      = new[] { opt1 ?? "Option 1", opt2 ?? "Option 2" },
                nextNodeIndices  = new[] { next1, next2 }
            };
        }
    }

    /// <summary>
    /// 条件分支节点 - 读取 DialogueVariables 中的变量决定走哪条路
    /// </summary>
    [Node("Branch", "")]
    [UseWithGraph(typeof(DialogueGraph))]
    [Serializable]
    internal class BranchNode : DialogueNode
    {
        private INodeOption m_ConditionKeyOption;
        private INodeOption m_ExpectedValueOption;
        private IPort m_TruePort;
        private IPort m_FalsePort;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddInputPort(context);
            m_TruePort  = context.AddOutputPort("True") .WithConnectorUI(PortConnectorUI.Arrowhead).Build();
            m_FalsePort = context.AddOutputPort("False").WithConnectorUI(PortConnectorUI.Arrowhead).Build();
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_ConditionKeyOption   = context.AddOption<string>("Condition Key").Delayed().Build();
            m_ExpectedValueOption  = context.AddOption<string>("Expected Value").Delayed().Build();
        }

        public override Runtime.DialogueRuntimeNode CreateRuntimeNode(DialogueGraph graph)
        {
            string conditionKey = "condition";
            m_ConditionKeyOption?.TryGetValue(out conditionKey);
            string expectedValue = "true";
            m_ExpectedValueOption?.TryGetValue(out expectedValue);

            int trueIndex = -1, falseIndex = -1;
            var connTrue = graph.GetConnectedInputPort(m_TruePort);
            if (connTrue != null && graph.FindNodeForPort(connTrue) is DialogueNode tn)
                trueIndex = tn.GetNodeIndex(graph);
            var connFalse = graph.GetConnectedInputPort(m_FalsePort);
            if (connFalse != null && graph.FindNodeForPort(connFalse) is DialogueNode fn)
                falseIndex = fn.GetNodeIndex(graph);

            return new Runtime.BranchNode
            {
                conditionKey  = conditionKey  ?? "condition",
                expectedValue = expectedValue ?? "true",
                trueNodeIndex  = trueIndex,
                falseNodeIndex = falseIndex
            };
        }
    }
}
