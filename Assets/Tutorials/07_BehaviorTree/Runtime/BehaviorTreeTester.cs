using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GraphToolkitTutorials.BehaviorTree.Runtime
{
    /// <summary>
    /// 行为树测试脚本 — 挂载在与 BehaviorTreeRunner 相同的 GameObject 上。
    ///
    /// 运行时操作：
    ///   Space   → 启动/重启行为树
    ///   S       → 停止行为树
    ///
    /// Inspector 参数：
    ///   Auto Start    — true 时游戏开始自动启动行为树
    ///   Tree Name     — 编辑器下自动加载时匹配的资产名称（留空则加载第一个找到的）
    /// </summary>
    [RequireComponent(typeof(BehaviorTreeRunner))]
    public class BehaviorTreeTester : MonoBehaviour
    {
        [Tooltip("游戏启动后自动运行行为树")]
        [SerializeField] private bool m_AutoStart = true;

        [Tooltip("编辑器 Play Mode 下自动查找的行为树资产名称（留空则加载第一个）")]
        [SerializeField] private string m_TreeName = "";

        private BehaviorTreeRunner m_Runner;

        void Awake()
        {
            m_Runner = GetComponent<BehaviorTreeRunner>();

#if UNITY_EDITOR
            // 编辑器 Play Mode 下，若未赋值则自动查找示例行为树
            AutoLoadTree();
#endif
        }

        void Start()
        {
            if (m_AutoStart)
            {
                Debug.Log("[BehaviorTreeTester] Start → StartBehaviorTree()");
                m_Runner.StartBehaviorTree();
            }
        }

        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            bool spaceDown = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            bool sDown     = Keyboard.current != null && Keyboard.current.sKey.wasPressedThisFrame;
#else
            bool spaceDown = Input.GetKeyDown(KeyCode.Space);
            bool sDown     = Input.GetKeyDown(KeyCode.S);
#endif
            if (spaceDown)
            {
                if (m_Runner.IsRunning())
                {
                    Debug.Log("[BehaviorTreeTester] Space → StopBehaviorTree()");
                    m_Runner.StopBehaviorTree();
                }
                else
                {
                    Debug.Log("[BehaviorTreeTester] Space → StartBehaviorTree()");
                    m_Runner.StartBehaviorTree();
                }
            }

            if (sDown)
            {
                Debug.Log("[BehaviorTreeTester] S → StopBehaviorTree()");
                m_Runner.StopBehaviorTree();
            }
        }

        void OnGUI()
        {
            string status = m_Runner != null && m_Runner.IsRunning() ? "Running" : "Stopped";
            GUI.Label(new Rect(10, 10, 400, 25), $"[Space] 启动/停止行为树  状态: {status}");
            GUI.Label(new Rect(10, 35, 400, 25), "[S] 停止行为树");
            GUI.Label(new Rect(10, 60, 400, 25), "详细日志请查看 Console 窗口");
        }

#if UNITY_EDITOR
        private void AutoLoadTree()
        {
            // 通过 Reflection 检查 m_BehaviorTree 是否为 null（避免直接访问 private 字段）
            var field = typeof(BehaviorTreeRunner).GetField(
                "m_BehaviorTree",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field == null || field.GetValue(m_Runner) != null)
                return;

            // 搜索 BehaviorTreeRuntime 资产
            string filter = string.IsNullOrEmpty(m_TreeName)
                ? "t:BehaviorTreeRuntime"
                : $"{m_TreeName} t:BehaviorTreeRuntime";

            var guids = AssetDatabase.FindAssets(filter);
            if (guids.Length == 0)
            {
                Debug.LogWarning("[BehaviorTreeTester] 未找到 BehaviorTreeRuntime 资产，请先在 Project 窗口创建 .behaviortree 文件并赋值给 BehaviorTreeRunner");
                return;
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var tree = AssetDatabase.LoadAssetAtPath<BehaviorTreeRuntime>(path);
            m_Runner.SetBehaviorTree(tree);
            Debug.Log($"[BehaviorTreeTester] 自动加载行为树: {path}");
        }
#endif
    }
}
