using System.Collections.Generic;
using UnityEngine;

namespace GraphToolkitTutorials.DialogueSystem.Runtime
{
    /// <summary>
    /// 运行时对话图形
    /// 包含所有运行时节点的可序列化版本
    /// </summary>
    public class DialogueRuntimeGraph : ScriptableObject
    {
        /// <summary>
        /// 所有运行时节点
        /// </summary>
        public List<DialogueRuntimeNode> nodes = new List<DialogueRuntimeNode>();

        /// <summary>
        /// 起始节点索引
        /// </summary>
        public int startNodeIndex = -1;

        /// <summary>
        /// 从编辑器图形构建运行时对话图形
        /// </summary>
        public void BuildFromEditorGraph(DialogueGraph editorGraph)
        {
            nodes.Clear();

            // 转换所有节点
            foreach (var editorNode in editorGraph.Nodes)
            {
                if (editorNode is DialogueNode dialogueNode)
                {
                    var runtimeNode = dialogueNode.CreateRuntimeNode(editorGraph);
                    nodes.Add(runtimeNode);

                    // 记录起始节点索引
                    if (editorNode is StartDialogueNode)
                    {
                        startNodeIndex = nodes.Count - 1;
                    }
                }
            }

            Debug.Log($"Built runtime dialogue graph with {nodes.Count} nodes, start index: {startNodeIndex}");
        }

        /// <summary>
        /// 获取指定索引的节点
        /// </summary>
        public T GetNode<T>(int index) where T : DialogueRuntimeNode
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
        public DialogueRuntimeNode GetNode(int index)
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
        public StartNode GetStartNode()
        {
            return GetNode<StartNode>(startNodeIndex);
        }
    }
}
