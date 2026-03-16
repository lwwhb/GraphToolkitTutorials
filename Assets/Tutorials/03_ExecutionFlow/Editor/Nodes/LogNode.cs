using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.ExecutionFlow
{
    /// <summary>
    /// 日志节点
    /// 输出日志信息到控制台
    /// </summary>
    [Node("Log", "Task")]
    internal class LogNode : TaskNode
    {
        [SerializeField]
        private string m_Message = "Hello from Task Graph!";

        [SerializeField]
        private LogType m_LogType = LogType.Log;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);
        }

        public override Runtime.TaskRuntimeNode CreateRuntimeNode(TaskGraph graph)
        {
            var runtimeNode = new Runtime.LogNode
            {
                message = m_Message,
                logType = m_LogType
            };

            // 获取下一个节点的索引
            var nextNode = GetNextNode(graph);
            runtimeNode.nextNodeIndex = nextNode != null ? graph.GetNodes().IndexOf(nextNode) : -1;

            return runtimeNode;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Message", () => m_Message, v => m_Message = v).Build();
            context.AddOption("Log Type", () => m_LogType, v => m_LogType = v).Build();
        }
    }
}
