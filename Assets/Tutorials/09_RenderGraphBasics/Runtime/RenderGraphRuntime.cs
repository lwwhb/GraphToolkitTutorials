using System.Collections.Generic;
using UnityEngine;

namespace GraphToolkitTutorials.RenderGraphBasics.Runtime
{
    /// <summary>
    /// 运行时渲染图形
    /// 包含所有运行时节点的可序列化列表，由 RenderGraphImporter 填充。
    /// </summary>
    public class RenderGraphRuntime : ScriptableObject
    {
        /// <summary>所有运行时节点（[SerializeReference] 支持多态序列化）</summary>
        [SerializeReference]
        public List<RenderRuntimeNode> nodes = new List<RenderRuntimeNode>();

        /// <summary>相机节点在 nodes 列表中的索引</summary>
        public int startNodeIndex = -1;

        /// <summary>获取指定索引的节点</summary>
        public RenderRuntimeNode GetNode(int index)
        {
            if (index >= 0 && index < nodes.Count)
                return nodes[index];
            return null;
        }

        /// <summary>获取指定索引的节点（泛型版本）</summary>
        public T GetNode<T>(int index) where T : RenderRuntimeNode
            => GetNode(index) as T;

        /// <summary>获取相机节点（执行起点）</summary>
        public CameraNode GetCameraNode()
        {
            if (startNodeIndex >= 0 && startNodeIndex < nodes.Count)
                return nodes[startNodeIndex] as CameraNode;
            return null;
        }
    }
}
