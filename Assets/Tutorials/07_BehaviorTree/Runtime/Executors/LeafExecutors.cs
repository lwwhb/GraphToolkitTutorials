using System.Collections;
using UnityEngine;

namespace GraphToolkitTutorials.BehaviorTree.Runtime
{
    /// <summary>
    /// 等待节点执行器
    /// </summary>
    public class WaitExecutor : IBTExecutor
    {
        public IEnumerator Execute(BehaviorTreeRuntime tree, int nodeIndex, Blackboard blackboard)
        {
            var node = tree.GetNode<WaitNode>(nodeIndex);
            if (node == null)
            {
                yield return NodeStatus.Failure;
                yield break;
            }

            yield return new WaitForSeconds(node.duration);
            yield return NodeStatus.Success;
        }
    }

    /// <summary>
    /// 日志节点执行器
    /// </summary>
    public class LogExecutor : IBTExecutor
    {
        public IEnumerator Execute(BehaviorTreeRuntime tree, int nodeIndex, Blackboard blackboard)
        {
            var node = tree.GetNode<LogNode>(nodeIndex);
            if (node == null)
            {
                yield return NodeStatus.Failure;
                yield break;
            }

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

            yield return NodeStatus.Success;
        }
    }

    /// <summary>
    /// 设置黑板值节点执行器
    /// </summary>
    public class SetBlackboardValueExecutor : IBTExecutor
    {
        public IEnumerator Execute(BehaviorTreeRuntime tree, int nodeIndex, Blackboard blackboard)
        {
            var node = tree.GetNode<SetBlackboardValueNode>(nodeIndex);
            if (node == null)
            {
                yield return NodeStatus.Failure;
                yield break;
            }

            blackboard.SetValue(node.key, node.value);
            yield return NodeStatus.Success;
        }
    }

    /// <summary>
    /// 检查黑板值节点执行器
    /// </summary>
    public class CheckBlackboardValueExecutor : IBTExecutor
    {
        public IEnumerator Execute(BehaviorTreeRuntime tree, int nodeIndex, Blackboard blackboard)
        {
            var node = tree.GetNode<CheckBlackboardValueNode>(nodeIndex);
            if (node == null)
            {
                yield return NodeStatus.Failure;
                yield break;
            }

            string value = blackboard.GetValue(node.key, string.Empty);
            yield return value == node.expectedValue ? NodeStatus.Success : NodeStatus.Failure;
        }
    }

    /// <summary>
    /// 随机成功节点执行器
    /// </summary>
    public class RandomSuccessExecutor : IBTExecutor
    {
        public IEnumerator Execute(BehaviorTreeRuntime tree, int nodeIndex, Blackboard blackboard)
        {
            var node = tree.GetNode<RandomSuccessNode>(nodeIndex);
            if (node == null)
            {
                yield return NodeStatus.Failure;
                yield break;
            }

            float random = Random.value;
            yield return random <= node.successProbability ? NodeStatus.Success : NodeStatus.Failure;
        }
    }
}
