using System;

namespace GraphToolkitTutorials.ExecutionFlow.Runtime
{
    /// <summary>
    /// 分支节点（运行时）
    /// </summary>
    [Serializable]
    public class BranchNode : TaskRuntimeNode
    {
        public bool condition = true;
        public string conditionVariableName = "";
        public int trueNodeIndex = -1;
        public int falseNodeIndex = -1;
    }
}
