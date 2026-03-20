using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.ExecutionFlow
{
    /// <summary>
    /// 延迟节点
    /// 等待指定的时间后继续执行
    /// </summary>
    [Node("Task", "")]
    [Serializable]
    internal class DelayNode : TaskNode
    {
        private IPort m_DurationInput;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);
            m_DurationInput = context.AddInputPort<float>("Duration").WithDefaultValue(1f).Build();
        }

        public override Runtime.TaskRuntimeNode CreateRuntimeNode(TaskGraph graph)
        {
            var runtimeNode = new Runtime.DelayNode();

            // 获取间隔时间（从输入端口或使用默认值）
            m_DurationInput.TryGetValue(out float value);
            var duratuibPort = graph.GetConnectedOutputPort(m_DurationInput);
            if (duratuibPort != null && duratuibPort.Direction == PortDirection.Output)
            {
                var node = graph.FindNodeForPort(duratuibPort);
                if (node is IConstantNode constantNode)
                    constantNode.TryGetValue(out value);
                else if (node is IVariableNode variableNode)
                    variableNode.Variable.TryGetDefaultValue(out value);
                
            }
            runtimeNode.duration = value;

            var nextNode = GetNextNode(graph);
            runtimeNode.nextNodeIndex = nextNode != null ? graph.GetNodeIndex(nextNode) : -1;

            return runtimeNode;
        }
    }
}
