using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.RenderGraphAPI
{
    /// <summary>
    /// RenderGraph节点基类
    /// </summary>
    internal abstract class RenderGraphNode : Node
    {
        protected IPort m_InputPort;
        protected IPort m_OutputPort;

        /// <summary>
        /// 添加执行流端口
        /// </summary>
        protected void AddExecutionPorts(IPortDefinitionContext context)
        {
            m_InputPort = context.AddInputPort("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            m_OutputPort = context.AddOutputPort("Out")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        /// <summary>
        /// 创建运行时节点
        /// </summary>
        public abstract Runtime.RenderGraphRuntimeNode CreateRuntimeNode(NewRenderGraph graph);

        /// <summary>
        /// 获取节点索引
        /// </summary>
        public int GetNodeIndex(NewRenderGraph graph)
        {
            return graph.GetNodes().IndexOf(this);
        }

        /// <summary>
        /// 获取下一个节点索引
        /// </summary>
        protected int GetNextNodeIndex(NewRenderGraph graph)
        {
            if (m_OutputPort == null) return -1;

            var connectedPort = graph.GetConnectedInputPort(m_OutputPort);
            if (connectedPort != null && connectedPort.Node is RenderGraphNode nextNode)
            {
                return nextNode.GetNodeIndex(graph);
            }
            return -1;
        }
    }

    /// <summary>
    /// 管线起始节点
    /// </summary>
    [Node("Pipeline Start", "RenderGraph")]
    [UseWithGraph(typeof(NewRenderGraph))]
    internal class PipelineStartNode : RenderGraphNode
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_OutputPort = context.AddOutputPort("Out")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        public override Runtime.RenderGraphRuntimeNode CreateRuntimeNode(NewRenderGraph graph)
        {
            return new Runtime.PipelineStartRuntimeNode
            {
                nextNodeIndex = GetNextNodeIndex(graph)
            };
        }
    }

    /// <summary>
    /// 管线结束节点
    /// </summary>
    [Node("Pipeline End", "RenderGraph")]
    [UseWithGraph(typeof(NewRenderGraph))]
    internal class PipelineEndNode : RenderGraphNode
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_InputPort = context.AddInputPort("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        public override Runtime.RenderGraphRuntimeNode CreateRuntimeNode(NewRenderGraph graph)
        {
            return new Runtime.PipelineEndRuntimeNode();
        }
    }
}
