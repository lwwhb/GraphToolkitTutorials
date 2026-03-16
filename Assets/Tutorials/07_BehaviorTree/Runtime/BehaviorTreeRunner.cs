using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GraphToolkitTutorials.BehaviorTree.Runtime
{
    /// <summary>
    /// 行为树运行器
    /// 负责执行运行时行为树
    /// </summary>
    public class BehaviorTreeRunner : MonoBehaviour
    {
        [Header("Behavior Tree")]
        [SerializeField]
        private BehaviorTreeRuntime m_BehaviorTree;

        [Header("Settings")]
        [SerializeField]
        private bool m_AutoStart = true;

        [SerializeField]
        private bool m_Loop = false;

        [Header("Debug")]
        [SerializeField]
        private bool m_ShowDebugInfo = true;

        private Blackboard m_Blackboard;
        private bool m_IsRunning = false;
        private static Dictionary<System.Type, IBTExecutor> s_Executors;

        private void Awake()
        {
            InitializeExecutors();
            m_Blackboard = new Blackboard();
        }

        private void Start()
        {
            if (m_AutoStart && m_BehaviorTree != null)
            {
                StartBehaviorTree();
            }
        }

        /// <summary>
        /// 初始化执行器映射
        /// </summary>
        private static void InitializeExecutors()
        {
            if (s_Executors != null)
                return;

            s_Executors = new Dictionary<System.Type, IBTExecutor>
            {
                // Composite nodes
                { typeof(SequenceNode), new SequenceExecutor() },
                { typeof(SelectorNode), new SelectorExecutor() },
                { typeof(ParallelNode), new ParallelExecutor() },

                // Decorator nodes
                { typeof(InverterNode), new InverterExecutor() },
                { typeof(RepeaterNode), new RepeaterExecutor() },
                { typeof(SucceederNode), new SucceederExecutor() },
                { typeof(ConditionalNode), new ConditionalExecutor() },

                // Leaf nodes
                { typeof(WaitNode), new WaitExecutor() },
                { typeof(LogNode), new LogExecutor() },
                { typeof(SetBlackboardValueNode), new SetBlackboardValueExecutor() },
                { typeof(CheckBlackboardValueNode), new CheckBlackboardValueExecutor() },
                { typeof(RandomSuccessNode), new RandomSuccessExecutor() }
            };
        }

        /// <summary>
        /// 获取节点的执行器
        /// </summary>
        public static IBTExecutor GetExecutor(BTRuntimeNode node)
        {
            if (node == null)
                return null;

            var nodeType = node.GetType();
            if (s_Executors.TryGetValue(nodeType, out var executor))
            {
                return executor;
            }

            Debug.LogError($"No executor found for node type: {nodeType.Name}");
            return null;
        }

        /// <summary>
        /// 开始执行行为树
        /// </summary>
        public void StartBehaviorTree()
        {
            if (m_BehaviorTree == null)
            {
                Debug.LogError("BehaviorTreeRunner: No behavior tree assigned!");
                return;
            }

            if (m_IsRunning)
            {
                Debug.LogWarning("BehaviorTreeRunner: Already running!");
                return;
            }

            StartCoroutine(ExecuteBehaviorTree());
        }

        /// <summary>
        /// 停止执行
        /// </summary>
        public void StopBehaviorTree()
        {
            StopAllCoroutines();
            m_IsRunning = false;

            if (m_ShowDebugInfo)
            {
                Debug.Log("Behavior tree stopped");
            }
        }

        /// <summary>
        /// 执行行为树
        /// </summary>
        private IEnumerator ExecuteBehaviorTree()
        {
            m_IsRunning = true;

            do
            {
                var rootNode = m_BehaviorTree.GetRootNode();
                if (rootNode == null)
                {
                    Debug.LogError("BehaviorTreeRunner: No root node found!");
                    yield break;
                }

                if (rootNode.childIndex < 0)
                {
                    Debug.LogError("BehaviorTreeRunner: Root node has no child!");
                    yield break;
                }

                if (m_ShowDebugInfo)
                {
                    Debug.Log("=== Behavior Tree Started ===");
                }

                // 执行根节点的子节点
                var executor = GetExecutor(m_BehaviorTree.GetNode(rootNode.childIndex));
                if (executor == null)
                {
                    Debug.LogError("BehaviorTreeRunner: No executor for root child!");
                    yield break;
                }

                var executionCoroutine = executor.Execute(m_BehaviorTree, rootNode.childIndex, m_Blackboard);
                NodeStatus finalStatus = NodeStatus.Running;

                while (executionCoroutine.MoveNext())
                {
                    var current = executionCoroutine.Current;

                    if (current is NodeStatus status)
                    {
                        finalStatus = status;
                    }
                    else
                    {
                        yield return current;
                    }
                }

                if (m_ShowDebugInfo)
                {
                    Debug.Log($"=== Behavior Tree Completed: {finalStatus} ===");
                }

            } while (m_Loop);

            m_IsRunning = false;
        }

        /// <summary>
        /// 设置行为树
        /// </summary>
        public void SetBehaviorTree(BehaviorTreeRuntime tree)
        {
            if (m_IsRunning)
            {
                Debug.LogWarning("BehaviorTreeRunner: Cannot change tree while running!");
                return;
            }

            m_BehaviorTree = tree;
        }

        /// <summary>
        /// 获取黑板
        /// </summary>
        public Blackboard GetBlackboard()
        {
            return m_Blackboard;
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
