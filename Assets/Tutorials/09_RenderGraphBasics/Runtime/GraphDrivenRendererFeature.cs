using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RendererUtils;

namespace GraphToolkitTutorials.RenderGraphBasics.Runtime
{
    /// <summary>
    /// 图形驱动的渲染器特性 - 将 RenderGraphRuntime 集成到 URP 渲染管线。
    ///
    /// Unity 6 URP 要求：
    ///   • 通过 AddRenderPasses 将 Pass 入队
    ///   • Pass 实现 RecordRenderGraph（而非旧版 Execute）
    ///   • 使用 AddRasterRenderPass + SetRenderAttachment 声明渲染目标
    /// </summary>
    public class GraphDrivenRendererFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private RenderGraphRuntime m_RenderGraph;

        private GraphDrivenRenderPass m_RenderPass;

        public override void Create()
        {
            if (m_RenderGraph != null)
                m_RenderPass = new GraphDrivenRenderPass(m_RenderGraph);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_RenderGraph == null) return;

            // 延迟初始化：Create() 可能在 m_RenderGraph 尚未反序列化时被调用
            if (m_RenderPass == null)
                m_RenderPass = new GraphDrivenRenderPass(m_RenderGraph);

            renderer.EnqueuePass(m_RenderPass);
        }

        protected override void Dispose(bool disposing)
        {
            m_RenderPass?.Dispose();
        }
    }

    /// <summary>
    /// 图形驱动的渲染 Pass。
    ///
    /// 架构（每节点一个 Pass）：
    ///   每个运行时节点在 RecordRenderGraph 中独立调用 AddRasterRenderPass，
    ///   这样每个节点在 Frame Debugger 中都有独立的可见条目。
    ///
    /// Frame Debugger 可见性原理：
    ///   AddRasterRenderPass + SetRenderAttachment
    ///     → RenderGraph 执行时自动插入 SetRenderTarget GPU 命令
    ///     → 该命令即为 Frame Debugger 中 Pass 的可见入口
    ///   BeginSample/EndSample 仅在 CPU Profiler 可见，不是 GPU 命令
    ///   DrawRendererList 是真正的 DrawCall GPU 命令
    /// </summary>
    public class GraphDrivenRenderPass : ScriptableRenderPass
    {
        private readonly RenderGraphRuntime m_RenderGraph;

        // DrawRendererList 所需的 ShaderTag，匹配 URP 标准着色器 LightMode
        private static readonly ShaderTagId[] k_ShaderTagIds =
        {
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly"),
        };

        // ─── RenderPassNode 的 PassData ─────────────────────────────────────
        // PassData 通过 RenderGraph Pass 生命周期传递数据，避免闭包捕获。
        // RendererListHandle 是值类型，可以安全存储在 class PassData 中。
        private class RenderPassData
        {
            public RendererListHandle rendererListHandle;
        }

        // ─── BlitNode 的 PassData ────────────────────────────────────────────
        private class BlitPassData
        {
            public TextureHandle sourceTexture;
            public Material      material;
        }

        public GraphDrivenRenderPass(RenderGraphRuntime renderGraph)
        {
            m_RenderGraph   = renderGraph;
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }

        /// <summary>
        /// RecordRenderGraph：遍历运行时图节点，每个节点独立添加一个 RenderGraph Pass。
        ///
        /// 与旧版单一 Pass 的区别：
        ///   旧：一个大 AddRasterRenderPass，内部 while 循环 → Frame Debugger 只有一个条目
        ///   新：每个节点各自调用 AddRasterRenderPass → Frame Debugger 每节点一个条目
        ///
        /// 数据来源（替代旧版 RenderingData）：
        ///   UniversalResourceData  → 当前帧颜色/深度 TextureHandle
        ///   UniversalCameraData    → Camera 对象（DrawRendererList 需要）
        ///   UniversalRenderingData → CullingResults（DrawRendererList 需要）
        /// </summary>
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_RenderGraph == null) return;

            var cameraNode = m_RenderGraph.GetCameraNode();
            if (cameraNode == null)
            {
                Debug.LogWarning("[RenderGraphBasics] No CameraNode in runtime graph. " +
                                 "Check that the .rendergraph asset was imported correctly.");
                return;
            }

            var resourceData  = frameData.Get<UniversalResourceData>();
            var cameraData    = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();

            int current = cameraNode.nextNodeIndex;
            while (current >= 0)
            {
                var node = m_RenderGraph.GetNode(current);
                if (node == null) break;

                if (node is RenderPassNode rp)
                {
                    RecordRenderPassNode(renderGraph, resourceData, cameraData, renderingData, rp);
                    current = rp.nextNodeIndex;
                }
                else if (node is BlitNode blit)
                {
                    RecordBlitNode(renderGraph, resourceData, blit);
                    current = blit.nextNodeIndex;
                }
                else if (node is OutputNode)
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
        /// 为 RenderPassNode 记录一个 DrawRendererList Pass。
        ///
        /// DrawRendererList 模式（Unity 6 RenderGraph 推荐方式）：
        ///   1. RecordRenderGraph 阶段：CreateRendererList 注册（延迟实例化）
        ///   2. builder.UseRendererList：声明依赖，防止 Pass 被 RenderGraph 裁剪
        ///   3. SetRenderFunc 中 DrawRendererList：发出真正的 DrawCall GPU 命令
        ///
        /// 对比旧版 ScriptableRenderContext.DrawRenderers：
        ///   旧：Execute() → context.DrawRenderers(cullResults, ref drawSettings, ref filterSettings)
        ///   新：RecordRenderGraph() 创建 RendererListHandle + SetRenderFunc 中 DrawRendererList
        ///
        /// RendererListDesc 参数说明：
        ///   k_ShaderTagIds            → 接受哪些 LightMode（UniversalForward 等）
        ///   renderingData.cullResults → 相机的可见对象裁剪结果（URP Cull 阶段计算）
        ///   cameraData.camera         → 用于视角矩阵和排序
        ///   layerMask                 → 来自节点的 Layer Mask 配置（默认 -1 = 所有层）
        ///   sortingCriteria           → 排序规则：不透明 = 前到后
        ///   renderQueueRange          → all = 不透明队列 + 透明队列
        /// </summary>
        private void RecordRenderPassNode(
            RenderGraph            rg,
            UniversalResourceData  resourceData,
            UniversalCameraData    cameraData,
            UniversalRenderingData renderingData,
            RenderPassNode         rp)
        {
            if (!resourceData.activeColorTexture.IsValid()) return;

            string passName = string.IsNullOrEmpty(rp.passName) ? "Graph: Render Pass" : "Graph: " + rp.passName;

            using var builder = rg.AddRasterRenderPass<RenderPassData>(passName, out var passData);

            // ── RendererListHandle ─────────────────────────────────────────────
            var desc = new RendererListDesc(k_ShaderTagIds, renderingData.cullResults, cameraData.camera)
            {
                sortingCriteria  = SortingCriteria.CommonOpaque,
                renderQueueRange = RenderQueueRange.all,
                layerMask        = rp.layerMask,
            };
            passData.rendererListHandle = rg.CreateRendererList(desc);

            // ── 资源声明 ───────────────────────────────────────────────────────
            // UseRendererList：告知 RenderGraph 此 Pass 依赖 RendererList，防止 Pass 被裁剪
            builder.UseRendererList(passData.rendererListHandle);

            // SetRenderAttachment：颜色写入 + 触发 RenderGraph 自动插入 SetRenderTarget
            //   → 这是 Pass 在 Frame Debugger 中出现独立条目的关键
            builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);

            // 深度在 Deferred 模式下可能无效，先检查再声明
            if (resourceData.activeDepthTexture.IsValid())
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite);

            // AllowPassCulling(false)：即使输出未被后续 Pass 读取也不裁剪
            builder.AllowPassCulling(false);

            // ── GPU 命令（Execute 阶段）────────────────────────────────────────
            // 渲染目标已由 SetRenderAttachment 自动绑定，无需手动 SetRenderTarget
            builder.SetRenderFunc((RenderPassData data, RasterGraphContext ctx) =>
            {
                // DrawRendererList：真正的 DrawCall GPU 命令，Frame Debugger 可见
                ctx.cmd.DrawRendererList(data.rendererListHandle);
            });
        }

        /// <summary>
        /// 为 BlitNode 记录后处理 Blit Pass（双 Pass 模式）。
        ///
        /// 为何需要双 Pass：
        ///   同一 RasterPass 内不能同时对同一纹理 UseTexture（读）和 SetRenderAttachment（写）。
        ///   解决方案：
        ///     Pass A：activeColor → tempTexture（material 处理）
        ///     Pass B：tempTexture → activeColor（复制回颜色缓冲）
        ///
        /// Blitter.BlitTexture 参数：
        ///   cmd       → RasterCommandBuffer（Unity 6 新增，封装底层 CommandBuffer）
        ///   source    → RTHandle 源纹理（TextureHandle 隐式转换为 RTHandle）
        ///   scaleBias → Vector4(scaleX, scaleY, offsetX, offsetY)，(1,1,0,0) = 全屏无偏移
        ///   material  → 后处理 Material（shader 通过 _BlitTexture uniform 采样源纹理）
        ///   pass      → Material 的 shader pass 索引（0 = 第一个 pass）
        ///
        /// 对比旧版 CommandBuffer.Blit：
        ///   旧：cmd.Blit(source, destination, material)
        ///   新：Blitter.BlitTexture(cmd, source, scaleBias, material, pass)
        ///      （渲染目标已由 SetRenderAttachment 声明，不作为 Blitter 参数）
        /// </summary>
        private void RecordBlitNode(
            RenderGraph           rg,
            UniversalResourceData resourceData,
            BlitNode              blit)
        {
            if (blit.material == null) return;
            if (!resourceData.activeColorTexture.IsValid()) return;

            // GetTextureDesc：从现有 TextureHandle 获取描述符，创建同规格临时纹理
            var tempDesc         = rg.GetTextureDesc(resourceData.activeColorTexture);
            tempDesc.name        = "Blit_Temp";
            tempDesc.clearBuffer = false;
            var tempTexture      = rg.CreateTexture(tempDesc);

            // ── Pass A：activeColor → tempTexture（material 处理）──────────────
            using (var builder = rg.AddRasterRenderPass<BlitPassData>("Graph: Blit " + blit.material.name, out var passData))
            {
                passData.sourceTexture = resourceData.activeColorTexture;
                passData.material      = blit.material;

                builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
                builder.SetRenderAttachment(tempTexture, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((BlitPassData data, RasterGraphContext ctx) =>
                {
                    // 绑定 source 到 _BlitTexture，用 material 绘制全屏三角形到 tempTexture
                    Blitter.BlitTexture(ctx.cmd, data.sourceTexture, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            // ── Pass B：tempTexture → activeColor（复制回）──────────────────────
            using (var builder = rg.AddRasterRenderPass<BlitPassData>("Graph: Blit Copy Back", out var passData))
            {
                passData.sourceTexture = tempTexture;
                passData.material      = null;

                builder.UseTexture(tempTexture, AccessFlags.Read);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((BlitPassData data, RasterGraphContext ctx) =>
                {
                    // 内置 Blit shader，像素精确复制（mipLevel=0, bilinear=false）
                    Blitter.BlitTexture(ctx.cmd, data.sourceTexture, new Vector4(1, 1, 0, 0), 0, false);
                });
            }
        }

        public void Dispose() { }
    }
}
