using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.ExecutionFlow
{
    /// <summary>
    /// 分支节点
    /// 根据条件选择不同的执行路径
    /// </summary>
    [Node("Task", "")]
    [Serializable]
    internal class BranchNode : TaskNode
    {
        private IPort m_ConditionInput;
        private IPort m_TrueOut;
        private IPort m_FalseOut;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_ExecutionIn = context.AddInputPort("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            m_TrueOut = context.AddOutputPort("True")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            m_FalseOut = context.AddOutputPort("False")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
            
            m_ConditionInput = context.AddInputPort<bool>("Condition").WithDefaultValue(true).Build();
        }

        public override Runtime.TaskRuntimeNode CreateRuntimeNode(TaskGraph graph)
        {
            var runtimeNode = new Runtime.BranchNode();
            // 条件（从输入端口或使用默认值）
            m_ConditionInput.TryGetValue(out bool value);
            var conditionPort = graph.GetConnectedOutputPort(m_ConditionInput);
            if (conditionPort != null && conditionPort.Direction == PortDirection.Output)
            {
                var node = graph.FindNodeForPort(conditionPort);
                if (node is IConstantNode constantNode)
                    constantNode.TryGetValue(out value);
                else if (node is IVariableNode variableNode)
                {
                    variableNode.Variable.TryGetDefaultValue(out value);
                    runtimeNode.conditionVariableName = variableNode.Variable.Name;
                }
            }
            runtimeNode.condition = value;

            // 获取True分支的下一个节点
            var trueConnectedPort = graph.GetConnectedInputPort(m_TrueOut);
            if (trueConnectedPort != null)
            {
                var trueNode = graph.FindNodeForPort(trueConnectedPort) as TaskNode;
                runtimeNode.trueNodeIndex = trueNode != null ? graph.GetNodeIndex(trueNode) : -1;
            }
            else
            {
                runtimeNode.trueNodeIndex = -1;
            }

            // 获取False分支的下一个节点
            var falseConnectedPort = graph.GetConnectedInputPort(m_FalseOut);
            if (falseConnectedPort != null)
            {
                var falseNode = graph.FindNodeForPort(falseConnectedPort) as TaskNode;
                runtimeNode.falseNodeIndex = falseNode != null ? graph.GetNodeIndex(falseNode) : -1;
            }
            else
            {
                runtimeNode.falseNodeIndex = -1;
            }

            return runtimeNode;
        }
    }
}
