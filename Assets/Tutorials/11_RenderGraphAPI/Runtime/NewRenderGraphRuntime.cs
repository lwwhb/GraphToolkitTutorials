using System.Collections.Generic;
using UnityEngine;

namespace GraphToolkitTutorials.RenderGraphAPI.Runtime
{
    /// <summary>
    /// 运行时渲染图 - 存储运行时节点数据
    /// </summary>
    public class NewRenderGraphRuntime : ScriptableObject
    {
        public int StartNodeIndex = -1;

        [SerializeReference]
        private List<RenderGraphRuntimeNode> m_Nodes = new List<RenderGraphRuntimeNode>();

        public void AddNode(RenderGraphRuntimeNode node)
        {
            m_Nodes.Add(node);
        }

        public RenderGraphRuntimeNode GetNode(int index)
        {
            if (index >= 0 && index < m_Nodes.Count)
                return m_Nodes[index];
            return null;
        }

        public int NodeCount => m_Nodes.Count;
    }

    /// <summary>
    /// 运行时节点基类
    /// </summary>
    [System.Serializable]
    public abstract class RenderGraphRuntimeNode
    {
        public int nextNodeIndex = -1;
    }

    [System.Serializable]
    public class PipelineStartRuntimeNode : RenderGraphRuntimeNode { }

    [System.Serializable]
    public class PipelineEndRuntimeNode : RenderGraphRuntimeNode { }

    [System.Serializable]
    public class OpaquePassRuntimeNode : RenderGraphRuntimeNode
    {
        public bool enableDynamicBatching = true;
        public bool enableInstancing = true;
    }

    [System.Serializable]
    public class TransparentPassRuntimeNode : RenderGraphRuntimeNode { }

    [System.Serializable]
    public class ClearPassRuntimeNode : RenderGraphRuntimeNode
    {
        public Color clearColor = Color.black;
        public bool clearDepth = true;
    }

    [System.Serializable]
    public class BlitPassRuntimeNode : RenderGraphRuntimeNode
    {
        public Material material;
    }
}
