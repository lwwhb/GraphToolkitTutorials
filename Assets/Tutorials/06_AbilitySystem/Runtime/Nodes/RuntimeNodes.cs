using System;

namespace GraphToolkitTutorials.AbilitySystem.Runtime
{
    /// <summary>事件触发节点 — 图的入口，匹配 FireEvent(eventName)。</summary>
    [Serializable]
    public class OnEventRuntimeNode : AbilityRuntimeNode
    {
        public string eventName;
        public int    next;      // -1 = 结束
    }

    /// <summary>
    /// 并行节点 — 同时激活 branchA 和 branchB，
    /// 两条分支全部完成后触发 done。
    /// </summary>
    [Serializable]
    public class ParallelRuntimeNode : AbilityRuntimeNode
    {
        public int branchA; // -1 = 未连接（视为立即完成）
        public int branchB;
        public int done;    // 两条分支完成后继续的节点
    }

    /// <summary>等待节点 — 暂停 duration 秒。</summary>
    [Serializable]
    public class WaitRuntimeNode : AbilityRuntimeNode
    {
        public float duration;
        public int   next;
    }

    /// <summary>动作节点 — 打印消息（模拟技能效果）。</summary>
    [Serializable]
    public class LogActionRuntimeNode : AbilityRuntimeNode
    {
        public string message;
        public int    next;
    }
}
