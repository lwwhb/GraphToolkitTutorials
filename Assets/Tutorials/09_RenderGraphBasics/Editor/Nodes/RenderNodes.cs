using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GraphToolkitTutorials.RenderGraphBasics
{
    /// <summary>
    /// 相机节点 - 渲染管线的起点，只有执行输出端口
    /// </summary>
    [Node("Render", "")]
    [UseWithGraph(typeof(RenderGraph))]
    [Serializable]
    internal class CameraNode : RenderNode
    {
        private INodeOption m_CameraTagOption;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // 相机节点只有执行输出，没有执行输入
            m_ExecutionOut = context.AddOutputPort("Out")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_CameraTagOption = context.AddOption<string>("Camera Tag").Delayed().Build();
        }

        public override Runtime.RenderRuntimeNode CreateRuntimeNode(RenderGraph graph)
        {
            string cameraTag = "MainCamera";
            m_CameraTagOption?.TryGetValue(out cameraTag);

            var nextNode = GetNextNode(graph);
            return new Runtime.CameraNode
            {
                cameraTag     = cameraTag ?? "MainCamera",
                nextNodeIndex = nextNode != null ? nextNode.GetNodeIndex(graph) : -1
            };
        }
    }

    /// <summary>
    /// 渲染Pass节点 - 在指定时机执行一个具名渲染Pass
    /// </summary>
    [Node("Render", "")]
    [UseWithGraph(typeof(RenderGraph))]
    [Serializable]
    internal class RenderPassNode : RenderNode
    {
        private INodeOption m_PassNameOption;
        private INodeOption m_PassEventOption;
        private INodeOption m_LayerMaskOption;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_PassNameOption  = context.AddOption<string>("Pass Name").Delayed().Build();
            m_PassEventOption = context.AddOption<RenderPassEvent>("Pass Event").Build();
            m_LayerMaskOption = context.AddOption<LayerMask>("Layer Mask").Build();
        }

        public override Runtime.RenderRuntimeNode CreateRuntimeNode(RenderGraph graph)
        {
            string passName = "CustomPass";
            m_PassNameOption?.TryGetValue(out passName);

            RenderPassEvent passEvent = RenderPassEvent.AfterRenderingOpaques;
            m_PassEventOption?.TryGetValue(out passEvent);

            LayerMask layerMask = -1;
            m_LayerMaskOption?.TryGetValue(out layerMask);

            var nextNode = GetNextNode(graph);
            return new Runtime.RenderPassNode
            {
                passName      = passName ?? "CustomPass",
                passEvent     = passEvent,
                layerMask     = layerMask,
                nextNodeIndex = nextNode != null ? nextNode.GetNodeIndex(graph) : -1
            };
        }
    }

    /// <summary>
    /// Blit节点 - 使用指定 Material 将画面 Blit 到目标
    /// </summary>
    [Node("Render", "")]
    [UseWithGraph(typeof(RenderGraph))]
    [Serializable]
    internal class BlitNode : RenderNode
    {
        private INodeOption m_MaterialOption;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_MaterialOption = context.AddOption<Material>("Material").Build();
        }

        public override Runtime.RenderRuntimeNode CreateRuntimeNode(RenderGraph graph)
        {
            Material material = null;
            m_MaterialOption?.TryGetValue(out material);

            var nextNode = GetNextNode(graph);
            return new Runtime.BlitNode
            {
                material      = material,
                nextNodeIndex = nextNode != null ? nextNode.GetNodeIndex(graph) : -1
            };
        }
    }

    /// <summary>
    /// 输出节点 - 渲染管线的终点
    /// </summary>
    [Node("Render", "")]
    [UseWithGraph(typeof(RenderGraph))]
    [Serializable]
    internal class OutputNode : RenderNode
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_ExecutionIn = context.AddInputPort("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        public override Runtime.RenderRuntimeNode CreateRuntimeNode(RenderGraph graph)
        {
            return new Runtime.OutputNode();
        }
    }
}
