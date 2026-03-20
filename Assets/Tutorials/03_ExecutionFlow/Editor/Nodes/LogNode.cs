using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.ExecutionFlow
{
    /// <summary>
    /// 日志节点
    /// 输出日志信息到控制台
    /// </summary>
    [Node("Task", "")]
    [Serializable]
    internal class LogNode : TaskNode
    {
        private INodeOption m_Message;
        private INodeOption m_LogType;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);
        }

        public override Runtime.TaskRuntimeNode CreateRuntimeNode(TaskGraph graph)
        {
            var runtimeNode = new Runtime.LogNode();

            if (m_Message != null && m_Message.TryGetValue(out string message))
                runtimeNode.message = message;

            if (m_LogType != null && m_LogType.TryGetValue(out LogType logType))
                runtimeNode.logType = logType;

            var nextNode = GetNextNode(graph);
            runtimeNode.nextNodeIndex = nextNode != null ? graph.GetNodeIndex(nextNode) : -1;

            return runtimeNode;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_Message = context.AddOption<string>("Message").Build();
            m_LogType = context.AddOption<LogType>("Log Type").Build();
        }
    }
}
