using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GraphToolkitTutorials.RenderGraphBasics
{
    /// <summary>
    /// 渲染节点基类
    /// 所有渲染节点都继承自此类
    /// </summary>
    internal abstract class RenderNode : Node
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
        /// 添加执行端口
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
        /// 获取下一个节点
        /// </summary>
        public RenderNode GetNextNode(RenderGraph graph)
        {
            var connectedPort = graph.GetConnectedInputPort(m_ExecutionOut);
            if (connectedPort != null && connectedPort.Node is RenderNode renderNode)
            {
                return renderNode;
            }
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
            return graph.GetNodes().IndexOf(this);
        }
    }
}
