using System.Collections;
using UnityEngine;

namespace GraphToolkitTutorials.ExecutionFlow.Runtime
{
    /// <summary>
    /// 分支节点执行器
    /// </summary>
    public class BranchExecutor : ITaskExecutor
    {
        public IEnumerator Execute(TaskRuntimeGraph graph, int nodeIndex)
        {
            var node = graph.GetNode<BranchNode>(nodeIndex);
            if (node == null)
            {
                Debug.LogError($"BranchExecutor: Invalid node at index {nodeIndex}");
                yield break;
            }

            Debug.Log($"Branch condition: {node.condition}");

            // 根据条件选择分支
            int nextIndex = node.condition ? node.trueNodeIndex : node.falseNodeIndex;

            if (nextIndex >= 0)
            {
                yield return nextIndex;
            }
            else
            {
                Debug.LogWarning($"Branch has no valid path for condition: {node.condition}");
            }
        }
    }
}
