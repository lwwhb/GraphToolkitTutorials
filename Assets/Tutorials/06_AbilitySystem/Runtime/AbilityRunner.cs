using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GraphToolkitTutorials.AbilitySystem.Runtime
{
    /// <summary>
    /// 技能运行器 — 挂载到游戏对象上，持有一个 AbilityRuntimeGraph 资产。
    /// 调用 FireEvent(eventName) 触发对应的事件链。
    /// </summary>
    public class AbilityRunner : MonoBehaviour
    {
        [Tooltip("由 AbilityImporter 生成的运行时图资产（.ability 文件）")]
        public AbilityRuntimeGraph graph;

        /// <summary>
        /// 触发指定名称的事件，若图中存在对应的 OnEvent 节点则开始执行。
        /// </summary>
        public void FireEvent(string eventName)
        {
            if (graph == null)
            {
                Debug.LogWarning("[AbilityRunner] graph 未赋值。");
                return;
            }

            int startIndex = graph.FindTrigger(eventName);
            if (startIndex < 0)
            {
                Debug.LogWarning($"[AbilityRunner] 未找到事件 '{eventName}'。");
                return;
            }

            // OnEventRuntimeNode 本身不执行任何逻辑，直接跳到 next
            var triggerNode = graph.nodes[startIndex] as OnEventRuntimeNode;
            if (triggerNode == null) return;

            StartCoroutine(ExecuteFrom(triggerNode.next));
        }

        // ──────────────────────────────────────────────────────────────────
        // 内部执行协程
        // ──────────────────────────────────────────────────────────────────

        /// <summary>从指定索引开始顺序执行节点链，直到 -1（链结束）。</summary>
        private IEnumerator ExecuteFrom(int nodeIndex)
        {
            while (nodeIndex >= 0 && nodeIndex < graph.nodes.Count)
            {
                var node = graph.nodes[nodeIndex];

                switch (node)
                {
                    case WaitRuntimeNode waitNode:
                        yield return new WaitForSeconds(waitNode.duration);
                        nodeIndex = waitNode.next;
                        break;

                    case LogActionRuntimeNode logNode:
                        Debug.Log($"[Ability] {logNode.message}");
                        nodeIndex = logNode.next;
                        break;

                    case ParallelRuntimeNode parallelNode:
                        yield return StartCoroutine(ExecuteParallel(parallelNode));
                        nodeIndex = parallelNode.done;
                        break;

                    case OnEventRuntimeNode eventNode:
                        // 事件节点作为内部跳转目标时直接跳过
                        nodeIndex = eventNode.next;
                        break;

                    default:
                        Debug.LogWarning($"[AbilityRunner] 未知节点类型: {node?.GetType().Name}，执行中止。");
                        yield break;
                }
            }
        }

        /// <summary>
        /// 并行执行两条分支，等待两条分支全部完成后返回。
        /// branchA / branchB 为 -1 时视为该分支立即完成。
        /// </summary>
        private IEnumerator ExecuteParallel(ParallelRuntimeNode parallelNode)
        {
            bool aDone = parallelNode.branchA < 0;
            bool bDone = parallelNode.branchB < 0;

            if (!aDone)
                StartCoroutine(ExecuteAndSignal(parallelNode.branchA, () => aDone = true));
            if (!bDone)
                StartCoroutine(ExecuteAndSignal(parallelNode.branchB, () => bDone = true));

            yield return new WaitUntil(() => aDone && bDone);
        }

        /// <summary>执行一条节点链，完成后调用回调通知并行节点。</summary>
        private IEnumerator ExecuteAndSignal(int startIndex, System.Action onDone)
        {
            yield return StartCoroutine(ExecuteFrom(startIndex));
            onDone?.Invoke();
        }
    }
}
