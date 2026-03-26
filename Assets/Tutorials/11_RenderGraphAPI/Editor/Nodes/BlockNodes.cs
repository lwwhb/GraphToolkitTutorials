using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GraphToolkitTutorials.RenderGraphAPI
{
    /// <summary>
    /// 清屏操作块节点 — 清除颜色/深度缓冲。
    ///
    /// 教学要点（BlockNode 核心模式）：
    ///   • BlockNode 通过 port.TryGetValue 读取内联常量（无连线）或上游连线值
    ///   • [UseWithContext] 限制此 BlockNode 只能拖入 RenderPassNode
    ///   • 对应运行时：ctx.cmd.ClearRenderTarget(clearDepth, true, clearColor)
    /// </summary>
    [Node("Operations", "")]
    [UseWithContext(typeof(RenderPassNode))]
    [Serializable]
    internal class ClearBlockNode : BlockNode
    {
        private IPort m_ClearColor;
        private IPort m_ClearDepth;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_ClearColor = context.AddInputPort<Color>("Clear Color").Build();
            m_ClearDepth = context.AddInputPort<bool>("Clear Depth").Build();
        }

        public Runtime.ClearOperation CreateOperation()
        {
            Color color = Color.black;
            m_ClearColor?.TryGetValue<Color>(out color);
            bool clearDepth = true;
            m_ClearDepth?.TryGetValue<bool>(out clearDepth);
            return new Runtime.ClearOperation { clearColor = color, clearDepth = clearDepth };
        }
    }

    /// <summary>
    /// 不透明物体绘制块节点。
    ///
    /// 教学要点：
    ///   • LayerMask 通过端口 TryGetValue 读取，支持编辑器内联设置
    ///   • 对应运行时 DrawRendererList(opaque) + SortingCriteria.CommonOpaque
    /// </summary>
    [Node("Operations", "")]
    [UseWithContext(typeof(RenderPassNode))]
    [Serializable]
    internal class DrawOpaqueBlockNode : BlockNode
    {
        private IPort m_LayerMask;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_LayerMask = context.AddInputPort<LayerMask>("Layer Mask").Build();
        }

        public Runtime.DrawOpaqueOperation CreateOperation()
        {
            LayerMask mask = ~0; // Everything
            m_LayerMask?.TryGetValue<LayerMask>(out mask);
            return new Runtime.DrawOpaqueOperation { layerMask = mask };
        }
    }

    /// <summary>
    /// 透明物体绘制块节点。
    ///
    /// 教学要点：
    ///   • 对应运行时 DrawRendererList(transparent) + SortingCriteria.CommonTransparent
    ///   • 透明物体必须在不透明物体之后绘制（后到前排序）
    /// </summary>
    [Node("Operations", "")]
    [UseWithContext(typeof(RenderPassNode))]
    [Serializable]
    internal class DrawTransparentBlockNode : BlockNode
    {
        private IPort m_LayerMask;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_LayerMask = context.AddInputPort<LayerMask>("Layer Mask").Build();
        }

        public Runtime.DrawTransparentOperation CreateOperation()
        {
            LayerMask mask = ~0;
            m_LayerMask?.TryGetValue<LayerMask>(out mask);
            return new Runtime.DrawTransparentOperation { layerMask = mask };
        }
    }

    /// <summary>
    /// Blit 后处理块节点 — 使用 INodeOption 持有 Material 引用。
    ///
    /// 教学要点：
    ///   • Material 是 UnityEngine.Object 引用类型，用 INodeOption 而非端口常量
    ///   • Blit 需要双 Pass（源纹理不能同时 UseTexture 读 + SetRenderAttachment 写）
    ///   • INodeOption 通过 TryGetValue 安全读取，支持运行时未赋值的情况
    /// </summary>
    [Node("Operations", "")]
    [UseWithContext(typeof(RenderPassNode))]
    [Serializable]
    internal class BlitBlockNode : BlockNode
    {
        private INodeOption m_Material;

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_Material = context.AddOption<Material>("Material").Build();
        }

        public Runtime.BlitOperation CreateOperation()
        {
            Material mat = null;
            m_Material?.TryGetValue<Material>(out mat);
            return new Runtime.BlitOperation { material = mat };
        }
    }
}
