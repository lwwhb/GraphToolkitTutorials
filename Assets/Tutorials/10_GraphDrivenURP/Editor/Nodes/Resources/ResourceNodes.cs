using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.GraphDrivenURP
{
    /// <summary>
    /// RenderTexture 资源节点。
    /// 教学重点：在 RenderGraph 系统中，纹理由 RenderGraph 内部管理（CreateTexture / GetTextureDesc），
    /// 此节点作为概念声明占位符，在 RecordRenderGraph 中不直接创建 RenderTexture。
    ///
    /// 注意：string / int / RenderTextureFormat 使用 INodeOption + TryGetValue 模式，
    /// 因为 AddOption 的 getter/setter（3参数）形式不支持这些类型。
    /// </summary>
    [Node("Resources", "")]
    [UseWithGraph(typeof(URPGraph))]
    [Serializable]
    internal class RenderTextureNode : URPNode
    {
        private INodeOption m_TextureNameOption;
        private INodeOption m_WidthOption;
        private INodeOption m_HeightOption;
        private INodeOption m_DepthBitsOption;
        private INodeOption m_FormatOption;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_TextureNameOption = context.AddOption<string>("Texture Name").Delayed().Build();
            m_WidthOption       = context.AddOption<int>("Width").Build();
            m_HeightOption      = context.AddOption<int>("Height").Build();
            m_DepthBitsOption   = context.AddOption<int>("Depth Bits").Build();
            m_FormatOption      = context.AddOption<RenderTextureFormat>("Format").Build();
        }

        public override Runtime.URPRuntimeNode CreateRuntimeNode(URPGraph graph)
        {
            string textureName = "CustomRT";
            m_TextureNameOption?.TryGetValue(out textureName);

            int width = 1920; m_WidthOption?.TryGetValue(out width);
            int height = 1080; m_HeightOption?.TryGetValue(out height);
            int depthBits = 24; m_DepthBitsOption?.TryGetValue(out depthBits);

            var format = RenderTextureFormat.ARGB32;
            m_FormatOption?.TryGetValue(out format);

            var nextNode = GetNextNode(graph);
            return new Runtime.RenderTextureNode
            {
                textureName   = textureName ?? "CustomRT",
                width         = Mathf.Max(1, width),
                height        = Mathf.Max(1, height),
                depthBits     = depthBits,
                format        = format,
                nextNodeIndex = nextNode != null ? nextNode.GetNodeIndex(graph) : -1
            };
        }
    }
}
