using System.Collections;
using UnityEngine;

namespace GraphToolkitTutorials.BehaviorTree.Runtime
{
    /// <summary>
    /// 反转节点执行器
    /// </summary>
    public class InverterExecutor : IBTExecutor
    {
        public IEnumerator Execute(BehaviorTreeRuntime tree, int nodeIndex, Blackboard blackboard)
        {
            var node = tree.GetNode<InverterNode>(nodeIndex);
            if (node == null || node.childIndex < 0)
            {
                yield return NodeStatus.Failure;
                yield break;
            }

            // 执行子节点
            var executor = BehaviorTreeRunner.GetExecutor(tree.GetNode(node.childIndex));
            var childCoroutine = executor.Execute(tree, node.childIndex, blackboard);

            NodeStatus childStatus = NodeStatus.Running;
            while (childCoroutine.MoveNext())
            {
                if (childCoroutine.Current is NodeStatus status)
                {
                    childStatus = status;
                }
                else
                {
                    yield return childCoroutine.Current;
                }
            }

            // 反转结果
            if (childStatus == NodeStatus.Success)
            {
                yield return NodeStatus.Failure;
            }
            else if (childStatus == NodeStatus.Failure)
            {
                yield return NodeStatus.Success;
            }
            else
            {
                yield return NodeStatus.Running;
            }
        }
    }

    /// <summary>
    /// 重复节点执行器
    /// </summary>
    public class RepeaterExecutor : IBTExecutor
    {
        public IEnumerator Execute(BehaviorTreeRuntime tree, int nodeIndex, Blackboard blackboard)
        {
            var node = tree.GetNode<RepeaterNode>(nodeIndex);
            if (node == null || node.childIndex < 0)
            {
                yield return NodeStatus.Failure;
                yield break;
            }

            int count = 0;
            while (node.infiniteLoop || count < node.repeatCount)
            {
                var executor = BehaviorTreeRunner.GetExecutor(tree.GetNode(node.childIndex));
                var childCoroutine = executor.Execute(tree, node.childIndex, blackboard);

                NodeStatus childStatus = NodeStatus.Running;
                while (childCoroutine.MoveNext())
                {
                    if (childCoroutine.Current is NodeStatus status)
                    {
                        childStatus = status;
                    }
                    else
                    {
                        yield return childCoroutine.Current;
                    }
                }

                // 如果子节点失败，重复器失败
                if (childStatus == NodeStatus.Failure)
                {
                    yield return NodeStatus.Failure;
                    yield break;
                }

                count++;
            }

            yield return NodeStatus.Success;
        }
    }

    /// <summary>
    /// 成功节点执行器
    /// </summary>
    public class SucceederExecutor : IBTExecutor
    {
        public IEnumerator Execute(BehaviorTreeRuntime tree, int nodeIndex, Blackboard blackboard)
        {
            var node = tree.GetNode<SucceederNode>(nodeIndex);
            if (node == null || node.childIndex < 0)
            {
                yield return NodeStatus.Success;
                yield break;
            }

            // 执行子节点
            var executor = BehaviorTreeRunner.GetExecutor(tree.GetNode(node.childIndex));
            var childCoroutine = executor.Execute(tree, node.childIndex, blackboard);

            while (childCoroutine.MoveNext())
            {
                if (!(childCoroutine.Current is NodeStatus))
                {
                    yield return childCoroutine.Current;
                }
            }

            // 无论子节点结果如何，总是返回成功
            yield return NodeStatus.Success;
        }
    }

    /// <summary>
    /// 条件装饰节点执行器
    /// </summary>
    public class ConditionalExecutor : IBTExecutor
    {
        public IEnumerator Execute(BehaviorTreeRuntime tree, int nodeIndex, Blackboard blackboard)
        {
            var node = tree.GetNode<ConditionalNode>(nodeIndex);
            if (node == null)
            {
                yield return NodeStatus.Failure;
                yield break;
            }

            // 检查条件
            bool condition = blackboard.GetValue(node.blackboardKey, false);
            if (condition != node.expectedValue)
            {
                yield return NodeStatus.Failure;
                yield break;
            }

            // 条件满足，执行子节点
            if (node.childIndex < 0)
            {
                yield return NodeStatus.Success;
                yield break;
            }

            var executor = BehaviorTreeRunner.GetExecutor(tree.GetNode(node.childIndex));
            var childCoroutine = executor.Execute(tree, node.childIndex, blackboard);

            NodeStatus childStatus = NodeStatus.Running;
            while (childCoroutine.MoveNext())
            {
                if (childCoroutine.Current is NodeStatus status)
                {
                    childStatus = status;
                }
                else
                {
                    yield return childCoroutine.Current;
                }
            }

            yield return childStatus;
        }
    }
}
