using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GraphToolkitTutorials.DialogueSystem.Runtime
{
    /// <summary>
    /// 对话系统测试脚本 — 挂载在与 DialogueRunner 相同的 GameObject 上。
    ///
    /// 运行时操作：
    ///   Space / Enter — 开始对话 / 继续对话文本
    ///   1 / 2         — 选择选项（ChoiceNode 等待输入时）
    ///
    /// 编辑器 Play Mode 下自动查找 TavernDialogue 资产并赋值。
    /// </summary>
    [RequireComponent(typeof(DialogueRunner))]
    public class DialogueTester : MonoBehaviour
    {
        [Tooltip("游戏启动后自动开始对话")]
        [SerializeField] private bool m_AutoStart = true;

        [Tooltip("编辑器下自动查找的对话资产名称（留空则加载第一个）")]
        [SerializeField] private string m_DialogueName = "";

        private DialogueRunner m_Runner;
        private string         m_CurrentSpeaker  = "";
        private string         m_CurrentText     = "";
        private string[]       m_CurrentOptions  = null;
        private Sprite         m_CurrentPortrait = null;
        private bool           m_ShowingDialogue  = false;
        private bool           m_ShowingChoice    = false;

        void Awake()
        {
            m_Runner = GetComponent<DialogueRunner>();

#if UNITY_EDITOR
            AutoLoadDialogue();
#endif

            m_Runner.OnDialogueStart.AddListener(OnDialogueStart);
            m_Runner.OnDialogueEnd.AddListener(OnDialogueEnd);
            m_Runner.OnDialogueText.AddListener(OnDialogueText);
            m_Runner.OnChoice.AddListener(OnChoice);
            m_Runner.OnEvent.AddListener(OnEvent);
        }

        void Start()
        {
            if (m_AutoStart)
                m_Runner.StartDialogue();
        }

        void Update()
        {
            bool spaceDown = Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame);
            bool key1Down  = Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame;
            bool key2Down  = Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame;
            if (!m_Runner.IsRunning() && spaceDown)
            {
                m_Runner.StartDialogue();
                return;
            }

            if (m_Runner.IsWaitingForInput())
            {
                if (m_ShowingChoice)
                {
                    if (key1Down) m_Runner.SelectChoice(0);
                    else if (key2Down) m_Runner.SelectChoice(1);
                }
                else if (spaceDown)
                {
                    m_Runner.ContinueDialogue();
                }
            }
        }

        void OnGUI()
        {
            if (!m_ShowingDialogue && !m_Runner.IsRunning())
            {
                GUI.Label(new Rect(10, 10, 500, 25), "[Space] 开始对话");
                return;
            }

            if (m_ShowingChoice && m_CurrentOptions != null)
            {
                for (int i = 0; i < m_CurrentOptions.Length; i++)
                    GUI.Label(new Rect(10, 10 + i * 25, 600, 25), $"[{i + 1}] {m_CurrentOptions[i]}");
            }
            else
            {
                float portraitSize = 80f;
                float textX = 10f;
                if (m_CurrentPortrait != null)
                {
                    var tex = m_CurrentPortrait.texture;
                    var sr  = m_CurrentPortrait.rect;
                    GUI.DrawTextureWithTexCoords(
                        new Rect(10, 10, portraitSize, portraitSize),
                        tex,
                        new Rect(sr.x / tex.width, sr.y / tex.height, sr.width / tex.width, sr.height / tex.height));
                    textX = 10f + portraitSize + 8f;
                }
                GUI.Label(new Rect(textX, 10, 600, 25), $"{m_CurrentSpeaker}: {m_CurrentText}");
                if (m_Runner.IsWaitingForInput())
                    GUI.Label(new Rect(textX, 40, 400, 25), "[Space/Enter] 继续");
            }
        }

        // ──────────────────────────────────────────────────────────────────
        private void OnDialogueStart()
        {
            m_ShowingDialogue = true;
            m_ShowingChoice   = false;
            Debug.Log("[DialogueTester] 对话开始");
        }

        private void OnDialogueEnd()
        {
            m_ShowingDialogue = false;
            m_ShowingChoice   = false;
            m_CurrentOptions  = null;
            m_CurrentPortrait = null;
            Debug.Log("[DialogueTester] 对话结束");
        }

        private void OnDialogueText(string speakerName, string text, Sprite portrait)
        {
            m_ShowingChoice  = false;
            m_CurrentSpeaker = speakerName;
            m_CurrentText    = text;
            m_CurrentPortrait = portrait;
            Debug.Log($"[DialogueTester] {speakerName}: {text}");
        }

        private void OnChoice(string[] options)
        {
            m_ShowingChoice  = true;
            m_CurrentOptions = options;
            for (int i = 0; i < options.Length; i++)
                Debug.Log($"[DialogueTester] 选项 {i + 1}: {options[i]}");
        }

        private void OnEvent(string eventName, string param)
        {
            Debug.Log($"[DialogueTester] 事件触发: {eventName} ({param})");
        }

#if UNITY_EDITOR
        private void AutoLoadDialogue()
        {
            if (m_Runner.GetVariables() != null && m_Runner.IsRunning())
                return;

            string filter = string.IsNullOrEmpty(m_DialogueName)
                ? "t:DialogueRuntimeGraph"
                : $"{m_DialogueName} t:DialogueRuntimeGraph";

            var guids = AssetDatabase.FindAssets(filter);
            if (guids.Length == 0)
            {
                Debug.LogWarning("[DialogueTester] 未找到 DialogueRuntimeGraph 资产");
                return;
            }

            var path  = AssetDatabase.GUIDToAssetPath(guids[0]);
            var graph = AssetDatabase.LoadAssetAtPath<DialogueRuntimeGraph>(path);
            m_Runner.SetDialogueGraph(graph);
            Debug.Log($"[DialogueTester] 自动加载对话: {path}");
        }
#endif
    }
}
