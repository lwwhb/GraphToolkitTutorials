using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace GraphToolkitTutorials.DialogueSystem.Runtime
{
    /// <summary>
    /// 对话运行器 — 驱动 DialogueRuntimeGraph 执行。
    ///
    /// 外部通过 UnityEvent 响应对话内容：
    ///   OnDialogueText  — 收到文本行（说话人、内容、头像）
    ///   OnChoice        — 收到选项列表；调用 SelectChoice(index) 继续
    ///   OnEvent         — 收到事件触发（事件名、参数）
    ///   OnDialogueStart / OnDialogueEnd — 对话开始/结束
    ///
    /// 继续流程：
    ///   对话文本 → 调用 ContinueDialogue()
    ///   选择     → 调用 SelectChoice(int index)
    /// </summary>
    public class DialogueRunner : MonoBehaviour
    {
        [Header("Dialogue")]
        [SerializeField]
        private DialogueRuntimeGraph m_DialogueGraph;

        [Header("Settings")]
        [SerializeField]
        private bool m_AutoStart = false;

        [Header("Events")]
        public UnityEvent<string, string, Sprite> OnDialogueText;
        public UnityEvent<string[]>               OnChoice;
        public UnityEvent<string, string>         OnEvent;
        public UnityEvent                         OnDialogueStart;
        public UnityEvent                         OnDialogueEnd;

        private DialogueVariables m_Variables;
        private bool m_IsRunning       = false;
        private bool m_WaitingForInput = false;
        private int  m_SelectedChoiceIndex = -1;
        private int  m_LastChoiceIndex     = -1;   // ChoiceNode 执行完后记录选中分支

        private void Awake()
        {
            m_Variables = new DialogueVariables();
        }

        private void Start()
        {
            if (m_AutoStart && m_DialogueGraph != null)
                StartDialogue();
        }

        // ──────────────────────────────────────────────────────────────────
        // 公共 API
        // ──────────────────────────────────────────────────────────────────

        public void StartDialogue()
        {
            if (m_DialogueGraph == null)
            {
                Debug.LogError("[DialogueRunner] No dialogue graph assigned!");
                return;
            }
            if (m_IsRunning)
            {
                Debug.LogWarning("[DialogueRunner] Dialogue already running!");
                return;
            }
            StartCoroutine(ExecuteDialogue());
        }

        public void StopDialogue()
        {
            StopAllCoroutines();
            m_IsRunning = false;
            m_WaitingForInput = false;
            OnDialogueEnd?.Invoke();
        }

        /// <summary>对话文本节点显示后，调用此方法继续执行。</summary>
        public void ContinueDialogue()
        {
            if (m_WaitingForInput)
                m_WaitingForInput = false;
        }

        /// <summary>选择节点显示选项后，调用此方法传入玩家选择的索引。</summary>
        public void SelectChoice(int choiceIndex)
        {
            if (m_WaitingForInput)
            {
                m_SelectedChoiceIndex = choiceIndex;
                m_WaitingForInput = false;
            }
        }

        public void SetDialogueGraph(DialogueRuntimeGraph graph)
        {
            if (m_IsRunning)
            {
                Debug.LogWarning("[DialogueRunner] Cannot change graph while running!");
                return;
            }
            m_DialogueGraph = graph;
        }

        public DialogueVariables GetVariables() => m_Variables;
        public bool IsRunning()        => m_IsRunning;
        public bool IsWaitingForInput()=> m_WaitingForInput;

        // ──────────────────────────────────────────────────────────────────
        // 内部执行协程
        // ──────────────────────────────────────────────────────────────────

        private IEnumerator ExecuteDialogue()
        {
            m_IsRunning = true;
            OnDialogueStart?.Invoke();

            var startNode = m_DialogueGraph.GetStartNode();
            if (startNode == null)
            {
                Debug.LogError("[DialogueRunner] No start node found!");
                yield break;
            }

            int currentNodeIndex = startNode.nextNodeIndex;

            while (currentNodeIndex >= 0)
            {
                var node = m_DialogueGraph.GetNode(currentNodeIndex);
                if (node == null)
                {
                    Debug.LogError($"[DialogueRunner] Invalid node at index {currentNodeIndex}");
                    break;
                }

                int nextNodeIndex = -1;

                if (node is DialogueTextNode dialogueNode)
                {
                    yield return StartCoroutine(ExecuteDialogueText(dialogueNode));
                    nextNodeIndex = dialogueNode.nextNodeIndex;
                }
                else if (node is ChoiceNode choiceNode)
                {
                    yield return StartCoroutine(ExecuteChoice(choiceNode));
                    nextNodeIndex = m_LastChoiceIndex;
                }
                else if (node is BranchNode branchNode)
                {
                    nextNodeIndex = ExecuteBranch(branchNode);
                }
                else if (node is SetVariableNode setVarNode)
                {
                    nextNodeIndex = ExecuteSetVariable(setVarNode);
                }
                else if (node is EventNode eventNode)
                {
                    nextNodeIndex = ExecuteEvent(eventNode);
                }
                else if (node is EndNode)
                {
                    nextNodeIndex = -1;
                }

                currentNodeIndex = nextNodeIndex;
            }

            m_IsRunning = false;
            OnDialogueEnd?.Invoke();
        }

        private IEnumerator ExecuteDialogueText(DialogueTextNode node)
        {
            OnDialogueText?.Invoke(node.speakerName, node.dialogueText, node.speakerPortrait);

            m_WaitingForInput = true;
            while (m_WaitingForInput)
                yield return null;
        }

        private IEnumerator ExecuteChoice(ChoiceNode node)
        {
            OnChoice?.Invoke(node.optionTexts);

            m_WaitingForInput = true;
            m_SelectedChoiceIndex = -1;
            while (m_WaitingForInput)
                yield return null;

            // 将选中分支的节点索引存入 m_LastChoiceIndex，供 ExecuteDialogue 读取
            if (m_SelectedChoiceIndex >= 0 && m_SelectedChoiceIndex < node.nextNodeIndices.Length)
                m_LastChoiceIndex = node.nextNodeIndices[m_SelectedChoiceIndex];
            else
                m_LastChoiceIndex = -1;
        }

        private int ExecuteBranch(BranchNode node)
        {
            string value = m_Variables.GetVariable(node.conditionKey, "");
            return value == node.expectedValue ? node.trueNodeIndex : node.falseNodeIndex;
        }

        private int ExecuteSetVariable(SetVariableNode node)
        {
            m_Variables.SetVariable(node.variableKey, node.variableValue);
            return node.nextNodeIndex;
        }

        private int ExecuteEvent(EventNode node)
        {
            OnEvent?.Invoke(node.eventName, node.eventParameter);
            return node.nextNodeIndex;
        }
    }
}
