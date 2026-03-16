using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GraphToolkitTutorials.GraphDrivenURP.Runtime
{
    /// <summary>
    /// 运行时URP节点基类
    /// </summary>
    [Serializable]
    public abstract class URPRuntimeNode
    {
        public string nodeType;
        public int nextNodeIndex = -1;

        protected URPRuntimeNode()
        {
            nodeType = GetType().Name;
        }
    }

    // Pipeline Nodes
    [Serializable]
    public class PipelineStartNode : URPRuntimeNode { }

    [Serializable]
    public class PipelineEndNode : URPRuntimeNode { }

    // Pass Nodes
    [Serializable]
    public class OpaquePassNode : URPRuntimeNode
    {
        public LayerMask layerMask;
        public bool enableDynamicBatching;
    }

    [Serializable]
    public class TransparentPassNode : URPRuntimeNode
    {
        public LayerMask layerMask;
    }

    [Serializable]
    public class ShadowPassNode : URPRuntimeNode
    {
        public int shadowResolution;
        public int cascadeCount;
    }

    [Serializable]
    public class SkyboxPassNode : URPRuntimeNode { }

    [Serializable]
    public class PostProcessPassNode : URPRuntimeNode
    {
        public bool enableBloom;
        public bool enableTonemapping;
        public bool enableVignette;
        public bool enableColorGrading;
    }

    [Serializable]
    public class CustomPassNode : URPRuntimeNode
    {
        public string passName;
        public Material material;
        public RenderPassEvent passEvent;
    }

    // Resource Nodes
    [Serializable]
    public class RenderTextureNode : URPRuntimeNode
    {
        public string textureName;
        public int width;
        public int height;
        public int depthBits;
        public RenderTextureFormat format;
    }

    [Serializable]
    public class MaterialNode : URPRuntimeNode
    {
        public Material material;
    }

    // Control Nodes
    [Serializable]
    public class QualityBranchNode : URPRuntimeNode
    {
        public int minimumQuality;
        public int highQualityIndex = -1;
        public int lowQualityIndex = -1;
    }

    [Serializable]
    public class PlatformBranchNode : URPRuntimeNode
    {
        public int pcIndex = -1;
        public int mobileIndex = -1;
    }
}
