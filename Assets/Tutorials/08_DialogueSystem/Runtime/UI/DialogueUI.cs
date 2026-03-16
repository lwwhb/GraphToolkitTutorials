using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GraphToolkitTutorials.DialogueSystem.Runtime
{
    /// <summary>
    /// 简单的对话UI
    /// 显示对话文本和选项
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField]
        private GameObject m_DialoguePanel;

        [SerializeField]
        private TextMeshProUGUI m_SpeakerNameText;

        [SerializeField]
        private TextMeshProUGUI m_DialogueText;

        [SerializeField]
        private Image m_SpeakerPortrait;

        [SerializeField]
        private Button m_ContinueButton;

        [SerializeField]
        private GameObject m_ChoicePanel;

        [SerializeField]
        private Button[] m_ChoiceButtons;

        [Header("Dialogue Runner")]
        [SerializeField]
        private DialogueRunner m_DialogueRunner;

        private void Awake()
        {
            if (m_DialogueRunner != null)
            {
                // 订阅对话事件
                m_DialogueRunner.OnDialogueStart.AddListener(OnDialogueStart);
                m_DialogueRunner.OnDialogueEnd.AddListener(OnDialogueEnd);
                m_DialogueRunner.OnDialogueText.AddListener(OnDialogueText);
                m_DialogueRunner.OnChoice.AddListener(OnChoice);
            }

            // 设置继续按钮
            if (m_ContinueButton != null)
            {
                m_ContinueButton.onClick.AddListener(OnContinueClicked);
            }

            // 设置选项按钮
            for (int i = 0; i < m_ChoiceButtons.Length; i++)
            {
                int index = i; // 闭包捕获
                m_ChoiceButtons[i].onClick.AddListener(() => OnChoiceClicked(index));
            }

            // 初始隐藏UI
            if (m_DialoguePanel != null)
            {
                m_DialoguePanel.SetActive(false);
            }
        }

        private void OnDialogueStart()
        {
            if (m_DialoguePanel != null)
            {
                m_DialoguePanel.SetActive(true);
            }
        }

        private void OnDialogueEnd()
        {
            if (m_DialoguePanel != null)
            {
                m_DialoguePanel.SetActive(false);
            }
        }

        private void OnDialogueText(string speakerName, string dialogueText, Sprite portrait)
        {
            // 显示对话文本
            if (m_SpeakerNameText != null)
            {
                m_SpeakerNameText.text = speakerName;
            }

            if (m_DialogueText != null)
            {
                m_DialogueText.text = dialogueText;
            }

            if (m_SpeakerPortrait != null && portrait != null)
            {
                m_SpeakerPortrait.sprite = portrait;
                m_SpeakerPortrait.gameObject.SetActive(true);
            }
            else if (m_SpeakerPortrait != null)
            {
                m_SpeakerPortrait.gameObject.SetActive(false);
            }

            // 显示继续按钮，隐藏选项
            if (m_ContinueButton != null)
            {
                m_ContinueButton.gameObject.SetActive(true);
            }

            if (m_ChoicePanel != null)
            {
                m_ChoicePanel.SetActive(false);
            }
        }

        private void OnChoice(string[] options)
        {
            // 隐藏继续按钮，显示选项
            if (m_ContinueButton != null)
            {
                m_ContinueButton.gameObject.SetActive(false);
            }

            if (m_ChoicePanel != null)
            {
                m_ChoicePanel.SetActive(true);
            }

            // 设置选项文本
            for (int i = 0; i < m_ChoiceButtons.Length; i++)
            {
                if (i < options.Length)
                {
                    m_ChoiceButtons[i].gameObject.SetActive(true);
                    var buttonText = m_ChoiceButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = options[i];
                    }
                }
                else
                {
                    m_ChoiceButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnContinueClicked()
        {
            if (m_DialogueRunner != null)
            {
                m_DialogueRunner.ContinueDialogue();
            }
        }

        private void OnChoiceClicked(int choiceIndex)
        {
            if (m_DialogueRunner != null)
            {
                m_DialogueRunner.SelectChoice(choiceIndex);
            }
        }
    }
}
