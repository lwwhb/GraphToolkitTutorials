using System;
using System.Collections.Generic;
using UnityEngine;

namespace GraphToolkitTutorials.ExecutionFlow.Runtime
{
    /// <summary>
    /// 运行时任务图形
    /// 包含所有运行时节点的可序列化版本
    /// </summary>
    public class TaskRuntimeGraph : ScriptableObject
    {
        /// <summary>
        /// 所有运行时节点
        /// </summary>
        public List<TaskRuntimeNode> nodes = new List<TaskRuntimeNode>();

        /// <summary>
        /// 起始节点索引
        /// </summary>
        public int startNodeIndex = -1;

        /// <summary>
        /// 从编辑器图形构建运行时图形
        /// </summary>
        public void BuildFromEditorGraph(TaskGraph editorGraph)
        {
            nodes.Clear();

            // 转换所有节点
            foreach (var editorNode in editorGraph.Nodes)
            {
                if (editorNode is TaskNode taskNode)
                {
                    var runtimeNode = taskNode.CreateRuntimeNode(editorGraph);
                    nodes.Add(runtimeNode);

                    // 记录起始节点索引
                    if (editorNode is StartNode)
                    {
                        startNodeIndex = nodes.Count - 1;
                    }
                }
            }

            Debug.Log($"Built runtime graph with {nodes.Count} nodes, start index: {startNodeIndex}");
        }

        /// <summary>
        /// 获取指定索引的节点
        /// </summary>
        public T GetNode<T>(int index) where T : TaskRuntimeNode
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
        public TaskRuntimeNode GetNode(int index)
        {
            if (index >= 0 && index < nodes.Count)
            {
                return nodes[index];
            }
            return null;
        }
    }
}
