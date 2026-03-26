# 教程8: 对话系统

## 概述

本教程实现一个完整的游戏对话系统，支持线性对话、玩家选择分支、条件判断、变量管理和事件触发。系统采用执行流（Push 模式）架构，与教程3、6、7保持一致的 Editor/Runtime 分离设计。

### 学习目标

- 综合运用执行流图形的核心技术
- 理解多分支对话图的编辑器到运行时转换
- 掌握 `UnityEvent` 解耦 Runner 与 UI 的模式
- 学习运行时变量系统（字符串键值对）的设计
- 理解 `graph.FindNodeForPort(port)` 的必要性（`IPort` 无 `.Node` 属性）

### 前置知识

- 完成教程3（执行流图形，必须）
- 了解 Unity 协程
- 熟悉 `UnityEvent`

---

## 系统架构

### 节点类型

| 编辑器节点 | 运行时节点 | 功能 |
|-----------|-----------|------|
| `StartDialogueNode` | `StartNode` | 对话起点，仅一个 |
| `DialogueTextNode` | `DialogueTextNode` | 显示说话人、文本、头像 |
| `ChoiceNode` | `ChoiceNode` | 向玩家显示固定两个选项 |
| `BranchNode` | `BranchNode` | 条件分支（读取变量） |
| `SetVariableNode` | `SetVariableNode` | 写入变量后继续 |
| `EventNode` | `EventNode` | 触发具名事件后继续 |
| `EndDialogueNode` | `EndNode` | 对话结束 |

### 执行模式

**Push 模式**，`DialogueRunner` 协程驱动：
- 遇到 `DialogueTextNode` / `ChoiceNode`：设 `m_WaitingForInput = true`，`yield return null` 轮询等待外部调用 `ContinueDialogue()` / `SelectChoice(int)`
- 遇到 `BranchNode` / `SetVariableNode` / `EventNode`：同步执行，立即跳转下一节点
- 遇到 `EndNode` 或索引为 -1：退出循环，触发 `OnDialogueEnd`

---

## 项目结构

```
Assets/Tutorials/08_DialogueSystem/
├─ Editor/
│  ├─ DialogueGraph.cs                  # [Graph("dialogue")] 图形类，含 FindNodeForPort
│  ├─ DialogueImporter.cs               # [ScriptedImporter(1,"dialogue")] → DialogueRuntimeGraph
│  ├─ Nodes/
│  │  ├─ DialogueNode.cs               # 抽象基类，AddInputPort、GetNodeIndex
│  │  ├─ BasicNodes.cs                 # Start, DialogueText, End
│  │  ├─ BranchNodes.cs               # Choice (两固定选项), Branch (变量条件)
│  │  └─ ActionNodes.cs               # SetVariable, Event
│  └─ Unity.GraphToolkit.Tutorials.DialogueSystem.Editor.asmdef
├─ Runtime/
│  ├─ DialogueRuntimeGraph.cs          # ScriptableObject，节点列表，GetStartNode
│  ├─ DialogueRunner.cs               # MonoBehaviour，UnityEvent API，协程执行
│  ├─ DialogueVariables.cs            # 字符串键值对存储
│  ├─ DialogueTester.cs               # 测试脚本，OnGUI + Input System 输入
│  ├─ Nodes/
│  │  └─ RuntimeNodes.cs             # 全部运行时节点数据类
│  ├─ UI/
│  │  └─ DialogueUI.cs               # UI 控制器（TextMeshPro）
│  └─ Unity.GraphToolkit.Tutorials.DialogueSystem.Runtime.asmdef
└─ Examples/
   ├─ Graphs/
   │  └─ TavernDialogue.dialogue       # 13节点示例对话（酒馆场景）
   └─ Scenes/
      └─ DialogueTestScene.unity       # 测试场景（含 DialogueTester）
```

---

## 核心实现

### 1. 对话图形

```csharp
[Graph("dialogue", GraphOptions.Default)]
[Serializable]
internal class DialogueGraph : Graph
{
    [MenuItem("Assets/Create/Graph Toolkit/Dialogue Graph")]
    static void CreateGraphAssetFile()
        => GraphDatabase.PromptInProjectBrowserToCreateNewAsset<DialogueGraph>();

    /// <summary>
    /// 根据端口反查所属节点。
    /// 注意：GraphToolkit 的 IPort 没有 .Node 属性，必须遍历查找。
    /// </summary>
    public INode FindNodeForPort(IPort port)
    {
        foreach (var node in GetNodes())
        {
            foreach (var p in node.GetInputPorts())
                if (p == port) return node;
            foreach (var p in node.GetOutputPorts())
                if (p == port) return node;
        }
        return null;
    }

    public IPort GetConnectedInputPort(IPort outputPort)
        => outputPort?.FirstConnectedPort;

    public StartDialogueNode FindStartNode()
    {
        foreach (var node in GetNodes())
            if (node is StartDialogueNode s) return s;
        return null;
    }
}
```

> **关键点**：`IPort` 没有 `.Node` 属性。所有"根据端口找节点"的操作都必须通过 `graph.FindNodeForPort(port)` 完成，这是与数据流图最常见的陷阱之一。

### 2. 节点基类

```csharp
[Serializable]
internal abstract class DialogueNode : Node
{
    protected IPort m_InputPort;

    protected void AddInputPort(IPortDefinitionContext context)
    {
        m_InputPort = context.AddInputPort("In")
            .WithConnectorUI(PortConnectorUI.Arrowhead)
            .Build();
    }

    public abstract Runtime.DialogueRuntimeNode CreateRuntimeNode(DialogueGraph graph);

    public int GetNodeIndex(DialogueGraph graph)
    {
        var allNodes = new List<INode>(graph.GetNodes());
        for (int i = 0; i < allNodes.Count; i++)
            if (allNodes[i] == this) return i;
        return -1;
    }
}
```

### 3. 对话文本节点（编辑器）

```csharp
[Node("Dialogue", "")]
[UseWithGraph(typeof(DialogueGraph))]
[Serializable]
internal class DialogueTextNode : DialogueNode
{
    private INodeOption m_SpeakerNameOption;
    private INodeOption m_DialogueTextOption;
    private INodeOption m_PortraitOption;
    private IPort m_OutputPort;

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        AddInputPort(context);
        m_OutputPort = context.AddOutputPort("Out")
            .WithConnectorUI(PortConnectorUI.Arrowhead).Build();
    }

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        m_SpeakerNameOption  = context.AddOption<string>("Speaker Name").Delayed().Build();
        m_DialogueTextOption = context.AddOption<string>("Dialogue Text").AsTextArea().Delayed().Build();
        m_PortraitOption     = context.AddOption<Sprite>("Portrait").Build();
    }

    public override Runtime.DialogueRuntimeNode CreateRuntimeNode(DialogueGraph graph)
    {
        string speakerName = "Character";
        m_SpeakerNameOption?.TryGetValue(out speakerName);
        string dialogueText = "";
        m_DialogueTextOption?.TryGetValue(out dialogueText);
        Sprite portrait = null;
        m_PortraitOption?.TryGetValue(out portrait);

        var nextNode = GetNextNode(graph);
        return new Runtime.DialogueTextNode
        {
            speakerName     = speakerName  ?? "Character",
            dialogueText    = dialogueText ?? "",
            speakerPortrait = portrait,
            nextNodeIndex   = nextNode != null ? nextNode.GetNodeIndex(graph) : -1
        };
    }

    private DialogueNode GetNextNode(DialogueGraph graph)
    {
        var connected = graph.GetConnectedInputPort(m_OutputPort);
        // 必须用 FindNodeForPort，不能用 connected.Node（API 不存在）
        return connected != null && graph.FindNodeForPort(connected) is DialogueNode dn ? dn : null;
    }
}
```

### 4. 选择节点（固定两选项）

```csharp
[Node("Branch", "")]
[UseWithGraph(typeof(DialogueGraph))]
[Serializable]
internal class ChoiceNode : DialogueNode
{
    private INodeOption m_Option1TextOption;
    private INodeOption m_Option2TextOption;
    private IPort m_OutputPort1;
    private IPort m_OutputPort2;

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        AddInputPort(context);
        m_OutputPort1 = context.AddOutputPort("Option 1").WithConnectorUI(PortConnectorUI.Arrowhead).Build();
        m_OutputPort2 = context.AddOutputPort("Option 2").WithConnectorUI(PortConnectorUI.Arrowhead).Build();
    }

    public override Runtime.DialogueRuntimeNode CreateRuntimeNode(DialogueGraph graph)
    {
        string opt1 = "Option 1"; m_Option1TextOption?.TryGetValue(out opt1);
        string opt2 = "Option 2"; m_Option2TextOption?.TryGetValue(out opt2);

        int next1 = -1, next2 = -1;
        var conn1 = graph.GetConnectedInputPort(m_OutputPort1);
        if (conn1 != null && graph.FindNodeForPort(conn1) is DialogueNode n1)
            next1 = n1.GetNodeIndex(graph);
        var conn2 = graph.GetConnectedInputPort(m_OutputPort2);
        if (conn2 != null && graph.FindNodeForPort(conn2) is DialogueNode n2)
            next2 = n2.GetNodeIndex(graph);

        return new Runtime.ChoiceNode
        {
            optionTexts     = new[] { opt1 ?? "Option 1", opt2 ?? "Option 2" },
            nextNodeIndices = new[] { next1, next2 }
        };
    }
}
```

### 5. 导入器

```csharp
[ScriptedImporter(1, "dialogue")]
internal class DialogueImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var graph = GraphDatabase.LoadGraphForImporter<DialogueGraph>(ctx.assetPath);

        var runtimeGraph = ScriptableObject.CreateInstance<Runtime.DialogueRuntimeGraph>();
        runtimeGraph.name = Path.GetFileNameWithoutExtension(ctx.assetPath);

        if (graph != null)
        {
            if (!graph.Validate(out string errorMessage))
                Debug.LogWarning($"[DialogueSystem] {ctx.assetPath}: {errorMessage}");

            var allNodes = new List<INode>(graph.GetNodes());
            for (int i = 0; i < allNodes.Count; i++)
            {
                if (allNodes[i] is DialogueNode dn)
                {
                    runtimeGraph.nodes.Add(dn.CreateRuntimeNode(graph));
                    if (allNodes[i] is StartDialogueNode)
                        runtimeGraph.startNodeIndex = runtimeGraph.nodes.Count - 1;
                }
            }
        }

        ctx.AddObjectToAsset("main", runtimeGraph);
        ctx.SetMainObject(runtimeGraph);
    }
}
```

### 6. 运行时节点定义

```csharp
// 基类
[Serializable]
public abstract class DialogueRuntimeNode { public string nodeType; }

// 数据节点（全部为纯数据，无 Editor 依赖）
[Serializable] public class StartNode       : DialogueRuntimeNode { public int nextNodeIndex = -1; }
[Serializable] public class DialogueTextNode: DialogueRuntimeNode { public string speakerName; public string dialogueText; public Sprite speakerPortrait; public int nextNodeIndex = -1; }
[Serializable] public class ChoiceNode      : DialogueRuntimeNode { public string[] optionTexts; public int[] nextNodeIndices; }
[Serializable] public class BranchNode      : DialogueRuntimeNode { public string conditionKey; public string expectedValue; public int trueNodeIndex = -1; public int falseNodeIndex = -1; }
[Serializable] public class SetVariableNode : DialogueRuntimeNode { public string variableKey; public string variableValue; public int nextNodeIndex = -1; }
[Serializable] public class EventNode       : DialogueRuntimeNode { public string eventName; public string eventParameter; public int nextNodeIndex = -1; }
[Serializable] public class EndNode         : DialogueRuntimeNode { }
```

### 7. DialogueRunner（协程执行 + UnityEvent 解耦）

```csharp
public class DialogueRunner : MonoBehaviour
{
    // UnityEvent 解耦 Runner 与 UI / 业务逻辑
    public UnityEvent<string, string, Sprite> OnDialogueText;
    public UnityEvent<string[]>               OnChoice;
    public UnityEvent<string, string>         OnEvent;
    public UnityEvent                         OnDialogueStart;
    public UnityEvent                         OnDialogueEnd;

    private bool m_WaitingForInput  = false;
    private int  m_SelectedChoiceIndex = -1;

    public void ContinueDialogue() => m_WaitingForInput = false;

    public void SelectChoice(int index)
    {
        m_SelectedChoiceIndex = index;
        m_WaitingForInput = false;
    }

    private IEnumerator ExecuteDialogue()
    {
        OnDialogueStart?.Invoke();
        int currentIndex = m_DialogueGraph.GetStartNode().nextNodeIndex;

        while (currentIndex >= 0)
        {
            var node = m_DialogueGraph.GetNode(currentIndex);

            if (node is DialogueTextNode t)
            {
                OnDialogueText?.Invoke(t.speakerName, t.dialogueText, t.speakerPortrait);
                m_WaitingForInput = true;
                while (m_WaitingForInput) yield return null;
                currentIndex = t.nextNodeIndex;
            }
            else if (node is ChoiceNode c)
            {
                OnChoice?.Invoke(c.optionTexts);
                m_WaitingForInput = true;
                while (m_WaitingForInput) yield return null;
                currentIndex = c.nextNodeIndices[m_SelectedChoiceIndex];
            }
            else if (node is BranchNode b)
            {
                string val = m_Variables.GetVariable(b.conditionKey, "");
                currentIndex = val == b.expectedValue ? b.trueNodeIndex : b.falseNodeIndex;
            }
            else if (node is SetVariableNode sv)
            {
                m_Variables.SetVariable(sv.variableKey, sv.variableValue);
                currentIndex = sv.nextNodeIndex;
            }
            else if (node is EventNode ev)
            {
                OnEvent?.Invoke(ev.eventName, ev.eventParameter);
                currentIndex = ev.nextNodeIndex;
            }
            else if (node is EndNode)
                currentIndex = -1;
        }

        OnDialogueEnd?.Invoke();
    }
}
```

### 8. DialogueTester（测试脚本）

无需搭建 UI，直接在 `OnGUI` 显示对话内容。挂载在与 `DialogueRunner` 相同的 GameObject 上：

```
空间/回车 — 开始对话 / 继续文本节点
1 / 2     — 选择选项
```

监听 `DialogueRunner` 的四个 `UnityEvent`，更新本地状态字段后 `OnGUI` 重绘。头像使用 `GUI.DrawTextureWithTexCoords` 渲染（正确支持图集精灵）。

> **注意**：`DialogueTester` 使用 **Input System package**（`UnityEngine.InputSystem.Keyboard`），Runtime asmdef 需引用 `Unity.InputSystem`。

---

## 示例：TavernDialogue.dialogue

13 个节点的酒馆对话示例：

```
[Start]
  └─ [Text: "酒馆掌柜: 你好，旅行者！"]
       └─ [Text: "需要喝一杯吗？"]
            └─ [Choice]
                 ├─ "来一杯！" → [SetVariable: ordered=true]
                 │                └─ [Text: "掌柜: 好的，最好的麦酒！"]
                 │                     └─ [EventNode: ServeAle]
                 │                          └─ [End]
                 └─ "不了"   → [Branch: ordered == true]
                                  ├─ True  → [Text: "掌柜: 哦，你刚才还要了一杯呢。"]
                                  │           └─ [End]
                                  └─ False → [Text: "掌柜: 好吧，随时欢迎。"]
                                              └─ [End]
```

---

## 设计要点总结

### FindNodeForPort 模式

GraphToolkit 的 `IPort` **没有** `.Node` 属性。所有连线遍历都必须：

```csharp
// 错误（编译失败）
if (connectedPort.Node is DialogueNode n) ...

// 正确
if (graph.FindNodeForPort(connectedPort) is DialogueNode n) ...
```

在 `DialogueGraph` 中添加 `FindNodeForPort` 辅助方法，避免每个节点重复遍历逻辑。

### Fan-in（多对一）连接

多个输出端口可以连接到同一个输入端口（如多个路径汇聚到 `EndNode`），这是执行流图的正常用法。导入器按输出端口逐一查询连接，不受影响。

### UnityEvent 解耦模式

`DialogueRunner` 不直接引用任何 UI 类。`OnDialogueText`、`OnChoice`、`OnEvent` 均为 `UnityEvent`，UI、测试脚本、游戏逻辑各自订阅所需事件，实现完全解耦。

### Input System 兼容

项目启用 **Input System package** 后，`UnityEngine.Input` 在运行时抛出异常。必须：
1. Runtime asmdef 的 `references` 中添加 `"Unity.InputSystem"`
2. 代码改用 `Keyboard.current.spaceKey.wasPressedThisFrame` 等 API

---

## 使用步骤

1. 在 Project 窗口右键 → `Create/Graph Toolkit/Dialogue Graph` 创建 `.dialogue` 资产
2. 在图形编辑器中添加节点、连线设计对话流程
3. 保存后 Importer 自动生成 `DialogueRuntimeGraph` 资产
4. 场景中创建空物体，挂载 `DialogueRunner` + `DialogueTester`
5. `DialogueTester` 在 Play 模式下自动查找 `DialogueRuntimeGraph` 并赋值
6. Play → 按空格开始对话，1/2 选择选项

---

## 常见问题

**Q: `CS1061: 'IPort' does not contain a definition for 'Node'`**
A: `IPort` 接口无 `.Node` 属性，使用 `graph.FindNodeForPort(port)` 替代。

**Q: `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class`**
A: 项目使用 Input System package，需在 runtime asmdef 添加 `Unity.InputSystem` 引用，并改用 `Keyboard.current` API。

**Q: 头像没有显示**
A: `DialogueTester.OnDialogueText` 接收 `Sprite portrait` 参数，需保存到字段并在 `OnGUI` 中用 `GUI.DrawTextureWithTexCoords` 渲染。

**Q: 如何扩展为三选项？**
A: 修改 `ChoiceNode` 增加 `m_OutputPort3` 和对应 Option 3 Text 选项，运行时 `optionTexts` / `nextNodeIndices` 数组扩展为长度 3。

---

## 练习题

1. **基础**: 创建一个3句线性对话，设置说话人名和头像
2. **进阶**: 添加选择节点，根据玩家选择 `SetVariable`，再用 `BranchNode` 验证分支
3. **挑战**: 扩展 `ChoiceNode` 支持动态数量选项（参考教程7的子节点列表模式）

---

**上一步**: [教程7: 行为树系统](07_BehaviorTree.md)
**下一步**: [教程9: 渲染图基础](09_RenderGraphBasics.md)
