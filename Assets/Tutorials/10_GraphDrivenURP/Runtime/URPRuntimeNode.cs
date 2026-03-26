using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GraphToolkitTutorials.GraphDrivenURP.Runtime
{
    /// <summary>运行时 URP 节点基类。所有子类均使用 [SerializeReference] 多态序列化。</summary>
    [Serializable]
    public abstract class URPRuntimeNode
    {
        public int nextNodeIndex = -1;
    }

    // ── Pipeline Nodes ──────────────────────────────────────────────────────

    [Serializable] public class PipelineStartNode : URPRuntimeNode { }
    [Serializable] public class PipelineEndNode   : URPRuntimeNode { }

    // ── Pass Nodes ──────────────────────────────────────────────────────────

    [Serializable]
    public class OpaquePassNode : URPRuntimeNode
    {
        public LayerMask       layerMask;
        public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    [Serializable]
    public class TransparentPassNode : URPRuntimeNode
    {
        public LayerMask       layerMask;
        public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    /// <summary>标记节点：URP 内部已处理阴影，此节点不发额外 GPU 命令。</summary>
    [Serializable] public class ShadowPassNode  : URPRuntimeNode { }

    /// <summary>标记节点：URP 内部已处理天空盒，此节点不发额外 GPU 命令。</summary>
    [Serializable] public class SkyboxPassNode  : URPRuntimeNode { }

    /// <summary>标记节点：URP Volume 管理后处理，此节点仅记录配置供参考。</summary>
    [Serializable]
    public class PostProcessPassNode : URPRuntimeNode
    {
        public bool enableBloom;
        public bool enableTonemapping;
        public bool enableVignette;
        public bool enableColorGrading;
    }

    /// <summary>自定义 Pass 节点：运行时通过 Blitter.BlitTexture 双 Pass 模式执行。</summary>
    [Serializable]
    public class CustomPassNode : URPRuntimeNode
    {
        public string         passName;
        public Material       material;
        public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    // ── Resource Nodes ──────────────────────────────────────────────────────

    [Serializable]
    public class RenderTextureNode : URPRuntimeNode
    {
        public string              textureName;
        public int                 width;
        public int                 height;
        public int                 depthBits;
        public RenderTextureFormat format;
    }

    // ── Control Nodes ───────────────────────────────────────────────────────

    [Serializable]
    public class QualityBranchNode : URPRuntimeNode
    {
        public int minimumQualityForHigh;  // 达到此等级则走 High 路径
        public int highQualityIndex = -1;
        public int lowQualityIndex  = -1;
    }

    [Serializable]
    public class PlatformBranchNode : URPRuntimeNode
    {
        public int pcIndex     = -1;
        public int mobileIndex = -1;
    }
}
