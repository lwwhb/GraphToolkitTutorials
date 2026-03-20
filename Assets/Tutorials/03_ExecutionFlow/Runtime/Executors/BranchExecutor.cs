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

            // 优先从运行时变量读取，否则使用导入时的默认值
            bool condition = string.IsNullOrEmpty(node.conditionVariableName)
                ? node.condition
                : graph.GetBool(node.conditionVariableName, node.condition);

            Debug.Log($"Branch condition '{node.conditionVariableName}': {condition}");

            int nextIndex = condition ? node.trueNodeIndex : node.falseNodeIndex;

            if (nextIndex >= 0)
            {
                yield return nextIndex;
            }
            else
            {
                Debug.LogWarning($"Branch has no valid path for condition: {condition}");
            }
        }
    }
}
