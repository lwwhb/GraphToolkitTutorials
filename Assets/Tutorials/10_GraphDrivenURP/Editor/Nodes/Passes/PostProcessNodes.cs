using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GraphToolkitTutorials.GraphDrivenURP
{
    /// <summary>
    /// 后处理 Pass 节点（标记节点）。
    /// URP Volume 系统管理实际后处理，此节点记录配置选项供参考，
    /// 在 RecordRenderGraph 中作为概念占位符，不发出额外 GPU 命令。
    /// </summary>
    [Node("Passes", "")]
    [UseWithGraph(typeof(URPGraph))]
    [Serializable]
    internal class PostProcessPassNode : URPNode
    {
        private INodeOption m_BloomOption;
        private INodeOption m_TonemappingOption;
        private INodeOption m_VignetteOption;
        private INodeOption m_ColorGradingOption;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_BloomOption        = context.AddOption<bool>("Bloom").Build();
            m_TonemappingOption  = context.AddOption<bool>("Tonemapping").Build();
            m_VignetteOption     = context.AddOption<bool>("Vignette").Build();
            m_ColorGradingOption = context.AddOption<bool>("Color Grading").Build();
        }

        public override Runtime.URPRuntimeNode CreateRuntimeNode(URPGraph graph)
        {
            bool enableBloom = true;        m_BloomOption?.TryGetValue(out enableBloom);
            bool enableTonemapping = true;  m_TonemappingOption?.TryGetValue(out enableTonemapping);
            bool enableVignette = false;    m_VignetteOption?.TryGetValue(out enableVignette);
            bool enableColorGrading = true; m_ColorGradingOption?.TryGetValue(out enableColorGrading);

            var nextNode = GetNextNode(graph);
            return new Runtime.PostProcessPassNode
            {
                enableBloom        = enableBloom,
                enableTonemapping  = enableTonemapping,
                enableVignette     = enableVignette,
                enableColorGrading = enableColorGrading,
                nextNodeIndex      = nextNode != null ? nextNode.GetNodeIndex(graph) : -1
            };
        }
    }

    /// <summary>
    /// 自定义 Pass 节点。
    /// 教学重点：
    ///   • 运行时对应 RecordRenderGraph 中的 Blitter.BlitTexture 双 Pass 模式
    ///   • Material 的 Shader 通过 _BlitTexture uniform 采样源纹理
    /// </summary>
    [Node("Custom Pass", "URP/Passes")]
    [UseWithGraph(typeof(URPGraph))]
    [Serializable]
    internal class CustomPassNode : URPNode
    {
        private INodeOption m_PassNameOption;
        private INodeOption m_MaterialOption;
        private INodeOption m_PassEventOption;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_PassNameOption  = context.AddOption<string>("Pass Name").Delayed().Build();
            m_MaterialOption  = context.AddOption<Material>("Material").Build();
            m_PassEventOption = context.AddOption<RenderPassEvent>("Pass Event").Build();
        }

        public override Runtime.URPRuntimeNode CreateRuntimeNode(URPGraph graph)
        {
            string passName = "CustomPass";
            m_PassNameOption?.TryGetValue(out passName);

            Material material = null;
            m_MaterialOption?.TryGetValue(out material);

            RenderPassEvent passEvent = RenderPassEvent.AfterRenderingOpaques;
            m_PassEventOption?.TryGetValue(out passEvent);

            var nextNode = GetNextNode(graph);
            return new Runtime.CustomPassNode
            {
                passName      = passName ?? "CustomPass",
                material      = material,
                passEvent     = passEvent,
                nextNodeIndex = nextNode != null ? nextNode.GetNodeIndex(graph) : -1
            };
        }
    }
}
