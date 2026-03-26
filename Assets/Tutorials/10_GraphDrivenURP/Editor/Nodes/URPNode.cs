using System;
using Unity.GraphToolkit.Editor;

namespace GraphToolkitTutorials.GraphDrivenURP
{
    /// <summary>
    /// URP节点基类。
    /// 教学重点：
    ///   • GetNextNode 使用 graph.FindNodeForPort（IPort 无 .Node 属性，必须遍历查找）
    ///   • GetNodeIndex 只计数 URPNode 实例，与 CreateRuntimeGraph 过滤逻辑对齐
    /// </summary>
    [Serializable]
    internal abstract class URPNode : Node
    {
        protected IPort m_ExecutionIn;
        protected IPort m_ExecutionOut;

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
        /// 获取执行流中的下一个节点。
        /// 注意：必须用 graph.FindNodeForPort，不能用 connectedPort.Node（IPort 无此属性）。
        /// </summary>
        public URPNode GetNextNode(URPGraph graph)
        {
            var connectedPort = graph.GetConnectedInputPort(m_ExecutionOut);
            if (connectedPort != null && graph.FindNodeForPort(connectedPort) is URPNode urpNode)
                return urpNode;
            return null;
        }

        /// <summary>创建对应的运行时节点数据。</summary>
        public abstract Runtime.URPRuntimeNode CreateRuntimeNode(URPGraph graph);

        /// <summary>
        /// 获取此节点在运行时图（runtimeGraph.nodes）中的整数索引。
        /// 注意：只计数 URPNode 实例，与 CreateRuntimeGraph 中的过滤逻辑保持一致。
        ///       若使用 GetNodes() 全列表索引，遇到非 URPNode 元素时索引会错位。
        /// </summary>
        public int GetNodeIndex(URPGraph graph)
        {
            int idx = 0;
            foreach (var node in graph.GetNodes())
            {
                if (node is URPNode)
                {
                    if (node == this) return idx;
                    idx++;
                }
            }
            return -1;
        }
    }
}
