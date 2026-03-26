using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GraphToolkitTutorials.GraphDrivenURP
{
    /// <summary>
    /// 不透明 Pass 节点。
    /// 教学重点：
    ///   • 纯执行流节点：只有 In/Out 执行端口，无数据端口
    ///   • 运行时对应 RecordRenderGraph 中的 DrawRendererList（不透明队列）
    ///   • 选项使用 INodeOption + TryGetValue（AddOption getter/setter 形式不被支持）
    /// </summary>
    [Node("Passes", "")]
    [UseWithGraph(typeof(URPGraph))]
    [Serializable]
    internal class OpaquePassNode : URPNode
    {
        private INodeOption m_LayerMaskOption;
        private INodeOption m_PassEventOption;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_LayerMaskOption = context.AddOption<LayerMask>("Layer Mask").Build();
            m_PassEventOption = context.AddOption<RenderPassEvent>("Pass Event").Build();
        }

        public override Runtime.URPRuntimeNode CreateRuntimeNode(URPGraph graph)
        {
            LayerMask layerMask = -1;
            m_LayerMaskOption?.TryGetValue(out layerMask);

            RenderPassEvent passEvent = RenderPassEvent.AfterRenderingOpaques;
            m_PassEventOption?.TryGetValue(out passEvent);

            var nextNode = GetNextNode(graph);
            return new Runtime.OpaquePassNode
            {
                layerMask     = layerMask,
                passEvent     = passEvent,
                nextNodeIndex = nextNode != null ? nextNode.GetNodeIndex(graph) : -1
            };
        }
    }

    /// <summary>
    /// 透明 Pass 节点。
    /// 运行时对应 RecordRenderGraph 中的 DrawRendererList（透明队列）。
    /// </summary>
    [Node("Passes", "")]
    [UseWithGraph(typeof(URPGraph))]
    [Serializable]
    internal class TransparentPassNode : URPNode
    {
        private INodeOption m_LayerMaskOption;
        private INodeOption m_PassEventOption;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_LayerMaskOption = context.AddOption<LayerMask>("Layer Mask").Build();
            m_PassEventOption = context.AddOption<RenderPassEvent>("Pass Event").Build();
        }

        public override Runtime.URPRuntimeNode CreateRuntimeNode(URPGraph graph)
        {
            LayerMask layerMask = -1;
            m_LayerMaskOption?.TryGetValue(out layerMask);

            RenderPassEvent passEvent = RenderPassEvent.AfterRenderingTransparents;
            m_PassEventOption?.TryGetValue(out passEvent);

            var nextNode = GetNextNode(graph);
            return new Runtime.TransparentPassNode
            {
                layerMask     = layerMask,
                passEvent     = passEvent,
                nextNodeIndex = nextNode != null ? nextNode.GetNodeIndex(graph) : -1
            };
        }
    }

    /// <summary>
    /// 阴影 Pass 节点（标记节点）。
    /// URP 内部管理阴影渲染，此节点在 RecordRenderGraph 中作为概念占位符。
    /// </summary>
    [Node("Passes", "")]
    [UseWithGraph(typeof(URPGraph))]
    [Serializable]
    internal class ShadowPassNode : URPNode
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);
        }

        public override Runtime.URPRuntimeNode CreateRuntimeNode(URPGraph graph)
        {
            var nextNode = GetNextNode(graph);
            return new Runtime.ShadowPassNode
            {
                nextNodeIndex = nextNode != null ? nextNode.GetNodeIndex(graph) : -1
            };
        }
    }

    /// <summary>
    /// 天空盒 Pass 节点（标记节点）。
    /// URP 内部管理天空盒渲染，此节点在 RecordRenderGraph 中作为概念占位符。
    /// </summary>
    [Node("Passes", "")]
    [UseWithGraph(typeof(URPGraph))]
    [Serializable]
    internal class SkyboxPassNode : URPNode
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);
        }

        public override Runtime.URPRuntimeNode CreateRuntimeNode(URPGraph graph)
        {
            var nextNode = GetNextNode(graph);
            return new Runtime.SkyboxPassNode
            {
                nextNodeIndex = nextNode != null ? nextNode.GetNodeIndex(graph) : -1
            };
        }
    }
}
