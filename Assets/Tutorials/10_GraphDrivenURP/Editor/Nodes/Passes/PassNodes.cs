using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GraphToolkitTutorials.GraphDrivenURP
{
    /// <summary>
    /// 不透明Pass节点
    /// </summary>
    [Node("Opaque Pass", "URP/Passes", Color = "#4A90D9")]
    [UseWithGraph(typeof(URPGraph))]
    internal class OpaquePassNode : URPNode
    {
        [SerializeField]
        private LayerMask m_LayerMask = -1;

        [SerializeField]
        private bool m_EnableDynamicBatching = true;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);
            context.AddOutputPort<RenderTexture>("Color").Build();
            context.AddOutputPort<RenderTexture>("Depth").Build();
        }

        public override Runtime.URPRuntimeNode CreateRuntimeNode(URPGraph graph)
        {
            var runtimeNode = new Runtime.OpaquePassNode
            {
                layerMask = m_LayerMask,
                enableDynamicBatching = m_EnableDynamicBatching
            };

            var nextNode = GetNextNode(graph);
            runtimeNode.nextNodeIndex = nextNode != null ? nextNode.GetNodeIndex(graph) : -1;

            return runtimeNode;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Layer Mask", () => m_LayerMask, v => m_LayerMask = v).Build();
            context.AddOption("Dynamic Batching", () => m_EnableDynamicBatching, v => m_EnableDynamicBatching = v).Build();
        }
    }

    /// <summary>
    /// 透明Pass节点
    /// </summary>
    [Node("Transparent Pass", "URP/Passes", Color = "#7EC8E3")]
    [UseWithGraph(typeof(URPGraph))]
    internal class TransparentPassNode : URPNode
    {
        [SerializeField]
        private LayerMask m_LayerMask = -1;

        private IPort m_ColorInput;
        private IPort m_DepthInput;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);
            m_ColorInput = context.AddInputPort<RenderTexture>("Color In").Build();
            m_DepthInput = context.AddInputPort<RenderTexture>("Depth In").Build();
            context.AddOutputPort<RenderTexture>("Color Out").Build();
        }

        public override Runtime.URPRuntimeNode CreateRuntimeNode(URPGraph graph)
        {
            var runtimeNode = new Runtime.TransparentPassNode
            {
                layerMask = m_LayerMask
            };

            var nextNode = GetNextNode(graph);
            runtimeNode.nextNodeIndex = nextNode != null ? nextNode.GetNodeIndex(graph) : -1;

            return runtimeNode;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Layer Mask", () => m_LayerMask, v => m_LayerMask = v).Build();
        }
    }

    /// <summary>
    /// 阴影Pass节点
    /// </summary>
    [Node("Shadow Pass", "URP/Passes", Color = "#555555")]
    [UseWithGraph(typeof(URPGraph))]
    internal class ShadowPassNode : URPNode
    {
        [SerializeField]
        private int m_ShadowResolution = 1024;

        [SerializeField]
        private int m_CascadeCount = 4;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);
            context.AddOutputPort<RenderTexture>("Shadow Map").Build();
        }

        public override Runtime.URPRuntimeNode CreateRuntimeNode(URPGraph graph)
        {
            var runtimeNode = new Runtime.ShadowPassNode
            {
                shadowResolution = m_ShadowResolution,
                cascadeCount = m_CascadeCount
            };

            var nextNode = GetNextNode(graph);
            runtimeNode.nextNodeIndex = nextNode != null ? nextNode.GetNodeIndex(graph) : -1;

            return runtimeNode;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Shadow Resolution", () => m_ShadowResolution, v => m_ShadowResolution = Mathf.Max(128, v)).Build();
            context.AddOption("Cascade Count", () => m_CascadeCount, v => m_CascadeCount = Mathf.Clamp(v, 1, 4)).Build();
        }
    }

    /// <summary>
    /// 天空盒Pass节点
    /// </summary>
    [Node("Skybox Pass", "URP/Passes", Color = "#87CEEB")]
    [UseWithGraph(typeof(URPGraph))]
    internal class SkyboxPassNode : URPNode
    {
        private IPort m_ColorInput;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);
            m_ColorInput = context.AddInputPort<RenderTexture>("Color In").Build();
            context.AddOutputPort<RenderTexture>("Color Out").Build();
        }

        public override Runtime.URPRuntimeNode CreateRuntimeNode(URPGraph graph)
        {
            var runtimeNode = new Runtime.SkyboxPassNode();

            var nextNode = GetNextNode(graph);
            runtimeNode.nextNodeIndex = nextNode != null ? nextNode.GetNodeIndex(graph) : -1;

            return runtimeNode;
        }
    }
}
