using System.Collections.Generic;
using UnityEngine;

namespace GraphToolkitTutorials.GraphDrivenURP.Runtime
{
    /// <summary>
    /// 运行时URP图形
    /// 包含所有运行时节点的可序列化版本
    /// </summary>
    public class URPGraphRuntime : ScriptableObject
    {
        /// <summary>
        /// 所有运行时节点
        /// </summary>
        public List<URPRuntimeNode> nodes = new List<URPRuntimeNode>();

        /// <summary>
        /// 起始节点索引
        /// </summary>
        public int startNodeIndex = -1;

        /// <summary>
        /// 从编辑器图形构建运行时URP图形
        /// </summary>
        public void BuildFromEditorGraph(URPGraph editorGraph)
        {
            nodes.Clear();

            // 转换所有节点
            foreach (var editorNode in editorGraph.Nodes)
            {
                if (editorNode is URPNode urpNode)
                {
                    var runtimeNode = urpNode.CreateRuntimeNode(editorGraph);
                    nodes.Add(runtimeNode);

                    // 记录起始节点索引
                    if (editorNode is PipelineStartNode)
                    {
                        startNodeIndex = nodes.Count - 1;
                    }
                }
            }

            Debug.Log($"Built runtime URP graph with {nodes.Count} nodes, start index: {startNodeIndex}");
        }

        /// <summary>
        /// 获取指定索引的节点
        /// </summary>
        public T GetNode<T>(int index) where T : URPRuntimeNode
        {
            if (index >= 0 && index < nodes.Count)
            {
                return nodes[index] as T;
            }
            return null;
        }

        /// <summary>
        /// 获取指定索引的节点（非泛型版本）
        /// </summary>
        public URPRuntimeNode GetNode(int index)
        {
            if (index >= 0 && index < nodes.Count)
            {
                return nodes[index];
            }
            return null;
        }

        /// <summary>
        /// 获取起始节点
        /// </summary>
        public PipelineStartNode GetStartNode()
        {
            return GetNode<PipelineStartNode>(startNodeIndex);
        }
    }
}
