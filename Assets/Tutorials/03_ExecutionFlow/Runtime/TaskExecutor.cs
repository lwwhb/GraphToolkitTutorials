using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GraphToolkitTutorials.ExecutionFlow.Runtime
{
    /// <summary>
    /// 任务执行器
    /// 负责执行运行时任务图形
    /// </summary>
    public class TaskExecutor : MonoBehaviour
    {
        [Header("Graph")]
        [SerializeField]
        private TaskRuntimeGraph m_Graph;

        [Header("Settings")]
        [SerializeField]
        private bool m_AutoStart = true;

        [SerializeField]
        private bool m_Loop = false;

        private Dictionary<System.Type, ITaskExecutor> m_Executors;
        private bool m_IsRunning = false;

        private void Awake()
        {
            InitializeExecutors();
        }

        private void Start()
        {
            if (m_AutoStart && m_Graph != null)
            {
                StartExecution();
            }
        }

        /// <summary>
        /// 初始化执行器映射
        /// </summary>
        private void InitializeExecutors()
        {
            m_Executors = new Dictionary<System.Type, ITaskExecutor>
            {
                { typeof(StartNode), new StartExecutor() },
                { typeof(DelayNode), new DelayExecutor() },
                { typeof(LogNode), new LogExecutor() },
                { typeof(BranchNode), new BranchExecutor() }
            };
        }

        /// <summary>
        /// 开始执行图形
        /// </summary>
        public void StartExecution()
        {
            if (m_Graph == null)
            {
                Debug.LogError("TaskExecutor: No graph assigned!");
                return;
            }

            if (m_IsRunning)
            {
                Debug.LogWarning("TaskExecutor: Already running!");
                return;
            }

            StartCoroutine(ExecuteGraph());
        }

        /// <summary>
        /// 停止执行
        /// </summary>
        public void StopExecution()
        {
            StopAllCoroutines();
            m_IsRunning = false;
            Debug.Log("Task execution stopped");
        }

        /// <summary>
        /// 执行图形
        /// </summary>
        private IEnumerator ExecuteGraph()
        {
            m_IsRunning = true;

            do
            {
                if (m_Graph.startNodeIndex < 0)
                {
                    Debug.LogError("TaskExecutor: No start node found in graph!");
                    yield break;
                }

                int currentNodeIndex = m_Graph.startNodeIndex;

                while (currentNodeIndex >= 0)
                {
                    var node = m_Graph.GetNode(currentNodeIndex);
                    if (node == null)
                    {
                        Debug.LogError($"TaskExecutor: Invalid node at index {currentNodeIndex}");
                        break;
                    }

                    // 获取对应的执行器
                    var nodeType = node.GetType();
                    if (!m_Executors.TryGetValue(nodeType, out var executor))
                    {
                        Debug.LogError($"TaskExecutor: No executor found for node type {nodeType.Name}");
                        break;
                    }

                    // 执行节点
                    var executionCoroutine = executor.Execute(m_Graph, currentNodeIndex);
                    int nextNodeIndex = -1;

                    // 运行执行器协程
                    while (executionCoroutine.MoveNext())
                    {
                        var current = executionCoroutine.Current;

                        // 如果返回的是整数，表示下一个节点索引
                        if (current is int index)
                        {
                            nextNodeIndex = index;
                        }
                        // 否则是Unity的yield指令（如WaitForSeconds）
                        else
                        {
                            yield return current;
                        }
                    }

                    currentNodeIndex = nextNodeIndex;
                }

                Debug.Log("Task graph execution completed");

            } while (m_Loop);

            m_IsRunning = false;
        }

        /// <summary>
        /// 设置图形
        /// </summary>
        public void SetGraph(TaskRuntimeGraph graph)
        {
            if (m_IsRunning)
            {
                Debug.LogWarning("TaskExecutor: Cannot change graph while running!");
                return;
            }

            m_Graph = graph;
        }

        /// <summary>
        /// 获取当前图形
        /// </summary>
        public TaskRuntimeGraph GetGraph()
        {
            return m_Graph;
        }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning()
        {
            return m_IsRunning;
        }
    }
}
