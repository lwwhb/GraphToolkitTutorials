using System;
using UnityEngine;

namespace GraphToolkitTutorials.ExecutionFlow.Runtime
{
    /// <summary>
    /// 运行时任务节点基类
    /// 所有运行时节点都继承自此类
    /// 这些节点是可序列化的，可以在运行时使用
    /// </summary>
    [Serializable]
    public abstract class TaskRuntimeNode
    {
        /// <summary>
        /// 下一个要执行的节点索引
        /// -1 表示没有下一个节点
        /// </summary>
        public int nextNodeIndex = -1;

        /// <summary>
        /// 节点类型名称（用于调试）
        /// </summary>
        public string nodeType;

        protected TaskRuntimeNode()
        {
            nodeType = GetType().Name;
        }
    }
}
