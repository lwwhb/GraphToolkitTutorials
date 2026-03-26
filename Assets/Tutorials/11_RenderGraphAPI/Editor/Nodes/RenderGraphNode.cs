using System;
using Unity.GraphToolkit.Editor;

namespace GraphToolkitTutorials.RenderGraphAPI
{
    /// <summary>
    /// RenderGraph 节点基类 — 继承 ContextNode，可容纳 BlockNode 子节点。
    ///
    /// 教学要点：
    ///   • ContextNode 与 Node 的区别：ContextNode 可包含 BlockNode 子节点
    ///   • Graph.GetNodes() 不包含 BlockNode，必须通过 ContextNode.BlockNodes 访问
    ///   • GetNextNodeIndex 使用 graph.FindNodeForPort（IPort 无 .Node 属性，必须遍历查找）
    ///   • GetNodeIndex 只计数 RenderGraphNode 实例，与 CreateRuntime 过滤逻辑对齐
    ///   • 若用 GetNodes() 全列表索引，遇到非 RenderGraphNode 元素时索引会错位
    /// </summary>
    [Serializable]
    internal abstract class RenderGraphNode : ContextNode
    {
        protected IPort m_InputPort;
        protected IPort m_OutputPort;

        protected void AddExecutionPorts(IPortDefinitionContext context)
        {
            m_InputPort = context.AddInputPort("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            m_OutputPort = context.AddOutputPort("Out")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        /// <summary>创建对应的运行时节点数据。</summary>
        public abstract Runtime.RenderGraphRuntimeNode CreateRuntimeNode(NewRenderGraph graph);

        /// <summary>
        /// 获取此节点在运行时图（nodes 列表）中的整数索引。
        /// 注意：只计数 RenderGraphNode 实例，与 CreateRuntime 中的过滤逻辑保持一致。
        /// </summary>
        public int GetNodeIndex(NewRenderGraph graph)
        {
            int idx = 0;
            foreach (var node in graph.GetNodes())
            {
                if (node is RenderGraphNode)
                {
                    if (node == this) return idx;
                    idx++;
                }
            }
            return -1;
        }

        /// <summary>
        /// 获取执行流中下一个节点的索引。
        /// 注意：必须用 graph.FindNodeForPort，不能用 connectedPort.Node（IPort 无此属性）。
        /// </summary>
        public int GetNextNodeIndex(NewRenderGraph graph)
        {
            if (m_OutputPort == null) return -1;
            var connectedPort = graph.GetConnectedInputPort(m_OutputPort);
            if (connectedPort != null && graph.FindNodeForPort(connectedPort) is RenderGraphNode nextNode)
                return nextNode.GetNodeIndex(graph);
            return -1;
        }
    }

    /// <summary>
    /// 管线起始节点 — 只有 Out 执行端口，不包含 BlockNode。
    /// </summary>
    [Node("Pipeline Start", "RenderGraph")]
    [UseWithGraph(typeof(NewRenderGraph))]
    [Serializable]
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
    /// 管线结束节点 — 只有 In 执行端口，不包含 BlockNode。
    /// </summary>
    [Node("Pipeline End", "RenderGraph")]
    [UseWithGraph(typeof(NewRenderGraph))]
    [Serializable]
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
