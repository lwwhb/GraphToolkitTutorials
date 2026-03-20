using System.Collections.Generic;
using UnityEngine;

namespace GraphToolkitTutorials.ExecutionFlow.Runtime
{
    /// <summary>
    /// 任务执行器
    /// 负责执行运行时任务图形
    /// </summary>
    public class TaskScheduler : MonoBehaviour
    {
        [Header("Graph")]
        [SerializeField]
        private TaskRuntimeGraph m_Graph;

        [Header("Settings")]
        [SerializeField]
        private bool m_AutoStart = true;

        [SerializeField]
        private bool m_Loop = false;

        [SerializeField]
        private bool m_DelayMode = true;

        private bool m_IsRunning = false;
        private int m_CurrentNodeIndex = -1;
        private float m_WaitUntil = -1f;

        private void Start()
        {
            if (m_AutoStart && m_Graph != null)
            {
                m_Graph.SetBool("DelayMode", m_DelayMode);
                StartExecution();
            }
        }

        private void Update()
        {
            m_Graph?.SetBool("DelayMode", m_DelayMode);

            // 如果Loop为true且未在运行，自动重启
            if (!m_IsRunning)
            {
                if (m_Loop && m_Graph != null)
                {
                    m_CurrentNodeIndex = m_Graph.startNodeIndex;
                    m_WaitUntil = -1f;
                    m_IsRunning = true;
                }
                else
                {
                    return;
                }
            }

            if (m_Graph == null) return;

            // 等待延迟
            if (m_WaitUntil > 0f)
            {
                if (Time.time < m_WaitUntil) return;
                m_WaitUntil = -1f;
            }

            if (m_CurrentNodeIndex < 0)
            {
                Debug.Log("Task graph execution completed");
                m_IsRunning = false;
                return;
            }

            var node = m_Graph.GetNode(m_CurrentNodeIndex);
            if (node == null)
            {
                Debug.LogError($"TaskScheduler: Invalid node at index {m_CurrentNodeIndex}");
                m_IsRunning = false;
                return;
            }

            m_CurrentNodeIndex = ExecuteNode(node);
        }

        /// <summary>
        /// 执行单个节点，返回下一个节点索引
        /// </summary>
        private int ExecuteNode(TaskRuntimeNode node)
        {
            if (node is StartNode startNode)
            {
                return startNode.nextNodeIndex;
            }
            else if (node is LogNode logNode)
            {
                switch (logNode.logType)
                {
                    case LogType.Warning: Debug.LogWarning(logNode.message); break;
                    case LogType.Error:   Debug.LogError(logNode.message);   break;
                    default:              Debug.Log(logNode.message);         break;
                }
                return logNode.nextNodeIndex;
            }
            else if (node is DelayNode delayNode)
            {
                m_WaitUntil = Time.time + delayNode.duration;
                return delayNode.nextNodeIndex;
            }
            else if (node is BranchNode branchNode)
            {
                bool condition = string.IsNullOrEmpty(branchNode.conditionVariableName)
                    ? branchNode.condition
                    : m_Graph.GetBool(branchNode.conditionVariableName, branchNode.condition);

                Debug.Log($"Branch '{branchNode.conditionVariableName}': {condition}");
                return condition ? branchNode.trueNodeIndex : branchNode.falseNodeIndex;
            }

            Debug.LogError($"TaskScheduler: Unknown node type {node.GetType().Name}");
            return -1;
        }

        /// <summary>
        /// 开始执行图形
        /// </summary>
        public void StartExecution()
        {
            if (m_Graph == null)
            {
                Debug.LogError("TaskScheduler: No graph assigned!");
                return;
            }
            if (m_IsRunning)
            {
                Debug.LogWarning("TaskScheduler: Already running!");
                return;
            }

            m_CurrentNodeIndex = m_Graph.startNodeIndex;
            m_WaitUntil = -1f;
            m_IsRunning = true;
        }

        /// <summary>
        /// 停止执行
        /// </summary>
        public void StopExecution()
        {
            m_IsRunning = false;
            m_CurrentNodeIndex = -1;
            m_WaitUntil = -1f;
            Debug.Log("Task execution stopped");
        }

        /// <summary>
        /// 设置图形
        /// </summary>
        public void SetGraph(TaskRuntimeGraph graph)
        {
            if (m_IsRunning)
            {
                Debug.LogWarning("TaskScheduler: Cannot change graph while running!");
                return;
            }
            m_Graph = graph;
        }

        public TaskRuntimeGraph GetGraph() => m_Graph;
        public bool IsRunning() => m_IsRunning;

        /// <summary>
        /// 设置图形中的bool变量（可在运行时动态修改）
        /// </summary>
        public void SetBool(string name, bool value)
        {
            m_Graph?.SetBool(name, value);
        }

        /// <summary>
        /// 获取图形中的bool变量
        /// </summary>
        public bool GetBool(string name, bool defaultValue = false)
        {
            return m_Graph != null ? m_Graph.GetBool(name, defaultValue) : defaultValue;
        }
    }
}
