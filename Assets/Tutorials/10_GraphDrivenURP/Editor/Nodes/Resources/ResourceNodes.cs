using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.GraphDrivenURP
{
    /// <summary>
    /// RenderTexture资源节点
    /// </summary>
    [Node("Render Texture", "URP/Resources", Color = "#2ECC71")]
    [UseWithGraph(typeof(URPGraph))]
    internal class RenderTextureNode : URPNode
    {
        [SerializeField]
        private string m_TextureName = "CustomRT";

        [SerializeField]
        private int m_Width = 1920;

        [SerializeField]
        private int m_Height = 1080;

        [SerializeField]
        private int m_DepthBits = 24;

        [SerializeField]
        private RenderTextureFormat m_Format = RenderTextureFormat.ARGB32;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);
            context.AddOutputPort<RenderTexture>("Texture").Build();
        }

        public override Runtime.URPRuntimeNode CreateRuntimeNode(URPGraph graph)
        {
            var runtimeNode = new Runtime.RenderTextureNode
            {
                textureName = m_TextureName,
                width = m_Width,
                height = m_Height,
                depthBits = m_DepthBits,
                format = m_Format
            };

            var nextNode = GetNextNode(graph);
            runtimeNode.nextNodeIndex = nextNode != null ? nextNode.GetNodeIndex(graph) : -1;

            return runtimeNode;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Texture Name", () => m_TextureName, v => m_TextureName = v).Delayed().Build();
            context.AddOption("Width", () => m_Width, v => m_Width = Mathf.Max(1, v)).Build();
            context.AddOption("Height", () => m_Height, v => m_Height = Mathf.Max(1, v)).Build();
            context.AddOption("Depth Bits", () => m_DepthBits, v => m_DepthBits = v).Build();
            context.AddOption("Format", () => m_Format, v => m_Format = v).Build();
        }
    }

    /// <summary>
    /// Material资源节点
    /// </summary>
    [Node("Material", "URP/Resources", Color = "#E74C3C")]
    [UseWithGraph(typeof(URPGraph))]
    internal class MaterialNode : URPNode
    {
        [SerializeField]
        private Material m_Material;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);
            context.AddOutputPort<Material>("Material").Build();
        }

        public override Runtime.URPRuntimeNode CreateRuntimeNode(URPGraph graph)
        {
            var runtimeNode = new Runtime.MaterialNode
            {
                material = m_Material
            };

            var nextNode = GetNextNode(graph);
            runtimeNode.nextNodeIndex = nextNode != null ? nextNode.GetNodeIndex(graph) : -1;

            return runtimeNode;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Material", () => m_Material, v => m_Material = v).Build();
        }
    }
}
