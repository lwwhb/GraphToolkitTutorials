using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.ExecutionFlow
{
    /// <summary>
    /// 任务节点基类
    /// 所有任务节点都继承自此类
    /// </summary>
    [Serializable]
    internal abstract class TaskNode : Node
    {
        /// <summary>
        /// 执行输入端口
        /// </summary>
        protected IPort m_ExecutionIn;

        /// <summary>
        /// 执行输出端口
        /// </summary>
        protected IPort m_ExecutionOut;

        /// <summary>
        /// 添加标准的执行流端口
        /// </summary>
        protected void AddExecutionPorts(IPortDefinitionContext context)
        {
            m_ExecutionIn = context.AddInputPort("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            m_ExecutionOut = context.AddOutputPort("Out")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        /// <summary>
        /// 获取下一个要执行的节点
        /// </summary>
        public TaskNode GetNextNode(TaskGraph graph)
        {
            var connectedPort = graph.GetConnectedInputPort(m_ExecutionOut);
            if (connectedPort != null)
            {
                return graph.FindNodeForPort(connectedPort) as TaskNode;
            }
            return null;
        }

        /// <summary>
        /// 创建运行时节点数据
        /// </summary>
        public abstract Runtime.TaskRuntimeNode CreateRuntimeNode(TaskGraph graph);
    }
}
