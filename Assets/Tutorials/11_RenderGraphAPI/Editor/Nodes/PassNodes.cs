using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GraphToolkitTutorials.RenderGraphAPI
{
    /// <summary>
    /// 不透明物体渲染Pass（新RenderGraph API）
    /// </summary>
    [Node("Opaque Pass (New API)", "RenderGraph/Passes")]
    [UseWithGraph(typeof(NewRenderGraph))]
    internal class OpaquePassNode : RenderGraphNode
    {
        [SerializeField] private bool m_EnableDynamicBatching = true;
        [SerializeField] private bool m_EnableInstancing = true;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Dynamic Batching",
                () => m_EnableDynamicBatching,
                v => m_EnableDynamicBatching = v).Build();

            context.AddOption("GPU Instancing",
                () => m_EnableInstancing,
                v => m_EnableInstancing = v).Build();
        }

        public override Runtime.RenderGraphRuntimeNode CreateRuntimeNode(NewRenderGraph graph)
        {
            return new Runtime.OpaquePassRuntimeNode
            {
                enableDynamicBatching = m_EnableDynamicBatching,
                enableInstancing = m_EnableInstancing,
                nextNodeIndex = GetNextNodeIndex(graph)
            };
        }
    }

    /// <summary>
    /// 透明物体渲染Pass（新RenderGraph API）
    /// </summary>
    [Node("Transparent Pass (New API)", "RenderGraph/Passes")]
    [UseWithGraph(typeof(NewRenderGraph))]
    internal class TransparentPassNode : RenderGraphNode
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);
        }

        public override Runtime.RenderGraphRuntimeNode CreateRuntimeNode(NewRenderGraph graph)
        {
            return new Runtime.TransparentPassRuntimeNode
            {
                nextNodeIndex = GetNextNodeIndex(graph)
            };
        }
    }

    /// <summary>
    /// 清屏Pass（新RenderGraph API）
    /// </summary>
    [Node("Clear Pass (New API)", "RenderGraph/Passes")]
    [UseWithGraph(typeof(NewRenderGraph))]
    internal class ClearPassNode : RenderGraphNode
    {
        [SerializeField] private Color m_ClearColor = Color.black;
        [SerializeField] private bool m_ClearDepth = true;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Clear Color",
                () => m_ClearColor,
                v => m_ClearColor = v).Build();

            context.AddOption("Clear Depth",
                () => m_ClearDepth,
                v => m_ClearDepth = v).Build();
        }

        public override Runtime.RenderGraphRuntimeNode CreateRuntimeNode(NewRenderGraph graph)
        {
            return new Runtime.ClearPassRuntimeNode
            {
                clearColor = m_ClearColor,
                clearDepth = m_ClearDepth,
                nextNodeIndex = GetNextNodeIndex(graph)
            };
        }
    }

    /// <summary>
    /// Blit Pass（新RenderGraph API）- 用于后处理
    /// </summary>
    [Node("Blit Pass (New API)", "RenderGraph/Passes")]
    [UseWithGraph(typeof(NewRenderGraph))]
    internal class BlitPassNode : RenderGraphNode
    {
        [SerializeField] private Material m_Material;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Material",
                () => m_Material,
                v => m_Material = v).Build();
        }

        public override Runtime.RenderGraphRuntimeNode CreateRuntimeNode(NewRenderGraph graph)
        {
            return new Runtime.BlitPassRuntimeNode
            {
                material = m_Material,
                nextNodeIndex = GetNextNodeIndex(graph)
            };
        }
    }
}
