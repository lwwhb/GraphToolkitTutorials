using System.Collections;
using UnityEngine;

namespace GraphToolkitTutorials.BehaviorTree.Runtime
{
    /// <summary>
    /// 序列节点执行器
    /// </summary>
    public class SequenceExecutor : IBTExecutor
    {
        public IEnumerator Execute(BehaviorTreeRuntime tree, int nodeIndex, Blackboard blackboard)
        {
            var node = tree.GetNode<SequenceNode>(nodeIndex);
            if (node == null)
            {
                yield return NodeStatus.Failure;
                yield break;
            }

            // 按顺序执行所有子节点
            foreach (var childIndex in node.childIndices)
            {
                var executor = BehaviorTreeRunner.GetExecutor(tree.GetNode(childIndex));
                var childCoroutine = executor.Execute(tree, childIndex, blackboard);

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

                // 如果任何子节点失败，序列失败
                if (childStatus == NodeStatus.Failure)
                {
                    yield return NodeStatus.Failure;
                    yield break;
                }
            }

            // 所有子节点成功，序列成功
            yield return NodeStatus.Success;
        }
    }

    /// <summary>
    /// 选择节点执行器
    /// </summary>
    public class SelectorExecutor : IBTExecutor
    {
        public IEnumerator Execute(BehaviorTreeRuntime tree, int nodeIndex, Blackboard blackboard)
        {
            var node = tree.GetNode<SelectorNode>(nodeIndex);
            if (node == null)
            {
                yield return NodeStatus.Failure;
                yield break;
            }

            // 按顺序执行子节点，直到一个成功
            foreach (var childIndex in node.childIndices)
            {
                var executor = BehaviorTreeRunner.GetExecutor(tree.GetNode(childIndex));
                var childCoroutine = executor.Execute(tree, childIndex, blackboard);

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

                // 如果任何子节点成功，选择成功
                if (childStatus == NodeStatus.Success)
                {
                    yield return NodeStatus.Success;
                    yield break;
                }
            }

            // 所有子节点失败，选择失败
            yield return NodeStatus.Failure;
        }
    }

    /// <summary>
    /// 并行节点执行器
    /// </summary>
    public class ParallelExecutor : IBTExecutor
    {
        public IEnumerator Execute(BehaviorTreeRuntime tree, int nodeIndex, Blackboard blackboard)
        {
            var node = tree.GetNode<ParallelNode>(nodeIndex);
            if (node == null)
            {
                yield return NodeStatus.Failure;
                yield break;
            }

            // 启动所有子节点
            var childCoroutines = new System.Collections.Generic.List<IEnumerator>();
            var childStatuses = new System.Collections.Generic.List<NodeStatus>();

            foreach (var childIndex in node.childIndices)
            {
                var executor = BehaviorTreeRunner.GetExecutor(tree.GetNode(childIndex));
                childCoroutines.Add(executor.Execute(tree, childIndex, blackboard));
                childStatuses.Add(NodeStatus.Running);
            }

            // 执行所有子节点直到完成
            bool allCompleted = false;
            while (!allCompleted)
            {
                allCompleted = true;

                for (int i = 0; i < childCoroutines.Count; i++)
                {
                    if (childStatuses[i] == NodeStatus.Running)
                    {
                        if (childCoroutines[i].MoveNext())
                        {
                            if (childCoroutines[i].Current is NodeStatus status)
                            {
                                childStatuses[i] = status;
                            }
                            else
                            {
                                allCompleted = false;
                            }
                        }
                    }
                }

                if (!allCompleted)
                {
                    yield return null;
                }
            }

            // 根据成功策略判断结果
            int successCount = 0;
            foreach (var status in childStatuses)
            {
                if (status == NodeStatus.Success)
                {
                    successCount++;
                }
            }

            if (node.successPolicy == ParallelNode.SuccessPolicy.RequireAll)
            {
                yield return successCount == childStatuses.Count ? NodeStatus.Success : NodeStatus.Failure;
            }
            else // RequireOne
            {
                yield return successCount > 0 ? NodeStatus.Success : NodeStatus.Failure;
            }
        }
    }
}
