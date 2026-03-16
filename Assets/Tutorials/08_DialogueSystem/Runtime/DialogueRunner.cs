using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace GraphToolkitTutorials.DialogueSystem.Runtime
{
    /// <summary>
    /// 对话运行器
    /// 负责执行运行时对话图形
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
        public UnityEvent<string[]> OnChoice;
        public UnityEvent<string, string> OnEvent;
        public UnityEvent OnDialogueStart;
        public UnityEvent OnDialogueEnd;

        private DialogueVariables m_Variables;
        private bool m_IsRunning = false;
        private bool m_WaitingForInput = false;
        private int m_SelectedChoiceIndex = -1;

        private void Awake()
        {
            m_Variables = new DialogueVariables();
        }

        private void Start()
        {
            if (m_AutoStart && m_DialogueGraph != null)
            {
                StartDialogue();
            }
        }

        /// <summary>
        /// 开始对话
        /// </summary>
        public void StartDialogue()
        {
            if (m_DialogueGraph == null)
            {
                Debug.LogError("DialogueRunner: No dialogue graph assigned!");
                return;
            }

            if (m_IsRunning)
            {
                Debug.LogWarning("DialogueRunner: Dialogue already running!");
                return;
            }

            StartCoroutine(ExecuteDialogue());
        }

        /// <summary>
        /// 停止对话
        /// </summary>
        public void StopDialogue()
        {
            StopAllCoroutines();
            m_IsRunning = false;
            m_WaitingForInput = false;
            OnDialogueEnd?.Invoke();
        }

        /// <summary>
        /// 继续对话（用于对话文本节点）
        /// </summary>
        public void ContinueDialogue()
        {
            if (m_WaitingForInput)
            {
                m_WaitingForInput = false;
            }
        }

        /// <summary>
        /// 选择选项（用于选择节点）
        /// </summary>
        public void SelectChoice(int choiceIndex)
        {
            if (m_WaitingForInput)
            {
                m_SelectedChoiceIndex = choiceIndex;
                m_WaitingForInput = false;
            }
        }

        /// <summary>
        /// 执行对话图形
        /// </summary>
        private IEnumerator ExecuteDialogue()
        {
            m_IsRunning = true;
            OnDialogueStart?.Invoke();

            var startNode = m_DialogueGraph.GetStartNode();
            if (startNode == null)
            {
                Debug.LogError("DialogueRunner: No start node found!");
                yield break;
            }

            int currentNodeIndex = startNode.nextNodeIndex;

            while (currentNodeIndex >= 0)
            {
                var node = m_DialogueGraph.GetNode(currentNodeIndex);
                if (node == null)
                {
                    Debug.LogError($"DialogueRunner: Invalid node at index {currentNodeIndex}");
                    break;
                }

                // 执行节点
                int nextNodeIndex = -1;

                if (node is DialogueTextNode dialogueNode)
                {
                    nextNodeIndex = yield return ExecuteDialogueText(dialogueNode);
                }
                else if (node is ChoiceNode choiceNode)
                {
                    nextNodeIndex = yield return ExecuteChoice(choiceNode);
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

        /// <summary>
        /// 执行对话文本节点
        /// </summary>
        private IEnumerator ExecuteDialogueText(DialogueTextNode node)
        {
            // 触发对话文本事件
            OnDialogueText?.Invoke(node.speakerName, node.dialogueText, node.speakerPortrait);

            // 等待玩家输入
            m_WaitingForInput = true;
            while (m_WaitingForInput)
            {
                yield return null;
            }

            yield return node.nextNodeIndex;
        }

        /// <summary>
        /// 执行选择节点
        /// </summary>
        private IEnumerator ExecuteChoice(ChoiceNode node)
        {
            // 触发选择事件
            OnChoice?.Invoke(node.optionTexts);

            // 等待玩家选择
            m_WaitingForInput = true;
            m_SelectedChoiceIndex = -1;

            while (m_WaitingForInput)
            {
                yield return null;
            }

            // 返回选中的分支
            if (m_SelectedChoiceIndex >= 0 && m_SelectedChoiceIndex < node.nextNodeIndices.Length)
            {
                yield return node.nextNodeIndices[m_SelectedChoiceIndex];
            }
            else
            {
                yield return -1;
            }
        }

        /// <summary>
        /// 执行条件分支节点
        /// </summary>
        private int ExecuteBranch(BranchNode node)
        {
            string value = m_Variables.GetVariable(node.conditionKey, "");
            bool condition = value == node.expectedValue;

            return condition ? node.trueNodeIndex : node.falseNodeIndex;
        }

        /// <summary>
        /// 执行设置变量节点
        /// </summary>
        private int ExecuteSetVariable(SetVariableNode node)
        {
            m_Variables.SetVariable(node.variableKey, node.variableValue);
            return node.nextNodeIndex;
        }

        /// <summary>
        /// 执行事件节点
        /// </summary>
        private int ExecuteEvent(EventNode node)
        {
            OnEvent?.Invoke(node.eventName, node.eventParameter);
            return node.nextNodeIndex;
        }

        /// <summary>
        /// 设置对话图形
        /// </summary>
        public void SetDialogueGraph(DialogueRuntimeGraph graph)
        {
            if (m_IsRunning)
            {
                Debug.LogWarning("DialogueRunner: Cannot change graph while running!");
                return;
            }

            m_DialogueGraph = graph;
        }

        /// <summary>
        /// 获取变量存储
        /// </summary>
        public DialogueVariables GetVariables()
        {
            return m_Variables;
        }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning()
        {
            return m_IsRunning;
        }

        /// <summary>
        /// 是否等待输入
        /// </summary>
        public bool IsWaitingForInput()
        {
            return m_WaitingForInput;
        }
    }
}
ondition = value == node.expectedValue;

            return condition ? node.trueNodeIndex : node.falseNodeIndex;
        }

        /// <summary>
        /// 执行设置变量节点
        /// </summary>
        private int ExecuteSetVariable(SetVariableNode node)
        {
            m_Variables.SetVariable(node.variableKey, node.variableValue);
            return node.nextNodeIndex;
        }

        /// <summary>
        /// 执行事件节点
        /// </summary>
        private int ExecuteEvent(EventNode node)
        {
            OnEvent?.Invoke(node.eventName, node.eventParameter);
            return node.nextNodeIndex;
        }

        /// <summary>
        /// 设置对话图形
        /// </summary>
        public void SetDialogueGraph(DialogueRuntimeGraph graph)
        {
            if (m_IsRunning)
            {
                Debug.LogWarning("DialogueRunner: Cannot change graph while running!");
                return;
            }

            m_DialogueGraph = graph;
        }

        /// <summary>
        /// 获取变量存储
        /// </summary>
        public DialogueVariables GetVariables()
        {
            return m_Variables;
        }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning()
        {
            return m_IsRunning;
        }

        /// <summary>
        /// 是否等待输入
        /// </summary>
        public bool IsWaitingForInput()
        {
            return m_WaitingForInput;
        }

        /// <summary>
        /// 设置变量（便捷方法）
        /// </summary>
        public void SetVariable(string key, string value)
        {
            m_Variables.SetVariable(key, value);
        }

        /// <summary>
        /// 获取变量（便捷方法）
        /// </summary>
        public string GetVariable(string key, string defaultValue = "")
        {
            return m_Variables.GetVariable(key, defaultValue);
        }
    }
}
