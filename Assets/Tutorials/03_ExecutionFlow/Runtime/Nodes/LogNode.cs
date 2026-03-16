using System;
using UnityEngine;

namespace GraphToolkitTutorials.ExecutionFlow.Runtime
{
    /// <summary>
    /// 日志节点（运行时）
    /// </summary>
    [Serializable]
    public class LogNode : TaskRuntimeNode
    {
        public string message = "Hello from Task Graph!";
        public LogType logType = LogType.Log;
    }
}
