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
        // 新增变量存储
        [Serializable]
        public class RuntimeVariable
        {
            public string name;
            public bool boolValue;
        }

        public List<RuntimeVariable> variables = new List<RuntimeVariable>();

        public bool GetBool(string name, bool defaultValue = false)
        {
            var v = variables.Find(x => x.name == name);
            return v != null ? v.boolValue : defaultValue;
        }

        public void SetBool(string name, bool value)
        {
            var v = variables.Find(x => x.name == name);
            if (v != null) v.boolValue = value;
        }
        /// <summary>
        /// 所有运行时节点
        /// </summary>
        [SerializeReference]
        public List<TaskRuntimeNode> nodes = new List<TaskRuntimeNode>();

        /// <summary>
        /// 起始节点索引
        /// </summary>
        public int startNodeIndex = -1;

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
