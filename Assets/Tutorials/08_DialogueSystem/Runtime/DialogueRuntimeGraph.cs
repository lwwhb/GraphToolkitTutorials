using System;
using System.Collections.Generic;
using UnityEngine;

namespace GraphToolkitTutorials.DialogueSystem.Runtime
{
    /// <summary>
    /// 运行时对话图形 — 由 DialogueImporter 生成的 ScriptableObject 主资产。
    /// 包含所有运行时节点的可序列化列表。
    /// </summary>
    public class DialogueRuntimeGraph : ScriptableObject
    {
        /// <summary>
        /// 所有运行时节点（多态序列化需要 [SerializeReference]）
        /// </summary>
        [SerializeReference]
        public List<DialogueRuntimeNode> nodes = new List<DialogueRuntimeNode>();

        /// <summary>
        /// 起始节点在 nodes 列表中的索引
        /// </summary>
        public int startNodeIndex = -1;

        /// <summary>
        /// 获取指定索引的节点（泛型版本）
        /// </summary>
        public T GetNode<T>(int index) where T : DialogueRuntimeNode
        {
            if (index >= 0 && index < nodes.Count)
                return nodes[index] as T;
            return null;
        }

        /// <summary>
        /// 获取指定索引的节点
        /// </summary>
        public DialogueRuntimeNode GetNode(int index)
        {
            if (index >= 0 && index < nodes.Count)
                return nodes[index];
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
