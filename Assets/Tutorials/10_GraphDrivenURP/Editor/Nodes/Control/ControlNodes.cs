using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.GraphDrivenURP
{
    /// <summary>
    /// 质量分支节点 — 根据当前质量等级选择不同渲染路径。
    /// 教学重点：
    ///   • 分支节点有两个输出端口（High Quality / Low Quality），无执行输出端口
    ///   • GetHighQualityNode / GetLowQualityNode 用 graph.FindNodeForPort 查找目标节点
    ///   • 在 RecordRenderGraph 中通过 QualitySettings.GetQualityLevel() 实时决策
    /// </summary>
    [Node("Control", "")]
    [UseWithGraph(typeof(URPGraph))]
    [Serializable]
    internal class QualityBranchNode : URPNode
    {
        // int 不支持 AddOption getter/setter 形式，改用 INodeOption + TryGetValue
        private INodeOption m_MinQualityOption;

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

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_MinQualityOption = context.AddOption<int>("Min Quality For High").Build();
        }

        private URPNode GetHighQualityNode(URPGraph graph)
        {
            var connectedPort = graph.GetConnectedInputPort(m_HighQualityPort);
            if (connectedPort != null && graph.FindNodeForPort(connectedPort) is URPNode n)
                return n;
            return null;
        }

        private URPNode GetLowQualityNode(URPGraph graph)
        {
            var connectedPort = graph.GetConnectedInputPort(m_LowQualityPort);
            if (connectedPort != null && graph.FindNodeForPort(connectedPort) is URPNode n)
                return n;
            return null;
        }

        public override Runtime.URPRuntimeNode CreateRuntimeNode(URPGraph graph)
        {
            int minQuality = 2;
            m_MinQualityOption?.TryGetValue(out minQuality);
            minQuality = Mathf.Clamp(minQuality, 0, 5);

            var highNode = GetHighQualityNode(graph);
            var lowNode  = GetLowQualityNode(graph);
            return new Runtime.QualityBranchNode
            {
                minimumQualityForHigh = minQuality,
                highQualityIndex      = highNode != null ? highNode.GetNodeIndex(graph) : -1,
                lowQualityIndex       = lowNode  != null ? lowNode.GetNodeIndex(graph)  : -1
            };
        }
    }

    /// <summary>
    /// 平台分支节点 — 根据运行平台选择不同渲染路径。
    /// 教学重点：
    ///   • PC 路径 / Mobile 路径各有独立输出端口
    ///   • 在 RecordRenderGraph 中通过 Application.isMobilePlatform 实时决策
    /// </summary>
    [Node("Control", "")]
    [UseWithGraph(typeof(URPGraph))]
    [Serializable]
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

        private URPNode GetPCNode(URPGraph graph)
        {
            var connectedPort = graph.GetConnectedInputPort(m_PCPort);
            if (connectedPort != null && graph.FindNodeForPort(connectedPort) is URPNode n)
                return n;
            return null;
        }

        private URPNode GetMobileNode(URPGraph graph)
        {
            var connectedPort = graph.GetConnectedInputPort(m_MobilePort);
            if (connectedPort != null && graph.FindNodeForPort(connectedPort) is URPNode n)
                return n;
            return null;
        }

        public override Runtime.URPRuntimeNode CreateRuntimeNode(URPGraph graph)
        {
            var pcNode     = GetPCNode(graph);
            var mobileNode = GetMobileNode(graph);
            return new Runtime.PlatformBranchNode
            {
                pcIndex     = pcNode     != null ? pcNode.GetNodeIndex(graph)     : -1,
                mobileIndex = mobileNode != null ? mobileNode.GetNodeIndex(graph) : -1
            };
        }
    }
}
