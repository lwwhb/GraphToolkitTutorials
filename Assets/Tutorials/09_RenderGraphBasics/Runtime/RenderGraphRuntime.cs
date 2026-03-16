using System.Collections.Generic;
using UnityEngine;

namespace GraphToolkitTutorials.RenderGraphBasics.Runtime
{
    /// <summary>
    /// 运行时渲染图形
    /// 包含所有运行时节点的可序列化版本
    /// </summary>
    public class RenderGraphRuntime : ScriptableObject
    {
        /// <summary>
        /// 所有运行时节点
        /// </summary>
        [SerializeReference]
        public List<RenderRuntimeNode> nodes = new List<RenderRuntimeNode>();

        /// <summary>
        /// 起始节点索引
        /// </summary>
        public int startNodeIndex = -1;

        /// <summary>
        /// 获取指定索引的节点（非泛型版本）
        /// </summary>
        public RenderRuntimeNode GetNode(int index)
        {
            if (index >= 0 && index < nodes.Count)
                return nodes[index];
            return null;
        }

        /// <summary>
        /// 获取指定索引的节点（泛型版本）
        /// </summary>
        public T GetNode<T>(int index) where T : RenderRuntimeNode
        {
            return GetNode(index) as T;
        }
    }
}
