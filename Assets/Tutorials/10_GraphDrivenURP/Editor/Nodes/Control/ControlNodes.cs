using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.GraphDrivenURP
{
    /// <summary>
    /// 条件分支节点 - 根据质量设置选择不同的渲染路径
    /// </summary>
    [Node("Quality Branch", "URP/Control", Color = "#F39C12")]
    [UseWithGraph(typeof(URPGraph))]
    internal class QualityBranchNode : URPNode
    {
        public enum QualityLevel
        {
            Low,
            Medium,
            High
        }

        [SerializeField]
        private QualityLevel m_MinimumQuality = QualityLevel.Medium;

        private IPort m_HighQualityPort;
        private IPort m_LowQualityPort;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_ExecutionIn = context.AddInputPort("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            m_HighQualityPort = context.AddOutputPort("High Quality")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            m_LowQualityPort = context.AddOutputPort("Low Quality")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        public URPNode GetHighQualityNode(URPGraph graph)
        {
            var connectedPort = graph.GetConnectedInputPort(m_HighQualityPort);
            if (connectedPort != null && connectedPort.Node is URPNode urpNode)
            {
                return urpNode;
            }
            return null;
        }

        public URPNode GetLowQualityNode(URPGraph graph)
        {
            var connectedPort = graph.GetConnectedInputPort(m_LowQualityPort);
            if (connectedPort != null && connectedPort.Node is URPNode urpNode)
            {
                return urpNode;
            }
            return null;
        }

        public override Runtime.URPRuntimeNode CreateRuntimeNode(URPGraph graph)
        {
            var runtimeNode = new Runtime.QualityBranchNode
            {
                minimumQuality = (int)m_MinimumQuality
            };

            var highQualityNode = GetHighQualityNode(graph);
            runtimeNode.highQualityIndex = highQualityNode != null ? highQualityNode.GetNodeIndex(graph) : -1;

            var lowQualityNode = GetLowQualityNode(graph);
            runtimeNode.lowQualityIndex = lowQualityNode != null ? lowQualityNode.GetNodeIndex(graph) : -1;

            return runtimeNode;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Minimum Quality", () => m_MinimumQuality, v => m_MinimumQuality = v).Build();
        }
    }

    /// <summary>
    /// 平台分支节点 - 根据平台选择不同的渲染路径
    /// </summary>
    [Node("Platform Branch", "URP/Control", Color = "#16A085")]
    [UseWithGraph(typeof(URPGraph))]
    internal class PlatformBranchNode : URPNode
    {
        private IPort m_PCPort;
        private IPort m_MobilePort;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_ExecutionIn = context.AddInputPort("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            m_PCPort = context.AddOutputPort("PC")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            m_MobilePort = context.AddOutputPort("Mobile")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        public URPNode GetPCNode(URPGraph graph)
        {
            var connectedPort = graph.GetConnectedInputPort(m_PCPort);
            if (connectedPort != null && connectedPort.Node is URPNode urpNode)
            {
                return urpNode;
            }
            return null;
        }

        public URPNode GetMobileNode(URPGraph graph)
        {
            var connectedPort = graph.GetConnectedInputPort(m_MobilePort);
            if (connectedPort != null && connectedPort.Node is URPNode urpNode)
            {
                return urpNode;
            }
            return null;
        }

        public override Runtime.URPRuntimeNode CreateRuntimeNode(URPGraph graph)
        {
            var runtimeNode = new Runtime.PlatformBranchNode();

            var pcNode = GetPCNode(graph);
            runtimeNode.pcIndex = pcNode != null ? pcNode.GetNodeIndex(graph) : -1;

            var mobileNode = GetMobileNode(graph);
            runtimeNode.mobileIndex = mobileNode != null ? mobileNode.GetNodeIndex(graph) : -1;

            return runtimeNode;
        }
    }
}
