# 教程6: 技能系统 — 事件驱动 + 并行执行

## 概述

本教程通过 **AbilityGraph**（`.ability`）演示 GraphToolkit 中完整的**执行流图（Push 模式）**实践：
- **事件驱动入口**：`OnEventNode` 监听具名事件，图可同时定义多条事件链
- **并行执行分支**：`ParallelNode` 同时激活两条分支，两者均完成后才继续
- **Editor / Runtime 分离**：编辑器节点序列化为运行时节点，MonoBehaviour 用协程驱动执行

这是前三个教程的综合深化：既涉及执行流连接（教程3），也要求完整的 Editor/Runtime 分离（教程7/8 的预演）。

---

## 学习目标

- 理解 **执行流图（Push 模式）** 与数据流图（Pull 模式）的核心区别
- 使用 `[Node]`、`[UseWithGraph]` 创建执行流节点，用 `PortConnectorUI.Arrowhead` 标识执行端口
- 掌握 `IAbilityEditorNode.CreateRuntimeNode` 模式将编辑器节点转换为运行时数据
- 使用 `[SerializeReference]` 实现多态运行时节点列表的序列化
- 用 `FindNextIndex` 工具方法把连线关系转换为整数索引
- 在 MonoBehaviour 中用 **协程（Coroutine）** 驱动顺序执行和并行等待
- 理解 `AddOption<T>.Delayed()` 对字符串选项的必要性

---

## 核心概念：Push 模式 vs Pull 模式

| 维度 | Pull 模式（数据流） | Push 模式（执行流） |
|------|------------------|------------------|
| 典型场景 | 计算结果，按需求值 | 播放动画、等待时间、触发效果 |
| 求值时机 | 导入时（ScriptedImporter）| 运行时（MonoBehaviour + Coroutine）|
| 节点关系 | 输出端口连输入端口，值向前传递 | 执行端口按箭头顺序依次激活节点 |
| 运行时数据 | 不需要（在 Editor 中已完成）| 需要独立的 Runtime 层 |
| 连接含义 | "这个节点的值来自那个节点" | "那个节点执行完后激活这个节点" |

---

## 项目结构

```
06_CustomUI/
├─ Editor/
│  ├── AbilityGraph.cs          # [Graph("ability")] + FindNextIndex 工具
│  ├── AbilityImporter.cs       # ScriptedImporter，两遍扫描生成运行时图
│  └── Nodes/
│      ├── IAbilityEditorNode.cs  # Editor 接口：CreateRuntimeNode(...)
│      ├── MultiPortNode.cs       # OnEventNode — 事件触发入口
│      ├── OptionsNode.cs         # ParallelNode — 并行执行节点
│      └── PreviewNode.cs         # WaitNode + LogActionNode — 动作节点
├─ Runtime/
│  ├── AbilityRuntimeNode.cs    # 运行时节点抽象基类（[Serializable]）
│  ├── AbilityRuntimeGraph.cs   # ScriptableObject 主资产（[SerializeReference] 多态列表）
│  ├── AbilityRunner.cs         # MonoBehaviour：FireEvent + 协程执行引擎
│  └── Nodes/
│      └── RuntimeNodes.cs      # 具体运行时节点数据类
└─ Editor/
   ├── Unity.GraphToolkit.Tutorials.AbilitySystem.Editor.asmdef
└─ Runtime/
   └── Unity.GraphToolkit.Tutorials.AbilitySystem.Runtime.asmdef
```

---

## 编辑器层

### AbilityGraph

图定义本身很简单——主要工作在于 `FindNextIndex` 工具方法：

```csharp
[Graph("ability", GraphOptions.Default)]
[Serializable]
internal class AbilityGraph : Graph
{
    [MenuItem("Assets/Create/Graph Toolkit/Ability Graph", false)]
    static void CreateGraphAssetFile()
        => GraphDatabase.PromptInProjectBrowserToCreateNewAsset<AbilityGraph>();

    /// <summary>
    /// 给定一个执行 OUTPUT 端口，找到它所连接节点在 allNodes 中的索引。
    /// 未连接或目标节点不是 IAbilityEditorNode 时返回 -1。
    /// </summary>
    internal static int FindNextIndex(
        IPort outputPort,
        List<INode> allNodes,
        Dictionary<INode, int> indexMap)
    {
        // outputPort.FirstConnectedPort → 对方节点的 INPUT 端口
        var connectedInput = outputPort?.FirstConnectedPort;
        if (connectedInput == null) return -1;

        // 遍历所有节点，找到拥有该 INPUT 端口的节点
        foreach (var node in allNodes)
        {
            foreach (var inputPort in node.GetInputPorts())
            {
                if (inputPort == connectedInput)
                    return indexMap.TryGetValue(node, out int idx) ? idx : -1;
            }
        }
        return -1;
    }
}
```

> **为什么需要 `FindNextIndex`？**
> 运行时节点不存储端口对象，只用 `int` 索引表示"下一个节点"。
> 此工具方法把编辑器图中的连线翻译成索引，是 Editor→Runtime 转换的核心。

---

### IAbilityEditorNode

所有编辑器节点实现此接口，AbilityImporter 通过它统一调用转换：

```csharp
internal interface IAbilityEditorNode
{
    AbilityRuntimeNode CreateRuntimeNode(
        List<INode> allNodes,
        Dictionary<INode, int> indexMap);
}
```

---

### AbilityImporter — 两遍扫描

```csharp
[ScriptedImporter(1, "ability")]
internal class AbilityImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var graph = GraphDatabase.LoadGraphForImporter<AbilityGraph>(ctx.assetPath);
        var runtimeGraph = ScriptableObject.CreateInstance<AbilityRuntimeGraph>();

        if (graph != null)
        {
            var allNodes = graph.GetNodes().ToList();

            // 第一遍：为每个 IAbilityEditorNode 分配索引
            var indexMap = new Dictionary<INode, int>();
            for (int i = 0; i < allNodes.Count; i++)
                if (allNodes[i] is IAbilityEditorNode)
                    indexMap[allNodes[i]] = i;

            // 第二遍：调用 CreateRuntimeNode，此时所有索引已就绪
            foreach (var node in allNodes)
                if (node is IAbilityEditorNode an)
                    runtimeGraph.nodes.Add(an.CreateRuntimeNode(allNodes, indexMap));
        }

        runtimeGraph.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
        ctx.AddObjectToAsset("main", runtimeGraph);
        ctx.SetMainObject(runtimeGraph);
    }
}
```

> **为什么要两遍扫描？**
> `CreateRuntimeNode` 需要通过 `FindNextIndex` 查找下游节点的索引，
> 若只做一遍扫描，某些后续节点可能尚未加入 `indexMap`，导致索引缺失。

---

### 节点详解

#### OnEventNode — 事件触发入口

```csharp
[Node("On Event", "Ability/Trigger")]
[UseWithGraph(typeof(AbilityGraph))]
[Serializable]
internal class OnEventNode : Node, IAbilityEditorNode
{
    private INodeOption m_EventNameOption;
    private IPort m_Next;   // 箭头输出端口

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        m_Next = context.AddOutputPort("Next")
            .WithConnectorUI(PortConnectorUI.Arrowhead).Build();
    }

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        // Delayed 避免每次按键都触发节点重建
        m_EventNameOption = context.AddOption<string>("Event Name").Delayed().Build();
    }

    public AbilityRuntimeNode CreateRuntimeNode(
        List<INode> allNodes, Dictionary<INode, int> indexMap)
    {
        string eventName = "Default";
        m_EventNameOption?.TryGetValue(out eventName);
        return new OnEventRuntimeNode
        {
            eventName = eventName ?? "Default",
            next      = AbilityGraph.FindNextIndex(m_Next, allNodes, indexMap)
        };
    }
}
```

#### ParallelNode — 并行执行

```csharp
[Node("Parallel", "Ability/Flow")]
[UseWithGraph(typeof(AbilityGraph))]
[Serializable]
internal class ParallelNode : Node, IAbilityEditorNode
{
    private IPort m_In;
    private IPort m_BranchA;
    private IPort m_BranchB;
    private IPort m_Done;   // 两条分支均完成后继续

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        m_In      = context.AddInputPort("In")
            .WithConnectorUI(PortConnectorUI.Arrowhead).Build();
        m_BranchA = context.AddOutputPort("Branch A")
            .WithConnectorUI(PortConnectorUI.Arrowhead).Build();
        m_BranchB = context.AddOutputPort("Branch B")
            .WithConnectorUI(PortConnectorUI.Arrowhead).Build();
        m_Done    = context.AddOutputPort("Done")
            .WithConnectorUI(PortConnectorUI.Arrowhead).Build();
    }

    public AbilityRuntimeNode CreateRuntimeNode(
        List<INode> allNodes, Dictionary<INode, int> indexMap)
        => new ParallelRuntimeNode
        {
            branchA = AbilityGraph.FindNextIndex(m_BranchA, allNodes, indexMap),
            branchB = AbilityGraph.FindNextIndex(m_BranchB, allNodes, indexMap),
            done    = AbilityGraph.FindNextIndex(m_Done,    allNodes, indexMap)
        };
}
```

#### WaitNode / LogActionNode

```csharp
[Node("Wait", "Ability/Action")]
[UseWithGraph(typeof(AbilityGraph))]
[Serializable]
internal class WaitNode : Node, IAbilityEditorNode
{
    private INodeOption m_DurationOption;
    private IPort m_In, m_Next;

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        m_In   = context.AddInputPort("In")
            .WithConnectorUI(PortConnectorUI.Arrowhead).Build();
        m_Next = context.AddOutputPort("Next")
            .WithConnectorUI(PortConnectorUI.Arrowhead).Build();
    }

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        m_DurationOption = context.AddOption<float>("Duration").Build();
    }

    public AbilityRuntimeNode CreateRuntimeNode(
        List<INode> allNodes, Dictionary<INode, int> indexMap)
    {
        float duration = 1f;
        m_DurationOption?.TryGetValue(out duration);
        return new WaitRuntimeNode
        {
            duration = duration,
            next     = AbilityGraph.FindNextIndex(m_Next, allNodes, indexMap)
        };
    }
}
```

```csharp
[Node("Log Action", "Ability/Action")]
[UseWithGraph(typeof(AbilityGraph))]
[Serializable]
internal class LogActionNode : Node, IAbilityEditorNode
{
    private INodeOption m_MessageOption;
    private IPort m_In, m_Next;

    // OnDefinePorts: In + Next (Arrowhead)
    // OnDefineOptions: m_MessageOption = context.AddOption<string>("Message").Delayed().Build()

    public AbilityRuntimeNode CreateRuntimeNode(
        List<INode> allNodes, Dictionary<INode, int> indexMap)
    {
        string message = "Action";
        m_MessageOption?.TryGetValue(out message);
        return new LogActionRuntimeNode
        {
            message = message ?? "Action",
            next    = AbilityGraph.FindNextIndex(m_Next, allNodes, indexMap)
        };
    }
}
```

---

## 运行时层

### 运行时节点设计原则

运行时节点是纯数据类，不依赖任何 Unity Editor API：

```csharp
// 抽象基类 — 必须 [Serializable]，配合 [SerializeReference] 实现多态序列化
[Serializable]
public abstract class AbilityRuntimeNode { }

[Serializable]
public class OnEventRuntimeNode : AbilityRuntimeNode
{
    public string eventName;
    public int    next;      // -1 = 链结束
}

[Serializable]
public class ParallelRuntimeNode : AbilityRuntimeNode
{
    public int branchA; // -1 = 未连接（视为立即完成）
    public int branchB;
    public int done;    // 两条分支完成后继续
}

[Serializable]
public class WaitRuntimeNode : AbilityRuntimeNode
{
    public float duration;
    public int   next;
}

[Serializable]
public class LogActionRuntimeNode : AbilityRuntimeNode
{
    public string message;
    public int    next;
}
```

> **`[SerializeReference]` 是必须的**：`AbilityRuntimeGraph.nodes` 存储抽象基类的多态列表。
> 普通 `[SerializeField]` 无法序列化多态引用，`[SerializeReference]` 专为此场景设计。

---

### AbilityRuntimeGraph

```csharp
public class AbilityRuntimeGraph : ScriptableObject
{
    [SerializeReference]
    public List<AbilityRuntimeNode> nodes = new List<AbilityRuntimeNode>();

    /// <summary>按事件名查找 OnEventRuntimeNode 的索引，未找到返回 -1。</summary>
    public int FindTrigger(string eventName)
    {
        for (int i = 0; i < nodes.Count; i++)
            if (nodes[i] is OnEventRuntimeNode trigger && trigger.eventName == eventName)
                return i;
        return -1;
    }
}
```

---

### AbilityRunner — 协程执行引擎

```csharp
public class AbilityRunner : MonoBehaviour
{
    public AbilityRuntimeGraph graph;

    public void FireEvent(string eventName)
    {
        int startIndex = graph.FindTrigger(eventName);
        if (startIndex < 0) return;

        var trigger = graph.nodes[startIndex] as OnEventRuntimeNode;
        StartCoroutine(ExecuteFrom(trigger.next));
    }

    private IEnumerator ExecuteFrom(int nodeIndex)
    {
        while (nodeIndex >= 0 && nodeIndex < graph.nodes.Count)
        {
            var node = graph.nodes[nodeIndex];
            switch (node)
            {
                case WaitRuntimeNode w:
                    yield return new WaitForSeconds(w.duration);
                    nodeIndex = w.next;
                    break;

                case LogActionRuntimeNode log:
                    Debug.Log($"[Ability] {log.message}");
                    nodeIndex = log.next;
                    break;

                case ParallelRuntimeNode p:
                    yield return StartCoroutine(ExecuteParallel(p));
                    nodeIndex = p.done;
                    break;

                default:
                    yield break;
            }
        }
    }

    private IEnumerator ExecuteParallel(ParallelRuntimeNode p)
    {
        bool aDone = p.branchA < 0;
        bool bDone = p.branchB < 0;

        if (!aDone) StartCoroutine(ExecuteAndSignal(p.branchA, () => aDone = true));
        if (!bDone) StartCoroutine(ExecuteAndSignal(p.branchB, () => bDone = true));

        yield return new WaitUntil(() => aDone && bDone);
    }

    private IEnumerator ExecuteAndSignal(int startIndex, System.Action onDone)
    {
        yield return StartCoroutine(ExecuteFrom(startIndex));
        onDone?.Invoke();
    }
}
```

> **并行等待模式**：两条子协程各自独立运行；主协程通过 `WaitUntil(() => aDone && bDone)` 轮询
> 两个布尔标志。这是 Unity 协程中实现"等待多个并发任务"的惯用写法。

---

## 使用示例

### 场景搭建

1. 在 Project 窗口右键 → **Create → Graph Toolkit → Ability Graph**，创建 `MyAbility.ability`
2. 打开图编辑器，构建如下结构：

```
[On Event "Attack"]
    └─ Next → [Parallel]
                 ├─ Branch A → [Log Action "播放动画"] → [Wait 1.5s]
                 ├─ Branch B → [Log Action "播放音效"] → [Wait 0.5s]
                 └─ Done    → [Log Action "技能结束"]
```

3. 保存图（Ctrl+S），触发 `AbilityImporter`，生成 `AbilityRuntimeGraph`
4. 在场景中创建空 GameObject，挂载 `AbilityRunner`
5. 将 `MyAbility.ability` 拖到 `AbilityRunner.graph` 字段
6. 在其他脚本或 `Start()` 中调用：

```csharp
GetComponent<AbilityRunner>().FireEvent("Attack");
```

### 预期输出（Console）

```
[Ability] 播放动画         ← 立即
[Ability] 播放音效         ← 立即（与上一行几乎同时）
[Ability] 技能结束         ← 1.5 秒后（等待较长分支完成）
```

---

## 架构速查

| 层 | 类 | 职责 |
|----|-----|------|
| Editor | `AbilityGraph` | 图类型注册，`FindNextIndex` 工具 |
| Editor | `IAbilityEditorNode` | 节点转换接口 |
| Editor | `AbilityImporter` | 两遍扫描，生成 `AbilityRuntimeGraph` |
| Editor | `OnEventNode` / `ParallelNode` / `WaitNode` / `LogActionNode` | 编辑器节点定义 |
| Runtime | `AbilityRuntimeNode` | 运行时基类（`[Serializable]`） |
| Runtime | `AbilityRuntimeGraph` | ScriptableObject 主资产（`[SerializeReference]`） |
| Runtime | `AbilityRunner` | MonoBehaviour，协程驱动执行 |

---

## 常见问题

### 为什么运行时节点用 `int` 索引而不是直接引用？

运行时节点是 `[Serializable]` 数据类，不能持有对其他节点的直接引用（引用类型在 Unity 序列化中不安全）。用 `int` 索引 + `nodes[index]` 查找是 Unity ScriptableObject 图系统中的标准做法。

### `[SerializeReference]` vs `[SerializeField]`

`[SerializeField]` 只能序列化具体类型，无法保留多态信息（会丢失子类字段）。
`[SerializeReference]` 使用类型标签序列化，可在同一列表中混合存储不同子类实例。

### 并行节点某条分支不连接时怎么处理？

`FindNextIndex` 对未连接端口返回 `-1`；运行时 `ExecuteParallel` 对 `branchA/branchB < 0` 时直接设 `aDone/bDone = true`，视为立即完成，不会挂起。

---

## 总结

| 特性 | 实现方式 |
|------|---------|
| 事件驱动入口 | `OnEventNode` + `AbilityRuntimeGraph.FindTrigger(name)` |
| 并行执行 | `ParallelRuntimeNode` + `WaitUntil(() => aDone && bDone)` |
| 执行流端口 | `WithConnectorUI(PortConnectorUI.Arrowhead)` |
| Editor→Runtime 转换 | `IAbilityEditorNode.CreateRuntimeNode` + `FindNextIndex` |
| 多态序列化 | 基类 + `[SerializeReference]` 列表 |
| 运行时执行 | `AbilityRunner.FireEvent` + 协程链 |

## 下一步

教程7将介绍**行为树（BehaviorTree）**，进一步深化执行流图的 Editor/Runtime 分离模式，
并引入 Composite（组合）、Decorator（装饰）、Leaf（叶子）三层节点体系。
