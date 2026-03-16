using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace GraphToolkitTutorials.RenderGraphAPI.Runtime
{
    /// <summary>
    /// 新RenderGraph API驱动的渲染特性
    /// 使用Unity 6的新RenderGraph API替代旧的CommandBuffer方式
    /// </summary>
    public class NewRenderGraphFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private NewRenderGraphRuntime m_GraphAsset;

        [SerializeField]
        private bool m_EnableDebugLog = false;

        private NewRenderGraphPass m_Pass;

        public override void Create()
        {
            if (m_GraphAsset != null)
            {
                m_Pass = new NewRenderGraphPass(m_GraphAsset, m_EnableDebugLog);
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_Pass != null && m_GraphAsset != null)
            {
                renderer.EnqueuePass(m_Pass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            m_Pass?.Dispose();
        }
    }

    /// <summary>
    /// 新RenderGraph API渲染Pass
    /// 关键区别：使用RecordRenderGraph而不是Execute
    /// </summary>
    public class NewRenderGraphPass : ScriptableRenderPass
    {
        private NewRenderGraphRuntime m_GraphAsset;
        private bool m_EnableDebugLog;
        private const string k_PassName = "New RenderGraph Pass";

        // PassData用于在RenderGraph中传递数据
        private class PassData
        {
            public RenderGraphRuntimeNode node;
            public TextureHandle cameraColor;
            public TextureHandle cameraDepth;
        }

        public NewRenderGraphPass(NewRenderGraphRuntime graphAsset, bool enableDebugLog)
        {
            m_GraphAsset = graphAsset;
            m_EnableDebugLog = enableDebugLog;
            renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        }

        /// <summary>
        /// 新API：使用RecordRenderGraph替代Execute
        /// 这是Unity 6 RenderGraph API的核心方法
        /// </summary>
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_GraphAsset == null || m_GraphAsset.StartNodeIndex < 0)
                return;

            // 从ContextContainer获取相机数据（替代旧的RenderingData）
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            if (m_EnableDebugLog)
            {
                Debug.Log($"=== New RenderGraph API Execution Start ===");
                Debug.Log($"Camera: {cameraData.camera.name}");
            }

            // 遍历图形节点
            int currentIndex = m_GraphAsset.StartNodeIndex;
            while (currentIndex >= 0)
            {
                var node = m_GraphAsset.GetNode(currentIndex);
                if (node == null) break;

                // 根据节点类型添加RenderGraph Pass
                switch (node)
                {
                    case ClearPassRuntimeNode clearNode:
                        AddClearPass(renderGraph, resourceData, clearNode);
                        currentIndex = clearNode.nextNodeIndex;
                        break;

                    case OpaquePassRuntimeNode opaqueNode:
                        AddOpaquePass(renderGraph, resourceData, opaqueNode);
                        currentIndex = opaqueNode.nextNodeIndex;
                        break;

                    case TransparentPassRuntimeNode transparentNode:
                        AddTransparentPass(renderGraph, resourceData, transparentNode);
                        currentIndex = transparentNode.nextNodeIndex;
                        break;

                    case BlitPassRuntimeNode blitNode:
                        AddBlitPass(renderGraph, resourceData, blitNode);
                        currentIndex = blitNode.nextNodeIndex;
                        break;

                    case PipelineEndRuntimeNode:
                        currentIndex = -1; // 结束
                        break;

                    default:
                        currentIndex = node.nextNodeIndex;
                        break;
                }
            }

            if (m_EnableDebugLog)
            {
                Debug.Log($"=== New RenderGraph API Execution End ===");
            }
        }

        /// <summary>
        /// 添加清屏Pass（新API方式）
        /// </summary>
        private void AddClearPass(RenderGraph renderGraph, UniversalResourceData resourceData, ClearPassRuntimeNode node)
        {
            // 使用RenderGraph.AddRasterRenderPass添加Pass
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Clear Pass", out var passData))
            {
                passData.node = node;
                passData.cameraColor = resourceData.activeColorTexture;

                // 设置渲染目标
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                if (node.clearDepth)
                {
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);
                }

                // 设置渲染函数（使用RasterCommandBuffer替代CommandBuffer）
                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    var clearNode = data.node as ClearPassRuntimeNode;
                    // 新API：使用RasterCommandBuffer
                    context.cmd.ClearRenderTarget(clearNode.clearDepth, true, clearNode.clearColor);
                });
            }
        }

        /// <summary>
        /// 添加不透明物体Pass（新API方式）
        /// </summary>
        private void AddOpaquePass(RenderGraph renderGraph, UniversalResourceData resourceData, OpaquePassRuntimeNode node)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Opaque Pass", out var passData))
            {
                passData.node = node;
                passData.cameraColor = resourceData.activeColorTexture;
                passData.cameraDepth = resourceData.activeDepthTexture;

                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    // 这里应该调用DrawRenderers，但为了简化示例，只做标记
                    context.cmd.BeginSample("Opaque Objects");
                    // 实际项目中：context.cmd.DrawRenderers(...)
                    context.cmd.EndSample("Opaque Objects");
                });
            }
        }

        /// <summary>
        /// 添加透明物体Pass（新API方式）
        /// </summary>
        private void AddTransparentPass(RenderGraph renderGraph, UniversalResourceData resourceData, TransparentPassRuntimeNode node)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Transparent Pass", out var passData))
            {
                passData.node = node;
                passData.cameraColor = resourceData.activeColorTexture;
                passData.cameraDepth = resourceData.activeDepthTexture;

                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    context.cmd.BeginSample("Transparent Objects");
                    // 实际项目中：context.cmd.DrawRenderers(...)
                    context.cmd.EndSample("Transparent Objects");
                });
            }
        }

        /// <summary>
        /// 添加Blit Pass（新API方式）
        /// </summary>
        private void AddBlitPass(RenderGraph renderGraph, UniversalResourceData resourceData, BlitPassRuntimeNode node)
        {
            if (node.material == null) return;

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Blit Pass", out var passData))
            {
                passData.node = node;
                passData.cameraColor = resourceData.activeColorTexture;

                // 读取源纹理
                builder.UseTexture(resourceData.activeColorTexture);
                // 写入目标纹理
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    var blitNode = data.node as BlitPassRuntimeNode;
                    // 新API：使用Blitter类进行Blit操作
                    Blitter.BlitCameraTexture(context.cmd, data.cameraColor, data.cameraColor, blitNode.material, 0);
                });
            }
        }

        public void Dispose()
        {
            // 新API中资源由RenderGraph自动管理，无需手动释放
        }
    }
}
