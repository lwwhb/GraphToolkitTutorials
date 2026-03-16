using System.Collections.Generic;
using UnityEngine;

namespace GraphToolkitTutorials.BehaviorTree.Runtime
{
    /// <summary>
    /// 运行时行为树
    /// 包含所有运行时节点的可序列化版本
    /// </summary>
    public class BehaviorTreeRuntime : ScriptableObject
    {
        /// <summary>
        /// 所有运行时节点
        /// </summary>
        public List<BTRuntimeNode> nodes = new List<BTRuntimeNode>();

        /// <summary>
        /// 根节点索引
        /// </summary>
        public int rootNodeIndex = -1;

        /// <summary>
        /// 从编辑器图形构建运行时行为树
        /// </summary>
        public void BuildFromEditorGraph(BehaviorTreeGraph editorGraph)
        {
            nodes.Clear();

            // 转换所有节点
            foreach (var editorNode in editorGraph.Nodes)
            {
                if (editorNode is BTNode btNode)
                {
                    var runtimeNode = btNode.CreateRuntimeNode(editorGraph);
                    nodes.Add(runtimeNode);

                    // 记录根节点索引
                    if (editorNode is RootNode)
                    {
                        rootNodeIndex = nodes.Count - 1;
                    }
                }
            }

            Debug.Log($"Built runtime behavior tree with {nodes.Count} nodes, root index: {rootNodeIndex}");
        }

        /// <summary>
        /// 获取指定索引的节点
        /// </summary>
        public T GetNode<T>(int index) where T : BTRuntimeNode
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
        public BTRuntimeNode GetNode(int index)
        {
            if (index >= 0 && index < nodes.Count)
            {
                return nodes[index];
            }
            return null;
        }

        /// <summary>
        /// 获取根节点
        /// </summary>
        public RootNode GetRootNode()
        {
            return GetNode<RootNode>(rootNodeIndex);
        }
    }
}
