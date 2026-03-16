using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GraphToolkitTutorials.RenderGraphBasics.Runtime
{
    /// <summary>
    /// 运行时渲染节点基类
    /// </summary>
    [Serializable]
    public abstract class RenderRuntimeNode
    {
        public string nodeType;
        public int nextNodeIndex = -1;

        protected RenderRuntimeNode()
        {
            nodeType = GetType().Name;
        }
    }

    /// <summary>
    /// 相机节点（运行时）
    /// </summary>
    [Serializable]
    public class CameraNode : RenderRuntimeNode
    {
        public string cameraTag = "MainCamera";
    }

    /// <summary>
    /// 渲染Pass节点（运行时）
    /// </summary>
    [Serializable]
    public class RenderPassNode : RenderRuntimeNode
    {
        public string passName;
        public RenderPassEvent passEvent;
        public LayerMask layerMask;
    }

    /// <summary>
    /// Blit节点（运行时）
    /// </summary>
    [Serializable]
    public class BlitNode : RenderRuntimeNode
    {
        public Material material;
    }

    /// <summary>
    /// 输出节点（运行时）
    /// </summary>
    [Serializable]
    public class OutputNode : RenderRuntimeNode
    {
    }
}
