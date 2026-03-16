using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GraphToolkitTutorials.RenderGraphBasics.Runtime
{
    /// <summary>
    /// 图形驱动的渲染器特性
    /// 将渲染图形集成到URP渲染管线
    /// </summary>
    public class GraphDrivenRendererFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private RenderGraphRuntime m_RenderGraph;

        private GraphDrivenRenderPass m_RenderPass;

        public override void Create()
        {
            if (m_RenderGraph != null)
            {
                m_RenderPass = new GraphDrivenRenderPass(m_RenderGraph);
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_RenderPass != null && m_RenderGraph != null)
            {
                renderer.EnqueuePass(m_RenderPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            m_RenderPass?.Dispose();
        }
    }

    /// <summary>
    /// 图形驱动的渲染Pass
    /// </summary>
    public class GraphDrivenRenderPass : ScriptableRenderPass
    {
        private RenderGraphRuntime m_RenderGraph;
        private const string k_ProfilerTag = "Graph Driven Render";

        public GraphDrivenRenderPass(RenderGraphRuntime renderGraph)
        {
            m_RenderGraph = renderGraph;
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_RenderGraph == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get(k_ProfilerTag);

            try
            {
                // 执行渲染图形
                ExecuteRenderGraph(cmd, ref renderingData);

                context.ExecuteCommandBuffer(cmd);
            }
            finally
            {
                CommandBufferPool.Release(cmd);
            }
        }

        private void ExecuteRenderGraph(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var cameraNode = m_RenderGraph.GetCameraNode();
            if (cameraNode == null)
            {
                Debug.LogWarning("No camera node found in render graph");
                return;
            }

            int currentNodeIndex = cameraNode.nextNodeIndex;

            while (currentNodeIndex >= 0)
            {
                var node = m_RenderGraph.GetNode(currentNodeIndex);
                if (node == null)
                    break;

                // 执行节点
                if (node is RenderPassNode renderPassNode)
                {
                    ExecuteRenderPassNode(cmd, renderPassNode, ref renderingData);
                }
                else if (node is BlitNode blitNode)
                {
                    ExecuteBlitNode(cmd, blitNode);
                }
                else if (node is OutputNode)
                {
                    // 输出节点，结束执行
                    break;
                }

                currentNodeIndex = node.nextNodeIndex;
            }
        }

        private void ExecuteRenderPassNode(CommandBuffer cmd, RenderPassNode node, ref RenderingData renderingData)
        {
            // 这是一个简化的实现
            // 实际应用中需要更复杂的Pass管理
            cmd.BeginSample(node.passName);

            // 这里可以添加实际的渲染逻辑
            // 例如：绘制特定层的对象、应用后处理等

            cmd.EndSample(node.passName);
        }

        private void ExecuteBlitNode(CommandBuffer cmd, BlitNode node)
        {
            // 简化的Blit实现
            if (node.material != null)
            {
                cmd.BeginSample("Blit");
                // 实际的Blit操作需要源和目标纹理
                // cmd.Blit(source, destination, node.material);
                cmd.EndSample("Blit");
            }
        }

        public void Dispose()
        {
            // 清理资源
        }
    }
}
