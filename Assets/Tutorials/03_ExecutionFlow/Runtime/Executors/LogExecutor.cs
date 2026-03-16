using System.Collections;
using UnityEngine;

namespace GraphToolkitTutorials.ExecutionFlow.Runtime
{
    /// <summary>
    /// 日志节点执行器
    /// </summary>
    public class LogExecutor : ITaskExecutor
    {
        public IEnumerator Execute(TaskRuntimeGraph graph, int nodeIndex)
        {
            var node = graph.GetNode<LogNode>(nodeIndex);
            if (node == null)
            {
                Debug.LogError($"LogExecutor: Invalid node at index {nodeIndex}");
                yield break;
            }

            // 输出日志
            switch (node.logType)
            {
                case LogType.Log:
                    Debug.Log(node.message);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(node.message);
                    break;
                case LogType.Error:
                    Debug.LogError(node.message);
                    break;
            }

            // 执行下一个节点
            if (node.nextNodeIndex >= 0)
            {
                yield return node.nextNodeIndex;
            }
        }
    }
}
