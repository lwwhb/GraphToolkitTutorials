using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.ExecutionFlow
{
    /// <summary>
    /// 延迟节点
    /// 等待指定的时间后继续执行
    /// </summary>
    [Node("Delay", "Task")]
    internal class DelayNode : TaskNode
    {
        [SerializeField]
        private float m_Duration = 1f;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);
            context.AddInputPort<float>("Duration").Build();
        }

        public override Runtime.TaskRuntimeNode CreateRuntimeNode(TaskGraph graph)
        {
            var runtimeNode = new Runtime.DelayNode
            {
                duration = m_Duration
            };

            // 获取下一个节点的索引
            var nextNode = GetNextNode(graph);
            runtimeNode.nextNodeIndex = nextNode != null ? graph.GetNodes().IndexOf(nextNode) : -1;

            return runtimeNode;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Duration", () => m_Duration, v => m_Duration = Mathf.Max(0f, v)).Build();
        }
    }
}
