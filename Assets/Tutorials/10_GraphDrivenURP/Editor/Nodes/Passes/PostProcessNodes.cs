using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.GraphDrivenURP
{
    /// <summary>
    /// 后处理Pass节点
    /// </summary>
    [Node("Post Process", "URP/Passes", Color = "#E8A838")]
    [UseWithGraph(typeof(URPGraph))]
    internal class PostProcessPassNode : URPNode
    {
        [SerializeField]
        private bool m_EnableBloom = true;

        [SerializeField]
        private bool m_EnableTonemapping = true;

        [SerializeField]
        private bool m_EnableVignette = false;

        [SerializeField]
        private bool m_EnableColorGrading = true;

        private IPort m_ColorInput;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);
            m_ColorInput = context.AddInputPort<RenderTexture>("Color In").Build();
            context.AddOutputPort<RenderTexture>("Color Out").Build();
        }

        public override Runtime.URPRuntimeNode CreateRuntimeNode(URPGraph graph)
        {
            var runtimeNode = new Runtime.PostProcessPassNode
            {
                enableBloom = m_EnableBloom,
                enableTonemapping = m_EnableTonemapping,
                enableVignette = m_EnableVignette,
                enableColorGrading = m_EnableColorGrading
            };

            var nextNode = GetNextNode(graph);
            runtimeNode.nextNodeIndex = nextNode != null ? nextNode.GetNodeIndex(graph) : -1;

            return runtimeNode;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Bloom", () => m_EnableBloom, v => m_EnableBloom = v).Build();
            context.AddOption("Tonemapping", () => m_EnableTonemapping, v => m_EnableTonemapping = v).Build();
            context.AddOption("Vignette", () => m_EnableVignette, v => m_EnableVignette = v).Build();
            context.AddOption("Color Grading", () => m_EnableColorGrading, v => m_EnableColorGrading = v).Build();
        }
    }

    /// <summary>
    /// 自定义Pass节点
    /// </summary>
    [Node("Custom Pass", "URP/Passes", Color = "#9B59B6")]
    [UseWithGraph(typeof(URPGraph))]
    internal class CustomPassNode : URPNode
    {
        [SerializeField]
        private string m_PassName = "CustomPass";

        [SerializeField]
        private Material m_Material;

        [SerializeField]
        private UnityEngine.Rendering.Universal.RenderPassEvent m_PassEvent =
            UnityEngine.Rendering.Universal.RenderPassEvent.AfterRenderingOpaques;

        private IPort m_ColorInput;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);
            m_ColorInput = context.AddInputPort<RenderTexture>("Color In").Build();
            context.AddOutputPort<RenderTexture>("Color Out").Build();
        }

        public override Runtime.URPRuntimeNode CreateRuntimeNode(URPGraph graph)
        {
            var runtimeNode = new Runtime.CustomPassNode
            {
                passName = m_PassName,
                material = m_Material,
                passEvent = m_PassEvent
            };

            var nextNode = GetNextNode(graph);
            runtimeNode.nextNodeIndex = nextNode != null ? nextNode.GetNodeIndex(graph) : -1;

            return runtimeNode;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Pass Name", () => m_PassName, v => m_PassName = v).Delayed().Build();
            context.AddOption("Material", () => m_Material, v => m_Material = v).Build();
            context.AddOption("Pass Event", () => m_PassEvent, v => m_PassEvent = v).Build();
        }
    }
}
