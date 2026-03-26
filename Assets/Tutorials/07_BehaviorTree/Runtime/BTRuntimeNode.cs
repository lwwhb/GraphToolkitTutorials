using System;
using UnityEngine;

namespace GraphToolkitTutorials.BehaviorTree.Runtime
{
    /// <summary>
    /// 行为树节点状态
    /// </summary>
    public enum NodeStatus
    {
        Running,    // 正在运行
        Success,    // 成功
        Failure     // 失败
    }

    /// <summary>
    /// 运行时行为树节点基类
    /// </summary>
    [Serializable]
    public abstract class BTRuntimeNode
    {
        public string nodeType;

        protected BTRuntimeNode()
        {
            nodeType = GetType().Name;
        }
    }

    /// <summary>
    /// 根节点（运行时）
    /// </summary>
    [Serializable]
    public class RootNode : BTRuntimeNode
    {
        public int childIndex = -1;
    }

    /// <summary>
    /// 复合节点基类（运行时）
    /// </summary>
    [Serializable]
    public abstract class CompositeRuntimeNode : BTRuntimeNode
    {
        public int[] childIndices = new int[0];
    }

    /// <summary>
    /// 装饰节点基类（运行时）
    /// </summary>
    [Serializable]
    public abstract class DecoratorRuntimeNode : BTRuntimeNode
    {
        public int childIndex = -1;
    }

    /// <summary>
    /// 叶子节点基类（运行时）
    /// </summary>
    [Serializable]
    public abstract class ActionRuntimeNode : BTRuntimeNode
    {
    }
}
