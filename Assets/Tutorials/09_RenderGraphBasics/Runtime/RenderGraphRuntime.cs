using System.Collections.Generic;
using UnityEngine;
using GraphToolkitTutorials.RenderGraphBasics;

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
        public List<RenderRuntimeNode> nodes = new List<RenderRuntimeNode>();

        /// <summary>
        /// 相机节点索引
        /// </summary>
        public int cameraNodeIndex = -1;

        /// <summary>
        /// 从编辑器图形构建运行时渲染图形
        /// </summary>
        public void BuildFromEditorGraph(RenderGraph editorGraph)
        {
            nodes.Clear();

            // 转换所有节点
            foreach (var editorNode in editorGraph.Nodes)
            {
                if (editorNode is RenderNode renderNode)
                {
                    var runtimeNode = renderNode.CreateRuntimeNode(editorGraph);
                    nodes.Add(runtimeNode);

                    // 记录相机节点索引
                    if (editorNode is CameraNode)
                    {
                        cameraNodeIndex = nodes.Count - 1;
                    }
                }
            }

            Debug.Log($"Built runtime render graph with {nodes.Count} nodes, camera index: {cameraNodeIndex}");
        }

        /// <summary>
        /// 获取指定索引的节点
        /// </summary>
        public T GetNode<T>(int index) where T : RenderRuntimeNode
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
        public RenderRuntimeNode GetNode(int index)
        {
            if (index >= 0 && index < nodes.Count)
            {
                return nodes[index];
            }
            return null;
        }

        /// <summary>
        /// 获取相机节点
        /// </summary>
        public CameraNode GetCameraNode()
        {
            return GetNode<CameraNode>(cameraNodeIndex);
        }
    }
}
