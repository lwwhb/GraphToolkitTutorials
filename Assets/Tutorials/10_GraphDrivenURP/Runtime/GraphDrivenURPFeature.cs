using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RendererUtils;

namespace GraphToolkitTutorials.GraphDrivenURP.Runtime
{
    /// <summary>
    /// 图形驱动的 URP 渲染器特性。
    ///
    /// 教学要点：
    ///   • 使用 RecordRenderGraph（Unity 6 URP 唯一入口）替代旧版 Execute
    ///   • 每个图节点独立调用 AddRasterRenderPass → Frame Debugger 每节点一个条目
    ///   • 分支节点（Quality / Platform）在 RecordRenderGraph 阶段决策，非 GPU 阶段
    ///   • Blit 使用双 Pass 模式，避免同一 RasterPass 内对同一纹理同时读写
    ///
    /// 配置步骤：
    ///   1. URP Renderer Inspector → Add Renderer Feature → Graph Driven URP Feature
    ///   2. 将 .urpgraph 资产（生成的 URPGraphRuntime）拖入 URP Graph 字段
    /// </summary>
    public class GraphDrivenURPFeature : ScriptableRendererFeature
    {
        [SerializeField] private URPGraphRuntime m_URPGraph;

        private GraphDrivenURPPass m_Pass;

        public override void Create()
        {
            if (m_URPGraph != null)
                m_Pass = new GraphDrivenURPPass(m_URPGraph);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_URPGraph == null) return;

            // 延迟初始化：Create() 可能在 m_URPGraph 尚未反序列化时被调用
            if (m_Pass == null)
                m_Pass = new GraphDrivenURPPass(m_URPGraph);

            renderer.EnqueuePass(m_Pass);
        }

        protected override void Dispose(bool disposing)
        {
            m_Pass?.Dispose();
        }
    }

    /// <summary>
    /// 图形驱动的 URP RenderGraph Pass。
    ///
    /// 每节点一个 Pass 架构：
    ///   遍历运行时图，每个有效节点调用 AddRasterRenderPass，
    ///   使 Frame Debugger 中每个节点显示为独立条目。
    ///
    /// 支持的节点类型：
    ///   OpaquePassNode       → DrawRendererList（不透明队列，前到后排序）
    ///   TransparentPassNode  → DrawRendererList（透明队列，后到前排序）
    ///   CustomPassNode       → Blitter.BlitTexture 双 Pass（material 后处理）
    ///   QualityBranchNode    → QualitySettings.GetQualityLevel() 条件跳转
    ///   PlatformBranchNode   → Application.isMobilePlatform 条件跳转
    ///   ShadowPassNode       → 跳过（URP 内置处理，标记节点）
    ///   SkyboxPassNode       → 跳过（URP 内置处理，标记节点）
    ///   PostProcessPassNode  → 跳过（URP Volume 系统处理，标记节点）
    ///   PipelineEndNode      → 终止遍历
    /// </summary>
    internal class GraphDrivenURPPass : ScriptableRenderPass
    {
        private readonly URPGraphRuntime m_URPGraph;

        // DrawRendererList 所需的 ShaderTag，匹配 URP 标准着色器 LightMode
        private static readonly ShaderTagId[] k_ShaderTagIds =
        {
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly"),
        };

        // ─── Pass Data ───────────────────────────────────────────────────────
        // PassData 通过 RenderGraph Pass 生命周期传递，避免直接闭包捕获外部变量

        private class DrawPassData
        {
            public RendererListHandle rendererListHandle;
        }

        private class BlitPassData
        {
            public TextureHandle sourceTexture;
            public Material      material;
        }

        public GraphDrivenURPPass(URPGraphRuntime graph)
        {
            m_URPGraph      = graph;
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }

        /// <summary>
        /// RecordRenderGraph：遍历运行时图，每节点独立添加一个 RenderGraph Pass。
        ///
        /// 与旧版 Execute 的区别：
        ///   旧：Execute(ScriptableRenderContext, ref RenderingData) + CommandBufferPool
        ///   新：RecordRenderGraph 声明资源依赖，SetRenderFunc 提交 GPU 命令
        ///
        /// ContextContainer 三个数据对象：
        ///   UniversalResourceData  → 当前帧颜色/深度 TextureHandle
        ///   UniversalCameraData    → Camera 对象（DrawRendererList 排序需要）
        ///   UniversalRenderingData → CullingResults（DrawRendererList 裁剪需要）
        /// </summary>
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_URPGraph == null) return;

            var startNode = m_URPGraph.GetStartNode();
            if (startNode == null)
            {
                Debug.LogWarning("[GraphDrivenURP] No PipelineStartNode in runtime graph. " +
                                 "Re-import the .urpgraph asset.");
                return;
            }

            var resourceData  = frameData.Get<UniversalResourceData>();
            var cameraData    = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();

            // 诊断日志：首帧输出节点数与起始索引，帮助确认图正确导入
            if (Time.frameCount <= 2)
                Debug.Log($"[GraphDrivenURP] Recording: {m_URPGraph.nodes.Count} nodes, " +
                          $"startIndex={m_URPGraph.startNodeIndex}, " +
                          $"activeColorValid={resourceData.activeColorTexture.IsValid()}");

            int current = startNode.nextNodeIndex;
            while (current >= 0)
            {
                var node = m_URPGraph.GetNode(current);
                if (node == null) break;

                if (node is OpaquePassNode opaque)
                {
                    // 教学重点：DrawRendererList 不透明物体 Pass
                    RecordDrawPass(renderGraph, resourceData, cameraData, renderingData,
                        "Graph: Opaque Pass",
                        RenderQueueRange.opaque,
                        SortingCriteria.CommonOpaque,
                        opaque.layerMask);
                    current = opaque.nextNodeIndex;
                }
                else if (node is TransparentPassNode transparent)
                {
                    // 教学重点：DrawRendererList 透明物体 Pass（后到前排序）
                    RecordDrawPass(renderGraph, resourceData, cameraData, renderingData,
                        "Graph: Transparent Pass",
                        RenderQueueRange.transparent,
                        SortingCriteria.CommonTransparent,
                        transparent.layerMask);
                    current = transparent.nextNodeIndex;
                }
                else if (node is CustomPassNode custom)
                {
                    // 教学重点：Blitter.BlitTexture 双 Pass 模式后处理
                    if (custom.material != null)
                        RecordBlitPass(renderGraph, resourceData, custom);
                    current = custom.nextNodeIndex;
                }
                else if (node is QualityBranchNode quality)
                {
                    // 教学重点：分支节点在 RecordRenderGraph 阶段（CPU）决策，不是 GPU 命令
                    current = QualitySettings.GetQualityLevel() >= quality.minimumQualityForHigh
                        ? quality.highQualityIndex
                        : quality.lowQualityIndex;
                }
                else if (node is PlatformBranchNode platform)
                {
                    // 教学重点：平台分支在 RecordRenderGraph 阶段决策
                    current = Application.isMobilePlatform ? platform.mobileIndex : platform.pcIndex;
                }
                else if (node is PipelineEndNode)
                {
                    break;
                }
                else
                {
                    // ShadowPassNode / SkyboxPassNode / PostProcessPassNode 为标记节点，
                    // 对应功能由 URP 内置 Pass 处理，此处直接跳过
                    current = node.nextNodeIndex;
                }
            }
        }

        /// <summary>
        /// 为一个绘制节点记录 DrawRendererList Pass（教学参考：T09 RecordRenderPassNode）。
        ///
        /// Frame Debugger 可见性原理：
        ///   AddRasterRenderPass + SetRenderAttachment 触发 RenderGraph 自动插入 SetRenderTarget，
        ///   该 SetRenderTarget 命令使此 Pass 在 Frame Debugger 中出现独立条目。
        ///   单靠 BeginSample/EndSample 是 CPU Profiler 标记，Frame Debugger 不可见。
        /// </summary>
        private void RecordDrawPass(
            RenderGraph            rg,
            UniversalResourceData  resourceData,
            UniversalCameraData    cameraData,
            UniversalRenderingData renderingData,
            string                 passName,
            RenderQueueRange       renderQueueRange,
            SortingCriteria        sortingCriteria,
            LayerMask              layerMask)
        {
            if (!resourceData.activeColorTexture.IsValid())
            {
                Debug.LogWarning($"[GraphDrivenURP] '{passName}': activeColorTexture is invalid. " +
                                 "Ensure a URP Renderer with this feature is active for the camera.");
                return;
            }

            using var builder = rg.AddRasterRenderPass<DrawPassData>(passName, out var passData);

            // RendererListDesc：描述要绘制哪些对象（队列范围、排序、层遮罩）
            var desc = new RendererListDesc(k_ShaderTagIds, renderingData.cullResults, cameraData.camera)
            {
                sortingCriteria  = sortingCriteria,
                renderQueueRange = renderQueueRange,
                layerMask        = layerMask,
            };
            passData.rendererListHandle = rg.CreateRendererList(desc);

            builder.UseRendererList(passData.rendererListHandle);  // 防止 Pass 被 RenderGraph 裁剪
            builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
            if (resourceData.activeDepthTexture.IsValid())
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc((DrawPassData data, RasterGraphContext ctx) =>
            {
                ctx.cmd.DrawRendererList(data.rendererListHandle);
            });
        }

        /// <summary>
        /// 为 CustomPassNode 记录 Blit Pass（双 Pass 模式）。
        ///
        /// 为何需要双 Pass：
        ///   同一 RasterPass 内不能同时 UseTexture（读）和 SetRenderAttachment（写）同一纹理。
        ///   解决方案：
        ///     Pass A：activeColor --read--> material 处理 --write--> tempTexture
        ///     Pass B：tempTexture --read--> 原样复制     --write--> activeColor
        ///
        /// 对比旧版 CommandBuffer.Blit：
        ///   旧：cmd.Blit(source, destination, material)
        ///   新：Blitter.BlitTexture(cmd, source, scaleBias, material, passIndex)
        ///       渲染目标由 SetRenderAttachment 声明，不作为 Blitter 参数
        /// </summary>
        private void RecordBlitPass(
            RenderGraph           rg,
            UniversalResourceData resourceData,
            CustomPassNode        node)
        {
            if (!resourceData.activeColorTexture.IsValid())
            {
                Debug.LogWarning($"[GraphDrivenURP] Blit pass '{node.passName}': activeColorTexture is invalid. " +
                                 "Ensure a URP Renderer with this feature is active for the camera.");
                return;
            }

            var tempDesc         = rg.GetTextureDesc(resourceData.activeColorTexture);
            tempDesc.name        = "URPGraph_Blit_Temp";
            tempDesc.clearBuffer = false;
            var tempTexture      = rg.CreateTexture(tempDesc);

            // Pass A：activeColor → tempTexture（material 后处理）
            using (var builder = rg.AddRasterRenderPass<BlitPassData>("Graph: " + node.passName, out var pd))
            {
                pd.sourceTexture = resourceData.activeColorTexture;
                pd.material      = node.material;

                builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
                builder.SetRenderAttachment(tempTexture, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((BlitPassData data, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(ctx.cmd, data.sourceTexture, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            // Pass B：tempTexture → activeColor（复制回颜色缓冲）
            using (var builder = rg.AddRasterRenderPass<BlitPassData>("Graph: " + node.passName + " Copy Back", out var pd))
            {
                pd.sourceTexture = tempTexture;
                pd.material      = null;

                builder.UseTexture(tempTexture, AccessFlags.Read);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((BlitPassData data, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(ctx.cmd, data.sourceTexture, new Vector4(1, 1, 0, 0), 0, false);
                });
            }
        }

        public void Dispose() { }
    }
}
