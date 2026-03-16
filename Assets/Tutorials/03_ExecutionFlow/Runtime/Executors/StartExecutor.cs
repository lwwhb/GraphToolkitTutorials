using System.Collections;
using UnityEngine;

namespace GraphToolkitTutorials.ExecutionFlow.Runtime
{
    /// <summary>
    /// 起始节点执行器
    /// </summary>
    public class StartExecutor : ITaskExecutor
    {
        public IEnumerator Execute(TaskRuntimeGraph graph, int nodeIndex)
        {
            var node = graph.GetNode<StartNode>(nodeIndex);
            if (node == null)
            {
                Debug.LogError($"StartExecutor: Invalid node at index {nodeIndex}");
                yield break;
            }

            Debug.Log("Task graph started");

            // 立即执行下一个节点
            if (node.nextNodeIndex >= 0)
            {
                yield return node.nextNodeIndex;
            }
        }
    }
}
