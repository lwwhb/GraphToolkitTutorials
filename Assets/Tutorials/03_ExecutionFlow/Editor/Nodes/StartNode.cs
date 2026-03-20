using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.ExecutionFlow
{
    /// <summary>
    /// 起始节点
    /// 标记任务图形的执行起点
    /// </summary>
    [Node("Task", "")]
    [Serializable]
    internal class StartNode : TaskNode
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // 起始节点只有输出端口
            m_ExecutionOut = context.AddOutputPort("Out")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        public override Runtime.TaskRuntimeNode CreateRuntimeNode(TaskGraph graph)
        {
            var runtimeNode = new Runtime.StartNode();

            var nextNode = GetNextNode(graph);
            runtimeNode.nextNodeIndex = nextNode != null ? graph.GetNodeIndex(nextNode) : -1;

            return runtimeNode;
        }
    }
}
