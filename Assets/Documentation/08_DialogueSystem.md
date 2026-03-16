# 教程8: 对话系统

## 概述

本教程实现一个完整的游戏对话系统，支持分支对话、条件判断、变量管理和UI集成。这是对VisualNovelDirector示例的扩展和改进，适用于RPG、视觉小说等游戏类型。

### 学习目标

- 掌握执行流图形的高级应用
- 实现分支对话和条件系统
- 学习变量管理和状态持久化
- 集成Unity UI系统
- 理解对话系统的生产级设计

### 前置知识

- 完成教程3（执行流图形）
- 了解Unity UI基础
- 熟悉协程

---

## 对话系统设计

### 核心概念

**对话图（Dialogue Graph）**:
- 节点代表对话中的每一步
- 连接代表对话的流向
- 支持线性对话和分支对话

**节点类型**:

| 节点 | 功能 |
|------|------|
| Start | 对话起点 |
| Dialogue | 显示对话文本和说话者 |
| Choice | 玩家选择分支 |
| Branch | 条件分支（根据变量） |
| SetVariable | 设置变量值 |
| TriggerEvent | 触发游戏事件 |
| End | 对话结束 |

---

## 项目结构

```
Assets/Tutorials/08_DialogueSystem/
├─ Editor/
│  ├─ DialogueGraph.cs                  # 对话图形定义
│  ├─ DialogueImporter.cs               # 资产导入器（.dialogue）
│  ├─ Nodes/
│  │  ├─ DialogueNode.cs                # 节点基类
│  │  ├─ BasicNodes.cs                  # Start, Dialogue, End
│  │  ├─ BranchNodes.cs                 # Choice, Branch
│  │  └─ ActionNodes.cs                 # SetVariable, TriggerEvent
│  └─ Unity.GraphToolkit.Tutorials.DialogueSystem.Editor.asmdef
├─ Runtime/
│  ├─ DialogueRuntimeGraph.cs           # 运行时图形
│  ├─ DialogueRunner.cs                 # 对话执行器（MonoBehaviour）
│  ├─ DialogueVariables.cs              # 变量管理系统
│  ├─ Nodes/
│  │  └─ RuntimeNodes.cs                # 运行时节点定义
│  ├─ UI/
│  │  └─ DialogueUI.cs                  # UI控制器
│  └─ Unity.GraphToolkit.Tutorials.DialogueSystem.Runtime.asmdef
└─ Examples/
   ├─ QuestDialogue.dialogue            # 任务对话示例
   └─ NPCConversation.dialogue          # NPC对话示例
```

---

## 核心实现

### 1. 对话节点基类

```csharp
using Unity.GraphToolkit.Editor;

namespace GraphToolkitTutorials.DialogueSystem
{
    /// <summary>
    /// 对话节点基类 - 所有对话节点继承此类
    /// </summary>
    internal abstract class DialogueNode : Node
    {
        protected IPort m_InputPort;

        /// <summary>
        /// 添加输入端口（执行流入口）
        /// </summary>
        protected void AddInputPort(IPortDefinitionContext context)
        {
            m_InputPort = context.AddInputPort("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        /// <summary>
        /// 创建对应的运行时节点
        /// </summary>
        public abstract Runtime.DialogueRuntimeNode CreateRuntimeNode(DialogueGraph graph);

        /// <summary>
        /// 获取节点在图形中的索引
        /// </summary>
        public int GetNodeIndex(DialogueGraph graph)
        {
            return graph.Nodes.IndexOf(this);
        }
    }
}
```

### 2. 对话文本节点

```csharp
/// <summary>
/// 对话文本节点 - 显示说话者和对话内容
/// </summary>
[Node("Dialogue", "Dialogue")]
[UseWithGraph(typeof(DialogueGraph))]
internal class DialogueTextNode : DialogueNode
{
    [SerializeField] private string m_SpeakerName = "Character";
    [SerializeField][TextArea(3, 10)] private string m_DialogueText = "Hello!";
    [SerializeField] private Sprite m_SpeakerPortrait;

    private IPort m_OutputPort;

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        AddInputPort(context);
        m_OutputPort = context.AddOutputPort("Out")
            .WithConnectorUI(PortConnectorUI.Arrowhead)
            .Build();
    }

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption("Speaker Name", () => m_SpeakerName, v => m_SpeakerName = v)
            .Delayed().Build();
        context.AddOption("Dialogue Text", () => m_DialogueText, v => m_DialogueText = v)
            .Delayed().Build();
        context.AddOption("Portrait", () => m_SpeakerPortrait, v => m_SpeakerPortrait = v)
            .Build();
    }

    public override Runtime.DialogueRuntimeNode CreateRuntimeNode(DialogueGraph graph)
    {
        var runtimeNode = new Runtime.DialogueTextNode
        {
            speakerName = m_SpeakerName,
            dialogueText = m_DialogueText,
            speakerPortrait = m_SpeakerPortrait
        };

        var connectedPort = graph.GetConnectedInputPort(m_OutputPort);
        if (connectedPort?.Node is DialogueNode nextNode)
            runtimeNode.nextNodeIndex = nextNode.GetNodeIndex(graph);
        else
            runtimeNode.nextNodeIndex = -1;

        return runtimeNode;
    }
}
```

### 3. 选择节点（玩家分支）

```csharp
/// <summary>
/// 选择节点 - 显示多个选项供玩家选择
/// </summary>
[Node("Choice", "Dialogue")]
[UseWithGraph(typeof(DialogueGraph))]
internal class ChoiceNode : DialogueNode
{
    [System.Serializable]
    public class ChoiceOption
    {
        public string text = "Option";
        public string portName = "Option";
    }

    [SerializeField]
    private List<ChoiceOption> m_Options = new List<ChoiceOption>
    {
        new ChoiceOption { text = "Option 1", portName = "Option 1" },
        new ChoiceOption { text = "Option 2", portName = "Option 2" }
    };

    private List<IPort> m_OutputPorts = new List<IPort>();

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        AddInputPort(context);
        m_OutputPorts.Clear();
        foreach (var option in m_Options)
        {
            var port = context.AddOutputPort(option.portName)
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
            m_OutputPorts.Add(port);
        }
    }

    public override Runtime.DialogueRuntimeNode CreateRuntimeNode(DialogueGraph graph)
    {
        var runtimeNode = new Runtime.ChoiceNode();
        runtimeNode.optionTexts = new string[m_Options.Count];
        runtimeNode.nextNodeIndices = new int[m_Options.Count];

        for (int i = 0; i < m_Options.Count; i++)
        {
            runtimeNode.optionTexts[i] = m_Options[i].text;

            var connectedPort = graph.GetConnectedInputPort(m_OutputPorts[i]);
            if (connectedPort?.Node is DialogueNode nextNode)
                runtimeNode.nextNodeIndices[i] = nextNode.GetNodeIndex(graph);
            else
                runtimeNode.nextNodeIndices[i] = -1;
        }

        return runtimeNode;
    }
}
```

### 4. 变量管理系统

```csharp
using System.Collections.Generic;

namespace GraphToolkitTutorials.DialogueSystem.Runtime
{
    /// <summary>
    /// 对话变量管理器 - 存储对话过程中的状态
    /// </summary>
    public class DialogueVariables
    {
        private Dictionary<string, string> m_Variables = new Dictionary<string, string>();

        public void Set(string key, string value) => m_Variables[key] = value;

        public string Get(string key, string defaultValue = "")
        {
            return m_Variables.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public bool Has(string key) => m_Variables.ContainsKey(key);

        /// <summary>
        /// 检查条件：key == expectedValue
        /// </summary>
        public bool CheckCondition(string key, string expectedValue)
        {
            return Get(key) == expectedValue;
        }
    }
}
```

### 5. 对话运行器

```csharp
using System.Collections;
using UnityEngine;

namespace GraphToolkitTutorials.DialogueSystem.Runtime
{
    /// <summary>
    /// 对话运行器 - 驱动对话系统执行
    /// </summary>
    public class DialogueRunner : MonoBehaviour
    {
        [SerializeField] private DialogueGraph m_DialogueAsset;
        [SerializeField] private DialogueUI m_DialogueUI;

        private DialogueRuntimeGraph m_Runtime;
        private DialogueVariables m_Variables;
        private int m_CurrentNodeIndex;
        private bool m_WaitingForInput;
        private int m_SelectedChoice = -1;

        public DialogueVariables Variables => m_Variables;

        void Start()
        {
            m_Variables = new DialogueVariables();
            if (m_DialogueAsset != null)
                StartDialogue(m_DialogueAsset);
        }

        public void StartDialogue(DialogueGraph asset)
        {
            m_Runtime = asset.CreateRuntime();
            m_CurrentNodeIndex = 0;
            StartCoroutine(RunDialogue());
        }

        private IEnumerator RunDialogue()
        {
            m_DialogueUI.Show();

            while (m_CurrentNodeIndex >= 0)
            {
                var node = m_Runtime.GetNode(m_CurrentNodeIndex);
                yield return StartCoroutine(ExecuteNode(node));
            }

            m_DialogueUI.Hide();
        }

        private IEnumerator ExecuteNode(DialogueRuntimeNode node)
        {
            switch (node)
            {
                case StartNode startNode:
                    m_CurrentNodeIndex = startNode.nextNodeIndex;
                    break;

                case DialogueTextNode textNode:
                    m_DialogueUI.ShowDialogue(textNode.speakerName,
                                              textNode.dialogueText,
                                              textNode.speakerPortrait);
                    m_WaitingForInput = true;
                    yield return new WaitUntil(() => !m_WaitingForInput);
                    m_CurrentNodeIndex = textNode.nextNodeIndex;
                    break;

                case ChoiceNode choiceNode:
                    m_DialogueUI.ShowChoices(choiceNode.optionTexts, OnChoiceSelected);
                    m_SelectedChoice = -1;
                    yield return new WaitUntil(() => m_SelectedChoice >= 0);
                    m_CurrentNodeIndex = choiceNode.nextNodeIndices[m_SelectedChoice];
                    break;

                case BranchNode branchNode:
                    bool condition = m_Variables.CheckCondition(
                        branchNode.conditionKey, branchNode.expectedValue);
                    m_CurrentNodeIndex = condition
                        ? branchNode.trueNodeIndex
                        : branchNode.falseNodeIndex;
                    break;

                case SetVariableNode setVarNode:
                    m_Variables.Set(setVarNode.variableKey, setVarNode.variableValue);
                    m_CurrentNodeIndex = setVarNode.nextNodeIndex;
                    break;

                case EndNode:
                    m_CurrentNodeIndex = -1;
                    break;
            }
        }

        public void OnNextPressed() => m_WaitingForInput = false;
        private void OnChoiceSelected(int index) => m_SelectedChoice = index;
    }
}
```

---

## UI集成

### DialogueUI组件

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GraphToolkitTutorials.DialogueSystem.Runtime
{
    /// <summary>
    /// 对话UI控制器
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [Header("对话面板")]
        [SerializeField] private GameObject m_DialoguePanel;
        [SerializeField] private TextMeshProUGUI m_SpeakerNameText;
        [SerializeField] private TextMeshProUGUI m_DialogueText;
        [SerializeField] private Image m_PortraitImage;

        [Header("选择面板")]
        [SerializeField] private GameObject m_ChoicePanel;
        [SerializeField] private Button[] m_ChoiceButtons;
        [SerializeField] private TextMeshProUGUI[] m_ChoiceTexts;

        public void Show() => m_DialoguePanel.SetActive(true);
        public void Hide() => m_DialoguePanel.SetActive(false);

        public void ShowDialogue(string speaker, string text, Sprite portrait)
        {
            m_ChoicePanel.SetActive(false);
            m_SpeakerNameText.text = speaker;
            m_DialogueText.text = text;
            m_PortraitImage.sprite = portrait;
            m_PortraitImage.gameObject.SetActive(portrait != null);
        }

        public void ShowChoices(string[] options, System.Action<int> onSelected)
        {
            m_ChoicePanel.SetActive(true);
            for (int i = 0; i < m_ChoiceButtons.Length; i++)
            {
                bool active = i < options.Length;
                m_ChoiceButtons[i].gameObject.SetActive(active);
                if (active)
                {
                    m_ChoiceTexts[i].text = options[i];
                    int index = i;
                    m_ChoiceButtons[i].onClick.RemoveAllListeners();
                    m_ChoiceButtons[i].onClick.AddListener(() => onSelected(index));
                }
            }
        }
    }
}
```

---

## 对话图示例

### 简单线性对话
```
[Start] → [Dialogue: "你好，旅行者！"] → [Dialogue: "欢迎来到这个村庄。"] → [End]
```

### 分支对话
```
[Start]
  └─ [Dialogue: "你想要什么？"]
       └─ [Choice]
            ├─ "购买物品" → [Dialogue: "好的，这是我的商品。"] → [End]
            ├─ "接受任务" → [SetVariable: quest=accepted] → [Dialogue: "谢谢你！"] → [End]
            └─ "离开"    → [End]
```

### 条件分支
```
[Start]
  └─ [Branch: quest == "completed"]
       ├─ True  → [Dialogue: "感谢你完成了任务！"] → [End]
       └─ False → [Dialogue: "任务还没完成呢。"]   → [End]
```

---

## 使用步骤

1. 创建 `.dialogue` 资产文件
2. 在图形编辑器中设计对话流程
3. 创建UI Canvas，添加 `DialogueUI` 组件
4. 在场景中添加 `DialogueRunner` 组件
5. 将资产和UI引用赋值给 `DialogueRunner`
6. 运行游戏，按下 Next 按钮推进对话

---

## 练习题

1. **基础**: 创建一个包含3个对话节点的线性对话
2. **进阶**: 添加玩家选择，根据选择设置不同的变量
3. **挑战**: 实现一个完整的任务对话，包含接受任务、完成检测和奖励发放

---

## 常见问题

**Q: 如何在对话结束后触发游戏事件？**
A: 使用 `TriggerEvent` 节点，在运行时通过 `UnityEvent` 或自定义事件系统触发。

**Q: 如何实现打字机效果？**
A: 在 `DialogueUI.ShowDialogue()` 中使用协程逐字显示文本。

**Q: 对话变量在场景切换后会丢失吗？**
A: 默认会丢失。如需持久化，将 `DialogueVariables` 存储在 `DontDestroyOnLoad` 的对象上，或使用 `PlayerPrefs`。

---

**下一步**: 继续学习 [教程9: 渲染图基础](09_RenderGraphBasics.md)
turn runtimeNode;
    }
}
```

### 4. 变量管理系统

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace GraphToolkitTutorials.DialogueSystem.Runtime
{
    /// <summary>
    /// 对话变量管理系统 - 存储对话状态和游戏变量
    /// </summary>
    public class DialogueVariables
    {
        private Dictionary<string, object> m_Variables = new Dictionary<string, object>();

        /// <summary>
        /// 设置变量
        /// </summary>
        public void Set(string key, object value)
        {
            m_Variables[key] = value;
        }

        /// <summary>
        /// 获取变量
        /// </summary>
        public T Get<T>(string key, T defaultValue = default)
        {
            if (m_Variables.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                    return typedValue;
                // 尝试类型转换
                try { return (T)System.Convert.ChangeType(value, typeof(T)); }
                catch { return defaultValue; }
            }
            return defaultValue;
        }

        /// <summary>
        /// 检查变量是否存在
        /// </summary>
        public bool Has(string key) => m_Variables.ContainsKey(key);

        /// <summary>
        /// 检查条件（支持简单的比较表达式）
        /// </summary>
        public bool CheckCondition(string key, string expectedValue)
        {
            if (!Has(key)) return false;
            var actualValue = Get<string>(key, "");
            return actualValue == expectedValue;
        }

        /// <summary>
        /// 保存到PlayerPrefs（持久化）
        /// </summary>
        public void Save(string saveKey)
        {
            var json = JsonUtility.ToJson(new SerializableDict(m_Variables));
            PlayerPrefs.SetString(saveKey, json);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 从PlayerPrefs加载
        /// </summary>
        public void Load(string saveKey)
        {
            if (PlayerPrefs.HasKey(saveKey))
            {
                var json = PlayerPrefs.GetString(saveKey);
                var dict = JsonUtility.FromJson<SerializableDict>(json);
                m_Variables = dict.ToDictionary();
            }
        }

        [System.Serializable]
        private class SerializableDict
        {
            public List<string> keys = new List<string>();
            public List<string> values = new List<string>();

            public SerializableDict(Dictionary<string, object> dict)
            {
                foreach (var kvp in dict)
                {
                    keys.Add(kvp.Key);
                    values.Add(kvp.Value?.ToString() ?? "");
                }
            }

            public Dictionary<string, object> ToDictionary()
            {
                var dict = new Dictionary<string, object>();
                for (int i = 0; i < keys.Count; i++)
                    dict[keys[i]] = values[i];
                return dict;
            }
        }
    }
}
```

### 5. 对话UI控制器

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GraphToolkitTutorials.DialogueSystem.Runtime
{
    /// <summary>
    /// 对话UI控制器 - 管理对话界面的显示
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject m_DialoguePanel;
        [SerializeField] private TextMeshProUGUI m_SpeakerNameText;
        [SerializeField] private TextMeshProUGUI m_DialogueText;
        [SerializeField] private Image m_SpeakerPortrait;
        [SerializeField] private GameObject m_ChoicePanel;
        [SerializeField] private Button m_ChoiceButtonPrefab;

        private System.Action<int> m_OnChoiceSelected;

        /// <summary>
        /// 显示对话文本
        /// </summary>
        public void ShowDialogue(string speakerName, string text, Sprite portrait)
        {
            m_DialoguePanel.SetActive(true);
            m_ChoicePanel.SetActive(false);

            m_SpeakerNameText.text = speakerName;
            m_DialogueText.text = text;
            
            if (portrait != null)
            {
                m_SpeakerPortrait.sprite = portrait;
                m_SpeakerPortrait.gameObject.SetActive(true);
            }
            else
            {
                m_SpeakerPortrait.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 显示选择项
        /// </summary>
        public void ShowChoices(string[] choices, System.Action<int> onChoiceSelected)
        {
            m_DialoguePanel.SetActive(false);
            m_ChoicePanel.SetActive(true);
            m_OnChoiceSelected = onChoiceSelected;

            // 清除旧按钮
            foreach (Transform child in m_ChoicePanel.transform)
                Destroy(child.gameObject);

            // 创建新按钮
            for (int i = 0; i < choices.Length; i++)
            {
                int index = i; // 闭包捕获
                var button = Instantiate(m_ChoiceButtonPrefab, m_ChoicePanel.transform);
                button.GetComponentInChildren<TextMeshProUGUI>().text = choices[i];
                button.onClick.AddListener(() => OnChoiceClicked(index));
            }
        }

        /// <summary>
        /// 隐藏对话UI
        /// </summary>
        public void Hide()
        {
            m_DialoguePanel.SetActive(false);
            m_ChoicePanel.SetActive(false);
        }

        private void OnChoiceClicked(int index)
        {
            m_OnChoiceSelected?.Invoke(index);
        }
    }
}
```

---

## 执行流程

```
DialogueRunner.Start()
    └─ 加载对话图形
    └─ 创建运行时图形
    └─ 开始执行协程

DialogueRunner.ExecuteDialogue()
    ├─ StartNode → 获取第一个节点
    ├─ DialogueTextNode → 显示对话，等待玩家点击
    ├─ ChoiceNode → 显示选项，等待玩家选择
    ├─ BranchNode → 检查条件，选择分支
    └─ EndNode → 对话结束
```

---

## 示例：任务对话

```
[Start]
    ↓
[Dialogue: "你好，冒险者！"]
    ↓
[Choice: "你需要帮助吗？"]
    ├─ "是的" → [SetVariable: quest_accepted = true]
    │              ↓
    │           [Dialogue: "太好了！去森林找到魔法石。"]
    │              ↓
    │           [End]
    │
    └─ "不需要" → [Dialogue: "好吧，有需要再来找我。"]
                     ↓
                  [End]
```

**使用步骤**:
1. 创建 `.dialogue` 资产文件
2. 在图形编辑器中设计对话流程
3. 添加 `DialogueRunner` 和 `DialogueUI` 组件
4. 配置UI引用
5. 调用 `DialogueRunner.StartDialogue()` 开始对话

---

## 高级功能

### 1. 条件分支

```csharp
// 根据玩家等级显示不同对话
[Branch: player_level >= 10]
    ├─ True → [Dialogue: "你已经很强了！"]
    └─ False → [Dialogue: "继续努力吧！"]
```

### 2. 变量操作

```csharp
// 设置任务状态
[SetVariable: quest_status = "completed"]

// 增加好感度
[SetVariable: npc_affection += 10]
```

### 3. 事件触发

```csharp
// 触发游戏事件
[TriggerEvent: "OnQuestComplete"]
    ↓
// 在代码中监听
DialogueRunner.OnEventTriggered += (eventName) => {
    if (eventName == "OnQuestComplete")
        GiveReward();
};
```

---

## 练习题

1. **基础**: 创建一个简单的NPC对话，包含3句台词
2. **进阶**: 添加选择分支，根据玩家选择显示不同结果
3. **挑战**: 实现对话历史记录功能，让玩家可以回顾之前的对话

---

## 常见问题

**Q: 如何实现打字机效果？**
A: 在 `DialogueUI.ShowDialogue()` 中使用协程逐字显示文本。

**Q: 如何保存对话进度？**
A: 使用 `DialogueVariables.Save()` 保存变量状态，下次加载时恢复。

**Q: 如何实现对话跳过功能？**
A: 在 `DialogueRunner` 中添加跳过标志，检测到时直接跳转到下一个节点。

**Q: 如何支持多语言？**
A: 将对话文本存储在本地化表中，运行时根据语言设置加载对应文本。

---

## 性能优化

- 对话文本使用 `TextMeshPro` 而非 `Text` 组件
- 选择按钮使用对象池避免频繁创建销毁
- 大型对话图分割成多个小图，按需加载
- 头像图片使用 Sprite Atlas 减少 DrawCall

---

**下一步**: 继续学习 [教程9: 渲染图基础](09_RenderGraphBasics.md)
→ [Dialogue: "太好了！请帮我找到丢失的宝石。"]
    │          → [End]
    └─ "不需要" → [Dialogue: "好的，祝你好运！"]
                 → [End]
```

**使用步骤**:
1. 创建 `.dialogue` 资产文件
2. 在图形编辑器中设计对话流程
3. 创建UI Canvas（使用提供的预制件）
4. 添加 `DialogueRunner` 组件到场景
5. 配置UI引用
6. 通过代码触发对话：`dialogueRunner.StartDialogue()`

---

## 高级特性

### 1. 变量替换

在对话文本中使用 `{变量名}` 来动态替换内容：

```csharp
// 对话文本: "你好，{playerName}！你已经完成了 {questCount} 个任务。"
// 运行时替换为: "你好，张三！你已经完成了 5 个任务。"

public string ReplaceVariables(string text, DialogueVariables variables)
{
    var regex = new System.Text.RegularExpressions.Regex(@"\{(\w+)\}");
    return regex.Replace(text, match =>
    {
        var key = match.Groups[1].Value;
        return variables.Get<string>(key, match.Value);
    });
}
```

### 2. 条件表达式

支持更复杂的条件判断：

```csharp
// 条件格式: "变量名 操作符 值"
// 例如: "playerLevel >= 10"
//       "hasKey == true"
//       "gold > 100"

public bool EvaluateCondition(string condition, DialogueVariables variables)
{
    var parts = condition.Split(' ');
    if (parts.Length != 3) return false;

    var key = parts[0];
    var op = parts[1];
    var expectedValue = parts[2];

    var actualValue = variables.Get<string>(key, "");

    switch (op)
    {
        case "==": return actualValue == expectedValue;
        case "!=": return actualValue != expectedValue;
        case ">": return float.Parse(actualValue) > float.Parse(expectedValue);
        case "<": return float.Parse(actualValue) < float.Parse(expectedValue);
        case ">=": return float.Parse(actualValue) >= float.Parse(expectedValue);
        case "<=": return float.Parse(actualValue) <= float.Parse(expectedValue);
        default: return false;
    }
}
```

### 3. 事件系统

触发游戏事件（如播放音效、改变场景等）：

```csharp
/// <summary>
/// 事件节点 - 触发游戏事件
/// </summary>
[Node("Trigger Event", "Dialogue")]
internal class TriggerEventNode : DialogueNode
{
    [SerializeField] private string m_EventName = "OnQuestComplete";

    public override Runtime.DialogueRuntimeNode CreateRuntimeNode(DialogueGraph graph)
    {
        return new Runtime.TriggerEventNode
        {
            eventName = m_EventName,
            nextNodeIndex = GetNextNodeIndex(graph)
        };
    }
}

// 在DialogueRunner中处理事件
public event System.Action<string> OnEventTriggered;

private void ExecuteTriggerEvent(Runtime.TriggerEventNode node)
{
    OnEventTriggered?.Invoke(node.eventName);
}
```

---

## 性能优化

### 1. 对象池

对于频繁创建的选择按钮，使用对象池：

```csharp
private Queue<Button> m_ButtonPool = new Queue<Button>();

private Button GetButton()
{
    if (m_ButtonPool.Count > 0)
        return m_ButtonPool.Dequeue();
    return Instantiate(m_ChoiceButtonPrefab);
}

private void ReturnButton(Button button)
{
    button.gameObject.SetActive(false);
    m_ButtonPool.Enqueue(button);
}
```

### 2. 文本缓存

缓存处理后的对话文本：

```csharp
private Dictionary<int, string> m_ProcessedTextCache = new Dictionary<int, string>();

public string GetProcessedText(int nodeIndex, string rawText, DialogueVariables variables)
{
    if (!m_ProcessedTextCache.ContainsKey(nodeIndex))
    {
        m_ProcessedTextCache[nodeIndex] = ReplaceVariables(rawText, variables);
    }
    return m_ProcessedTextCache[nodeIndex];
}
```

---

## 调试技巧

### 1. 对话日志

记录对话执行路径：

```csharp
private List<string> m_DialogueLog = new List<string>();

private void LogDialogue(string nodeType, string content)
{
    var log = $"[{Time.time:F2}] {nodeType}: {content}";
    m_DialogueLog.Add(log);
    Debug.Log(log);
}
```

### 2. 变量监视器

在Inspector中显示当前变量状态：

```csharp
#if UNITY_EDITOR
[CustomEditor(typeof(DialogueRunner))]
public class DialogueRunnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var runner = (DialogueRunner)target;
        if (runner.Variables != null && Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Current Variables", EditorStyles.boldLabel);
            // 显示所有变量...
        }
    }
}
#endif
```

---

## 练习题

1. **基础**: 创建一个简单的NPC对话，包含3句台词
2. **进阶**: 添加一个选择节点，根据玩家选择显示不同的后续对话
3. **挑战**: 实现一个任务系统，使用变量记录任务状态，并在对话中根据任务状态显示不同内容

---

## 常见问题

**Q: 如何实现打字机效果？**
A: 在DialogueUI中使用协程逐字显示文本：
```csharp
private IEnumerator TypeText(string text, float delay = 0.05f)
{
    m_DialogueText.text = "";
    foreach (char c in text)
    {
        m_DialogueText.text += c;
        yield return new WaitForSeconds(delay);
    }
}
```

**Q: 如何保存对话进度？**
A: 使用DialogueVariables的Save/Load方法，并记录当前节点索引：
```csharp
PlayerPrefs.SetInt("CurrentDialogueNode", currentNodeIndex);
variables.Save("DialogueVariables");
```

**Q: 如何实现对话历史记录？**
A: 维护一个对话历史列表，每次显示对话时添加记录：
```csharp
private List<DialogueEntry> m_History = new List<DialogueEntry>();

public void AddToHistory(string speaker, string text)
{
    m_History.Add(new DialogueEntry { speaker = speaker, text = text });
}
```

---

## 扩展方向

1. **本地化支持**: 使用本地化表替换对话文本
2. **语音集成**: 为每句对话添加语音文件
3. **表情系统**: 根据对话内容改变角色表情
4. **对话动画**: 添加角色动画和镜头效果
5. **自动保存**: 在关键节点自动保存进度

---

**下一步**: 继续学习 [教程9: 渲染图基础](09_RenderGraphBasics.md)
