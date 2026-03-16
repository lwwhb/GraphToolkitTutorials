using System;

namespace GraphToolkitTutorials.ExecutionFlow.Runtime
{
    /// <summary>
    /// 延迟节点（运行时）
    /// </summary>
    [Serializable]
    public class DelayNode : TaskRuntimeNode
    {
        public float duration = 1f;
    }
}
