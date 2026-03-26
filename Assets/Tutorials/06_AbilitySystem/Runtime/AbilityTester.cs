using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GraphToolkitTutorials.AbilitySystem.Runtime
{
    /// <summary>
    /// 技能系统测试脚本 — 挂载在与 AbilityRunner 相同的 GameObject 上。
    ///
    /// 运行时操作：
    ///   Space   → FireEvent(m_EventName)
    ///   R       → 重复触发（测试并发）
    ///
    /// Inspector 参数：
    ///   Event Name   — 要触发的事件名，默认 "Attack"
    ///   Fire On Start — true 时游戏开始自动触发一次
    /// </summary>
    [RequireComponent(typeof(AbilityRunner))]
    public class AbilityTester : MonoBehaviour
    {
        [Tooltip("要触发的事件名，需与图中 On Event 节点的 Event Name 一致")]
        [SerializeField] private string m_EventName = "Attack";

        [Tooltip("游戏启动后自动触发一次")]
        [SerializeField] private bool m_FireOnStart = true;

        private AbilityRunner m_Runner;

        void Awake()
        {
            m_Runner = GetComponent<AbilityRunner>();

#if UNITY_EDITOR
            // 编辑器 Play Mode 下，若 graph 未赋值则自动查找示例图
            if (m_Runner.graph == null)
            {
                var guids = AssetDatabase.FindAssets("AbilitySystemGraph t:AbilityRuntimeGraph");
                if (guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    m_Runner.graph = AssetDatabase.LoadAssetAtPath<AbilityRuntimeGraph>(path);
                    Debug.Log($"[AbilityTester] 自动加载图: {path}");
                }
                else
                {
                    Debug.LogWarning("[AbilityTester] 未找到 AbilitySystemGraph，请手动赋值 AbilityRunner.graph");
                }
            }
#endif
        }

        void Start()
        {
            if (m_FireOnStart)
            {
                Debug.Log($"[AbilityTester] Start → FireEvent(\"{m_EventName}\")");
                m_Runner.FireEvent(m_EventName);
            }
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.spaceKey.wasPressedThisFrame)
            {
                Debug.Log($"[AbilityTester] Space → FireEvent(\"{m_EventName}\")");
                m_Runner.FireEvent(m_EventName);
            }

            if (kb.rKey.wasPressedThisFrame)
            {
                Debug.Log("[AbilityTester] R → 连续触发两次（测试并发协程）");
                m_Runner.FireEvent(m_EventName);
                m_Runner.FireEvent(m_EventName);
            }
        }

        void OnGUI()
        {
            GUI.Label(new Rect(10, 10, 400, 25), $"[Space] FireEvent(\"{m_EventName}\")");
            GUI.Label(new Rect(10, 35, 400, 25), "[R] 连续触发两次（测试并发）");
        }
    }
}
