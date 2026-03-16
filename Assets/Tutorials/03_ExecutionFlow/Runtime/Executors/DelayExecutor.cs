using System.Collections;
using UnityEngine;

namespace GraphToolkitTutorials.ExecutionFlow.Runtime
{
    /// <summary>
    /// 延迟节点执行器
    /// </summary>
    public class DelayExecutor : ITaskExecutor
    {
        public IEnumerator Execute(TaskRuntimeGraph graph, int nodeIndex)
        {
            var node = graph.GetNode<DelayNode>(nodeIndex);
            if (node == null)
            {
                Debug.LogError($"DelayExecutor: Invalid node at index {nodeIndex}");
                yield break;
            }

            Debug.Log($"Delaying for {node.duration} seconds...");

            // 等待指定时间
            yield return new WaitForSeconds(node.duration);

            Debug.Log("Delay completed");

            // 执行下一个节点
            if (node.nextNodeIndex >= 0)
            {
                yield return node.nextNodeIndex;
            }
        }
    }
}
