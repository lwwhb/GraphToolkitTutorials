using System;
using UnityEngine;

namespace GraphToolkitTutorials.BehaviorTree.Runtime
{
    /// <summary>
    /// 序列节点（运行时）
    /// </summary>
    [Serializable]
    public class SequenceNode : CompositeRuntimeNode
    {
    }

    /// <summary>
    /// 选择节点（运行时）
    /// </summary>
    [Serializable]
    public class SelectorNode : CompositeRuntimeNode
    {
    }

    /// <summary>
    /// 并行节点（运行时）
    /// </summary>
    [Serializable]
    public class ParallelNode : CompositeRuntimeNode
    {
        public enum SuccessPolicy
        {
            RequireAll,
            RequireOne
        }

        public SuccessPolicy successPolicy;
    }

    /// <summary>
    /// 反转节点（运行时）
    /// </summary>
    [Serializable]
    public class InverterNode : DecoratorRuntimeNode
    {
    }

    /// <summary>
    /// 重复节点（运行时）
    /// </summary>
    [Serializable]
    public class RepeaterNode : DecoratorRuntimeNode
    {
        public int repeatCount = 3;
        public bool infiniteLoop = false;
    }

    /// <summary>
    /// 成功节点（运行时）
    /// </summary>
    [Serializable]
    public class SucceederNode : DecoratorRuntimeNode
    {
    }

    /// <summary>
    /// 条件装饰节点（运行时）
    /// </summary>
    [Serializable]
    public class ConditionalNode : DecoratorRuntimeNode
    {
        public string blackboardKey;
        public bool expectedValue;
    }

    /// <summary>
    /// 等待节点（运行时）
    /// </summary>
    [Serializable]
    public class WaitNode : LeafRuntimeNode
    {
        public float duration = 1f;
    }

    /// <summary>
    /// 日志节点（运行时）
    /// </summary>
    [Serializable]
    public class LogNode : LeafRuntimeNode
    {
        public string message;
        public LogType logType;
    }

    /// <summary>
    /// 设置黑板值节点（运行时）
    /// </summary>
    [Serializable]
    public class SetBlackboardValueNode : LeafRuntimeNode
    {
        public string key;
        public string value;
    }

    /// <summary>
    /// 检查黑板值节点（运行时）
    /// </summary>
    [Serializable]
    public class CheckBlackboardValueNode : LeafRuntimeNode
    {
        public string key;
        public string expectedValue;
    }

    /// <summary>
    /// 随机成功节点（运行时）
    /// </summary>
    [Serializable]
    public class RandomSuccessNode : LeafRuntimeNode
    {
        public float successProbability = 0.5f;
    }
}
