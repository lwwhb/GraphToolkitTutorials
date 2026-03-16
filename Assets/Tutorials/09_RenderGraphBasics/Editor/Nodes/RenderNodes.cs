using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GraphToolkitTutorials.RenderGraphBasics
{
    /// <summary>
    /// 相机节点 - 渲染管线的起点
    /// </summary>
    [Node("Camera", "Render")]
    [UseWithGraph(typeof(RenderGraph))]
    internal class CameraNode : RenderNode
    {
        [SerializeField]
        private string m_CameraTag = "MainCamera";

        private IPort m_ColorOutput;
        private IPort m_DepthOutput;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // 相机节点只有输出端口
            m_ExecutionOut = context.AddOutputPort("Out")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            m_ColorOutput = context.AddOutputPort<RenderTexture>("Color").Build();
            m_DepthOutput = context.AddOutputPort<RenderTexture>("Depth").Build();
        }

        public override Runtime.RenderRuntimeNode CreateRuntimeNode(RenderGraph graph)
        {
            var runtimeNode = new Runtime.CameraNode
            {
                cameraTag = m_CameraTag
            };

            var nextNode = GetNextNode(graph);
            if (nextNode != null)
            {
                runtimeNode.nextNodeIndex = nextNode.GetNodeIndex(graph);
            }
            else
            {
                runtimeNode.nextNodeIndex = -1;
            }

            return runtimeNode;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Camera Tag", () => m_CameraTag, v => m_CameraTag = v)
                .Delayed()
                .Build();
        }
    }

    /// <summary>
    /// 渲染Pass节点 - 执行渲染Pass
    /// </summary>
    [Node("Render Pass", "Render")]
    [UseWithGraph(typeof(RenderGraph))]
    internal class RenderPassNode : RenderNode
    {
        [SerializeField]
        private string m_PassName = "CustomPass";

        [SerializeField]
        private RenderPassEvent m_PassEvent = RenderPassEvent.AfterRenderingOpaques;

        [SerializeField]
        private LayerMask m_LayerMask = -1;

        private IPort m_ColorInput;
        private IPort m_DepthInput;
        private IPort m_ColorOutput;
        private IPort m_DepthOutput;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);

            // 输入纹理
            m_ColorInput = context.AddInputPort<RenderTexture>("Color In").Build();
            m_DepthInput = context.AddInputPort<RenderTexture>("Depth In").Build();

            // 输出纹理
            m_ColorOutput = context.AddOutputPort<RenderTexture>("Color Out").Build();
            m_DepthOutput = context.AddOutputPort<RenderTexture>("Depth Out").Build();
        }

        public override Runtime.RenderRuntimeNode CreateRuntimeNode(RenderGraph graph)
        {
            var runtimeNode = new Runtime.RenderPassNode
            {
                passName = m_PassName,
                passEvent = m_PassEvent,
                layerMask = m_LayerMask
            };

            var nextNode = GetNextNode(graph);
            if (nextNode != null)
            {
                runtimeNode.nextNodeIndex = nextNode.GetNodeIndex(graph);
            }
            else
            {
                runtimeNode.nextNodeIndex = -1;
            }

            return runtimeNode;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Pass Name", () => m_PassName, v => m_PassName = v)
                .Delayed()
                .Build();

            context.AddOption("Pass Event", () => m_PassEvent, v => m_PassEvent = v).Build();
            context.AddOption("Layer Mask", () => m_LayerMask, v => m_LayerMask = v).Build();
        }
    }

    /// <summary>
    /// Blit节点 - 将纹理复制到另一个纹理
    /// </summary>
    [Node("Blit", "Render")]
    [UseWithGraph(typeof(RenderGraph))]
    internal class BlitNode : RenderNode
    {
        [SerializeField]
        private Material m_Material;

        private IPort m_SourceInput;
        private IPort m_DestinationInput;
        private IPort m_Output;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);

            m_SourceInput = context.AddInputPort<RenderTexture>("Source").Build();
            m_DestinationInput = context.AddInputPort<RenderTexture>("Destination").Build();
            m_Output = context.AddOutputPort<RenderTexture>("Output").Build();
        }

        public override Runtime.RenderRuntimeNode CreateRuntimeNode(RenderGraph graph)
        {
            var runtimeNode = new Runtime.BlitNode
            {
                material = m_Material
            };

            var nextNode = GetNextNode(graph);
            if (nextNode != null)
            {
                runtimeNode.nextNodeIndex = nextNode.GetNodeIndex(graph);
            }
            else
            {
                runtimeNode.nextNodeIndex = -1;
            }

            return runtimeNode;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Material", () => m_Material, v => m_Material = v).Build();
        }
    }

    /// <summary>
    /// 输出节点 - 渲染管线的终点
    /// </summary>
    [Node("Output", "Render")]
    [UseWithGraph(typeof(RenderGraph))]
    internal class OutputNode : RenderNode
    {
        private IPort m_ColorInput;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_ExecutionIn = context.AddInputPort("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            m_ColorInput = context.AddInputPort<RenderTexture>("Final Color").Build();
        }

        public override Runtime.RenderRuntimeNode CreateRuntimeNode(RenderGraph graph)
        {
            return new Runtime.OutputNode();
        }
    }
}
