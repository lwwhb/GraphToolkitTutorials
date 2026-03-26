using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RendererUtils;

namespace GraphToolkitTutorials.RenderGraphAPI.Runtime
{
    /// <summary>
    /// 新 RenderGraph API 驱动的 URP 渲染特性。
    ///
    /// 与 Tutorial 10 的设计对比：
    ///   T10：每种操作对应独立节点 → 每个节点 = 一个 AddRasterRenderPass
    ///   T11：RenderPassNode（ContextNode）内嵌多个 BlockNode
    ///        → 同一节点内的 Clear + Draw 合并为一个 AddRasterRenderPass
    ///        → Blit 操作因读写约束仍需独立双 Pass
    ///
    /// 配置步骤：
    ///   1. URP Renderer Inspector → Add Renderer Feature → New RenderGraph Feature
    ///   2. 将 .newrendergraph 资产（生成的 NewRenderGraphRuntime）拖入 Graph Asset 字段
    /// </summary>
    public class NewRenderGraphFeature : ScriptableRendererFeature
    {
        [SerializeField] private NewRenderGraphRuntime m_GraphAsset;

        private NewRenderGraphPass m_Pass;

        public override void Create()
        {
            if (m_GraphAsset != null)
                m_Pass = new NewRenderGraphPass(m_GraphAsset);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_GraphAsset == null) return;

            // 延迟初始化：Create() 可能在 m_GraphAsset 尚未反序列化时被调用
            if (m_Pass == null)
                m_Pass = new NewRenderGraphPass(m_GraphAsset);

            renderer.EnqueuePass(m_Pass);
        }

        protected override void Dispose(bool disposing)
        {
            m_Pass?.Dispose();
        }
    }

    /// <summary>
    /// 新 RenderGraph API 渲染 Pass。
    ///
    /// 核心架构（每个 RenderPassDataRuntimeNode 的处理策略）：
    ///   1. 非 Blit 操作（Clear、DrawOpaque、DrawTransparent）合并进一个 AddRasterRenderPass
    ///      → 合并减少 SetRenderTarget 切换，提高 GPU 效率
    ///   2. 每个 Blit 操作生成独立双 Pass（Pass A: activeColor→temp，Pass B: temp→activeColor）
    ///      → 同一 RasterPass 内不能对同一纹理同时 UseTexture（读）和 SetRenderAttachment（写）
    ///
    /// Frame Debugger 可见性：
    ///   AddRasterRenderPass + SetRenderAttachment 触发 RenderGraph 自动插入 SetRenderTarget，
    ///   该命令使 Pass 在 Frame Debugger 中出现独立条目。
    ///   AllowPassCulling(false) 防止 RenderGraph 剔除无下游读者的 Pass。
    /// </summary>
    internal class NewRenderGraphPass : ScriptableRenderPass
    {
        private readonly NewRenderGraphRuntime m_Graph;

        private static readonly ShaderTagId[] k_ShaderTagIds =
        {
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly"),
        };

        // ─── Pass Data ────────────────────────────────────────────────────────────
        // DrawPassData 通过 RenderGraph Pass 生命周期传递，避免直接闭包捕获外部变量

        private class DrawPassData
        {
            public List<PassOperation> operations;
            public RendererListHandle  opaqueList;
            public bool                hasOpaqueList;
            public RendererListHandle  transparentList;
            public bool                hasTransparentList;
        }

        private class BlitPassData
        {
            public TextureHandle sourceTexture;
            public Material      material;
        }

        public NewRenderGraphPass(NewRenderGraphRuntime graph)
        {
            m_Graph         = graph;
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }

        /// <summary>
        /// RecordRenderGraph：遍历运行时图，每个 RenderPassDataRuntimeNode 生成一组 GPU Pass。
        ///
        /// 与 T10 RecordRenderGraph 的对比：
        ///   T10：switch(nodeType) → 逐节点调用 AddRasterRenderPass
        ///   T11：switch(nodeType) → RenderPassDataRuntimeNode 调用 RecordRenderPassNode
        ///        RecordRenderPassNode 遍历 operations 列表，按类型分组处理
        ///
        /// ContextContainer 三个数据对象（与 T10 完全一致）：
        ///   UniversalResourceData  → 当前帧颜色/深度 TextureHandle
        ///   UniversalCameraData    → Camera 对象（DrawRendererList 排序需要）
        ///   UniversalRenderingData → CullingResults（DrawRendererList 裁剪需要）
        /// </summary>
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Graph == null) return;

            var startNode = m_Graph.GetStartNode();
            if (startNode == null)
            {
                Debug.LogWarning("[NewRenderGraph] No PipelineStartNode in runtime graph. " +
                                 "Re-import the .newrendergraph asset.");
                return;
            }

            var resourceData  = frameData.Get<UniversalResourceData>();
            var cameraData    = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();

            if (Time.frameCount <= 2)
                Debug.Log($"[NewRenderGraph] Recording: {m_Graph.nodes.Count} nodes, " +
                          $"startIndex={m_Graph.startNodeIndex}, " +
                          $"activeColorValid={resourceData.activeColorTexture.IsValid()}");

            int current = startNode.nextNodeIndex;
            while (current >= 0)
            {
                var node = m_Graph.GetNode(current);
                if (node == null) break;

                if (node is RenderPassDataRuntimeNode pass)
                {
                    RecordRenderPassNode(renderGraph, resourceData, cameraData, renderingData, pass);
                    current = pass.nextNodeIndex;
                }
                else if (node is PipelineEndRuntimeNode)
                {
                    break;
                }
                else
                {
                    current = node.nextNodeIndex;
                }
            }
        }

        /// <summary>
        /// 为一个 RenderPassDataRuntimeNode 录制 Pass。
        ///
        /// 分组策略（教学重点）：
        ///   非 Blit 操作 → 合并为一个 AddRasterRenderPass（减少 SetRenderTarget 切换）
        ///   Blit 操作    → 每个独立双 Pass（读写约束）
        /// </summary>
        private void RecordRenderPassNode(
            RenderGraph            rg,
            UniversalResourceData  resourceData,
            UniversalCameraData    cameraData,
            UniversalRenderingData renderingData,
            RenderPassDataRuntimeNode passNode)
        {
            var drawOps = new List<PassOperation>();
            var blitOps = new List<BlitOperation>();

            foreach (var op in passNode.operations)
            {
                if (op is BlitOperation blit) blitOps.Add(blit);
                else drawOps.Add(op);
            }

            if (drawOps.Count > 0)
                RecordDrawCombinedPass(rg, resourceData, cameraData, renderingData, drawOps);

            foreach (var blitOp in blitOps)
                if (blitOp.material != null)
                    RecordBlitPass(rg, resourceData, blitOp);
        }

        /// <summary>
        /// 将 Clear + DrawOpaque + DrawTransparent 合并到一个 RasterRenderPass。
        ///
        /// Frame Debugger 中将显示为 "Graph: Render Pass" 单个条目，
        /// 包含 Clear + DrawOpaqueObjects + DrawTransparentObjects 三个子命令。
        /// </summary>
        private void RecordDrawCombinedPass(
            RenderGraph            rg,
            UniversalResourceData  resourceData,
            UniversalCameraData    cameraData,
            UniversalRenderingData renderingData,
            List<PassOperation>    ops)
        {
            if (!resourceData.activeColorTexture.IsValid())
            {
                Debug.LogWarning("[NewRenderGraph] activeColorTexture is invalid. " +
                                 "Ensure a URP Renderer with this feature is active for the camera.");
                return;
            }

            using var builder = rg.AddRasterRenderPass<DrawPassData>("Graph: Render Pass", out var pd);

            pd.operations        = ops;
            pd.hasOpaqueList     = false;
            pd.hasTransparentList = false;

            // 提前创建 RendererListHandle（必须在 Pass 外创建，不能在 SetRenderFunc 内创建）
            foreach (var op in ops)
            {
                if (op is DrawOpaqueOperation opaqueOp)
                {
                    var desc = new RendererListDesc(k_ShaderTagIds, renderingData.cullResults, cameraData.camera)
                    {
                        sortingCriteria  = SortingCriteria.CommonOpaque,
                        renderQueueRange = RenderQueueRange.opaque,
                        layerMask        = opaqueOp.layerMask,
                    };
                    pd.opaqueList    = rg.CreateRendererList(desc);
                    pd.hasOpaqueList = true;
                    builder.UseRendererList(pd.opaqueList);
                }
                else if (op is DrawTransparentOperation transparentOp)
                {
                    var desc = new RendererListDesc(k_ShaderTagIds, renderingData.cullResults, cameraData.camera)
                    {
                        sortingCriteria  = SortingCriteria.CommonTransparent,
                        renderQueueRange = RenderQueueRange.transparent,
                        layerMask        = transparentOp.layerMask,
                    };
                    pd.transparentList    = rg.CreateRendererList(desc);
                    pd.hasTransparentList = true;
                    builder.UseRendererList(pd.transparentList);
                }
            }

            builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
            if (resourceData.activeDepthTexture.IsValid())
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc((DrawPassData data, RasterGraphContext ctx) =>
            {
                foreach (var op in data.operations)
                {
                    if (op is ClearOperation clear)
                    {
                        ctx.cmd.ClearRenderTarget(clear.clearDepth, true, clear.clearColor);
                    }
                    else if (op is DrawOpaqueOperation && data.hasOpaqueList)
                    {
                        ctx.cmd.DrawRendererList(data.opaqueList);
                    }
                    else if (op is DrawTransparentOperation && data.hasTransparentList)
                    {
                        ctx.cmd.DrawRendererList(data.transparentList);
                    }
                }
            });
        }

        /// <summary>
        /// 为 BlitOperation 录制双 Pass（与 Tutorial 10 RecordBlitPass 一致）。
        ///
        /// 为何需要双 Pass（教学重点）：
        ///   同一 RasterPass 内不能同时 UseTexture（读）和 SetRenderAttachment（写）同一纹理。
        ///   解决方案：
        ///     Pass A：activeColor → tempTexture（material 后处理）
        ///     Pass B：tempTexture → activeColor（复制回颜色缓冲）
        /// </summary>
        private void RecordBlitPass(
            RenderGraph           rg,
            UniversalResourceData resourceData,
            BlitOperation         blitOp)
        {
            if (!resourceData.activeColorTexture.IsValid()) return;

            var tempDesc         = rg.GetTextureDesc(resourceData.activeColorTexture);
            tempDesc.name        = "NewRenderGraph_Blit_Temp";
            tempDesc.clearBuffer = false;
            var tempTexture      = rg.CreateTexture(tempDesc);

            // Pass A：activeColor → tempTexture（material 后处理）
            using (var builder = rg.AddRasterRenderPass<BlitPassData>("Graph: Blit", out var pd))
            {
                pd.sourceTexture = resourceData.activeColorTexture;
                pd.material      = blitOp.material;
                builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
                builder.SetRenderAttachment(tempTexture, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((BlitPassData data, RasterGraphContext ctx) =>
                    Blitter.BlitTexture(ctx.cmd, data.sourceTexture, new Vector4(1, 1, 0, 0), data.material, 0));
            }

            // Pass B：tempTexture → activeColor（复制回颜色缓冲）
            using (var builder = rg.AddRasterRenderPass<BlitPassData>("Graph: Blit Copy Back", out var pd))
            {
                pd.sourceTexture = tempTexture;
                pd.material      = null;
                builder.UseTexture(tempTexture, AccessFlags.Read);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((BlitPassData data, RasterGraphContext ctx) =>
                    Blitter.BlitTexture(ctx.cmd, data.sourceTexture, new Vector4(1, 1, 0, 0), 0, false));
            }
        }

        public void Dispose() { }
    }
}
