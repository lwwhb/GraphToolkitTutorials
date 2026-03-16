using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GraphToolkitTutorials.GraphDrivenURP.Runtime
{
    /// <summary>
    /// 完整的图形驱动URP渲染器特性
    /// 终极目标：通过图形编辑器完全控制URP渲染管线
    /// </summary>
    public class GraphDrivenURPFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private URPGraphRuntime m_URPGraph;

        [SerializeField]
        private bool m_EnableDebugLog = false;

        private GraphDrivenURPPass m_URPPass;

        public override void Create()
        {
            if (m_URPGraph != null)
            {
                m_URPPass = new GraphDrivenURPPass(m_URPGraph, m_EnableDebugLog);
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_URPPass != null && m_URPGraph != null)
            {
                renderer.EnqueuePass(m_URPPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            m_URPPass?.Dispose();
        }
    }

    /// <summary>
    /// 图形驱动的URP渲染Pass
    /// </summary>
    public class GraphDrivenURPPass : ScriptableRenderPass
    {
        private URPGraphRuntime m_URPGraph;
        private bool m_EnableDebugLog;
        private const string k_ProfilerTag = "Graph Driven URP";

        // 资源池
        private System.Collections.Generic.Dictionary<string, RenderTexture> m_RenderTexturePool;

        public GraphDrivenURPPass(URPGraphRuntime urpGraph, bool enableDebugLog)
        {
            m_URPGraph = urpGraph;
            m_EnableDebugLog = enableDebugLog;
            m_RenderTexturePool = new System.Collections.Generic.Dictionary<string, RenderTexture>();
            renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_URPGraph == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get(k_ProfilerTag);

            try
            {
                if (m_EnableDebugLog)
                {
                    Debug.Log("=== Graph Driven URP Execution Start ===");
                }

                // 执行URP图形
                ExecuteURPGraph(cmd, context, ref renderingData);

                context.ExecuteCommandBuffer(cmd);
            }
            finally
            {
                CommandBufferPool.Release(cmd);
            }
        }

        private void ExecuteURPGraph(CommandBuffer cmd, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var startNode = m_URPGraph.GetStartNode();
            if (startNode == null)
            {
                Debug.LogWarning("No start node found in URP graph");
                return;
            }

            int currentNodeIndex = startNode.nextNodeIndex;

            while (currentNodeIndex >= 0)
            {
                var node = m_URPGraph.GetNode(currentNodeIndex);
                if (node == null)
                    break;

                if (m_EnableDebugLog)
                {
                    Debug.Log($"Executing node: {node.nodeType}");
                }

                // 执行节点
                int nextIndex = -1;

                if (node is OpaquePassNode opaqueNode)
                {
                    ExecuteOpaquePass(cmd, opaqueNode, ref renderingData);
                    nextIndex = node.nextNodeIndex;
                }
                else if (node is TransparentPassNode transparentNode)
                {
                    ExecuteTransparentPass(cmd, transparentNode, ref renderingData);
                    nextIndex = node.nextNodeIndex;
                }
                else if (node is ShadowPassNode shadowNode)
                {
                    ExecuteShadowPass(cmd, shadowNode, ref renderingData);
                    nextIndex = node.nextNodeIndex;
                }
                else if (node is SkyboxPassNode skyboxNode)
                {
                    ExecuteSkyboxPass(cmd, skyboxNode, ref renderingData);
                    nextIndex = node.nextNodeIndex;
                }
                else if (node is PostProcessPassNode postProcessNode)
                {
                    ExecutePostProcessPass(cmd, postProcessNode, ref renderingData);
                    nextIndex = node.nextNodeIndex;
                }
                else if (node is CustomPassNode customNode)
                {
                    ExecuteCustomPass(cmd, customNode, ref renderingData);
                    nextIndex = node.nextNodeIndex;
                }
                else if (node is RenderTextureNode rtNode)
                {
                    CreateRenderTexture(rtNode);
                    nextIndex = node.nextNodeIndex;
                }
                else if (node is QualityBranchNode qualityNode)
                {
                    nextIndex = EvaluateQualityBranch(qualityNode);
                }
                else if (node is PlatformBranchNode platformNode)
                {
                    nextIndex = EvaluatePlatformBranch(platformNode);
                }
                else if (node is PipelineEndNode)
                {
                    break;
                }
                else
                {
                    nextIndex = node.nextNodeIndex;
                }

                currentNodeIndex = nextIndex;
            }

            if (m_EnableDebugLog)
            {
                Debug.Log("=== Graph Driven URP Execution End ===");
            }
        }

        private void ExecuteOpaquePass(CommandBuffer cmd, OpaquePassNode node, ref RenderingData renderingData)
        {
            cmd.BeginSample("Opaque Pass");
            // 简化实现：实际应用中需要完整的渲染逻辑
            // 这里可以添加绘制不透明物体的代码
            cmd.EndSample("Opaque Pass");
        }

        private void ExecuteTransparentPass(CommandBuffer cmd, TransparentPassNode node, ref RenderingData renderingData)
        {
            cmd.BeginSample("Transparent Pass");
            // 简化实现：绘制透明物体
            cmd.EndSample("Transparent Pass");
        }

        private void ExecuteShadowPass(CommandBuffer cmd, ShadowPassNode node, ref RenderingData renderingData)
        {
            cmd.BeginSample("Shadow Pass");
            // 简化实现：渲染阴影贴图
            cmd.EndSample("Shadow Pass");
        }

        private void ExecuteSkyboxPass(CommandBuffer cmd, SkyboxPassNode node, ref RenderingData renderingData)
        {
            cmd.BeginSample("Skybox Pass");
            // 简化实现：渲染天空盒
            cmd.EndSample("Skybox Pass");
        }

        private void ExecutePostProcessPass(CommandBuffer cmd, PostProcessPassNode node, ref RenderingData renderingData)
        {
            cmd.BeginSample("Post Process Pass");
            // 简化实现：应用后处理效果
            if (node.enableBloom)
            {
                cmd.BeginSample("Bloom");
                // Bloom效果
                cmd.EndSample("Bloom");
            }
            if (node.enableTonemapping)
            {
                cmd.BeginSample("Tonemapping");
                // Tonemapping
                cmd.EndSample("Tonemapping");
            }
            cmd.EndSample("Post Process Pass");
        }

        private void ExecuteCustomPass(CommandBuffer cmd, CustomPassNode node, ref RenderingData renderingData)
        {
            cmd.BeginSample(node.passName);
            // 简化实现：执行自定义Pass
            if (node.material != null)
            {
                // 使用自定义材质进行渲染
            }
            cmd.EndSample(node.passName);
        }

        private void CreateRenderTexture(RenderTextureNode node)
        {
            if (!m_RenderTexturePool.ContainsKey(node.textureName))
            {
                var rt = new RenderTexture(node.width, node.height, node.depthBits, node.format);
                rt.name = node.textureName;
                m_RenderTexturePool[node.textureName] = rt;

                if (m_EnableDebugLog)
                {
                    Debug.Log($"Created RenderTexture: {node.textureName} ({node.width}x{node.height})");
                }
            }
        }

        private int EvaluateQualityBranch(QualityBranchNode node)
        {
            int currentQuality = QualitySettings.GetQualityLevel();
            bool useHighQuality = currentQuality >= node.minimumQuality;

            if (m_EnableDebugLog)
            {
                Debug.Log($"Quality Branch: Current={currentQuality}, Minimum={node.minimumQuality}, UseHigh={useHighQuality}");
            }

            return useHighQuality ? node.highQualityIndex : node.lowQualityIndex;
        }

        private int EvaluatePlatformBranch(PlatformBranchNode node)
        {
            bool isMobile = Application.isMobilePlatform;

            if (m_EnableDebugLog)
            {
                Debug.Log($"Platform Branch: IsMobile={isMobile}");
            }

            return isMobile ? node.mobileIndex : node.pcIndex;
        }

        public void Dispose()
        {
            // 清理RenderTexture池
            foreach (var rt in m_RenderTexturePool.Values)
            {
                if (rt != null)
                {
                    rt.Release();
                }
            }
            m_RenderTexturePool.Clear();
        }
    }
}
以添加实际的不透明物体渲染
            cmd.EndSample("Opaque Pass");
        }

        private void ExecuteTransparentPass(CommandBuffer cmd, TransparentPassNode node, ref RenderingData renderingData)
        {
            cmd.BeginSample("Transparent Pass");
            // 透明物体渲染
            cmd.EndSample("Transparent Pass");
        }

        private void ExecuteShadowPass(CommandBuffer cmd, ShadowPassNode node, ref RenderingData renderingData)
        {
            cmd.BeginSample("Shadow Pass");
            // 阴影渲染
            cmd.EndSample("Shadow Pass");
        }

        private void ExecuteSkyboxPass(CommandBuffer cmd, SkyboxPassNode node, ref RenderingData renderingData)
        {
            cmd.BeginSample("Skybox Pass");
            // 天空盒渲染
            cmd.EndSample("Skybox Pass");
        }

        private void ExecutePostProcessPass(CommandBuffer cmd, PostProcessPassNode node, ref RenderingData renderingData)
        {
            cmd.BeginSample("Post Process");
            // 后处理效果
            if (node.enableBloom)
            {
                cmd.BeginSample("Bloom");
                // Bloom实现
                cmd.EndSample("Bloom");
            }
            if (node.enableTonemapping)
            {
                cmd.BeginSample("Tonemapping");
                // Tonemapping实现
                cmd.EndSample("Tonemapping");
            }
            cmd.EndSample("Post Process");
        }

        private void ExecuteCustomPass(CommandBuffer cmd, CustomPassNode node, ref RenderingData renderingData)
        {
            cmd.BeginSample(node.passName);
            // 自定义Pass实现
            if (node.material != null)
            {
                // 使用自定义材质进行渲染
            }
            cmd.EndSample(node.passName);
        }

        private void CreateRenderTexture(RenderTextureNode node)
        {
            if (!m_RenderTexturePool.ContainsKey(node.textureName))
            {
                var rt = new RenderTexture(node.width, node.height, node.depthBits, node.format);
                rt.name = node.textureName;
                m_RenderTexturePool[node.textureName] = rt;

                if (m_EnableDebugLog)
                {
                    Debug.Log($"Created RenderTexture: {node.textureName} ({node.width}x{node.height})");
                }
            }
        }

        private int EvaluateQualityBranch(QualityBranchNode node)
        {
            // 根据当前质量设置选择分支
            int currentQuality = QualitySettings.GetQualityLevel();

            if (currentQuality >= node.minimumQuality)
            {
                return node.highQualityIndex;
            }
            else
            {
                return node.lowQualityIndex;
            }
        }

        private int EvaluatePlatformBranch(PlatformBranchNode node)
        {
            // 根据平台选择分支
#if UNITY_STANDALONE || UNITY_EDITOR
            return node.pcIndex;
#elif UNITY_ANDROID || UNITY_IOS
            return node.mobileIndex;
#else
            return node.pcIndex;
#endif
        }

        public void Dispose()
        {
            // 清理RenderTexture池
            foreach (var rt in m_RenderTexturePool.Values)
            {
                if (rt != null)
                {
                    rt.Release();
                }
            }
            m_RenderTexturePool.Clear();
        }
    }
}
