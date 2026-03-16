using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.ExecutionFlow
{
    /// <summary>
    /// 分支节点
    /// 根据条件选择不同的执行路径
    /// </summary>
    [Node("Branch", "Task")]
    internal class BranchNode : TaskNode
    {
        [SerializeField]
        private bool m_Condition = true;

        private IPort m_TrueOut;
        private IPort m_FalseOut;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_ExecutionIn = context.AddInputPort("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            context.AddInputPort<bool>("Condition").Build();

            m_TrueOut = context.AddOutputPort("True")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            m_FalseOut = context.AddOutputPort("False")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        public override Runtime.TaskRuntimeNode CreateRuntimeNode(TaskGraph graph)
        {
            var runtimeNode = new Runtime.BranchNode
            {
                condition = m_Condition
            };

            // 获取True分支的下一个节点
            var truePort = graph.GetConnectedInputPort(m_TrueOut);
            if (truePort != null && truePort.Node is TaskNode trueNode)
            {
                runtimeNode.trueNodeIndex = graph.GetNodes().IndexOf(trueNode);
            }
            else
            {
                runtimeNode.trueNodeIndex = -1;
            }

            // 获取False分支的下一个节点
            var falsePort = graph.GetConnectedInputPort(m_FalseOut);
            if (falsePort != null && falsePort.Node is TaskNode falseNode)
            {
                runtimeNode.falseNodeIndex = graph.GetNodes().IndexOf(falseNode);
            }
            else
            {
                runtimeNode.falseNodeIndex = -1;
            }

            return runtimeNode;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Condition", () => m_Condition, v => m_Condition = v).Build();
        }
    }
}
