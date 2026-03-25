using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;

namespace GraphToolkitTutorials.RenderGraphBasics
{
    /// <summary>
    /// 渲染节点基类
    /// 所有渲染节点都继承自此类
    /// </summary>
    [Serializable]
    internal abstract class RenderNode : Node
    {
        /// <summary>执行输入端口</summary>
        protected IPort m_ExecutionIn;

        /// <summary>执行输出端口</summary>
        protected IPort m_ExecutionOut;

        /// <summary>
        /// 添加执行流端口（In / Out）
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
        /// 获取执行流的下一个节点
        /// </summary>
        public RenderNode GetNextNode(RenderGraph graph)
        {
            var connectedPort = graph.GetConnectedInputPort(m_ExecutionOut);
            // IPort 没有 .Node 属性，必须用 graph.FindNodeForPort
            if (connectedPort != null && graph.FindNodeForPort(connectedPort) is RenderNode rn)
                return rn;
            return null;
        }

        /// <summary>
        /// 创建运行时节点
        /// </summary>
        public abstract Runtime.RenderRuntimeNode CreateRuntimeNode(RenderGraph graph);

        /// <summary>
        /// 获取节点在图形中的索引
        /// </summary>
        public int GetNodeIndex(RenderGraph graph)
        {
            var allNodes = new List<INode>(graph.GetNodes());
            for (int i = 0; i < allNodes.Count; i++)
                if (allNodes[i] == this) return i;
            return -1;
        }
    }
}
