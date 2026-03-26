# GraphToolkit 教程设计指南

本文档详细说明每个教程的设计思想、核心概念和关键实现细节。
当前已完成：教程 1–12（全系列）。

---

## 两大图形范式

所有教程围绕两种核心范式展开：

| 范式 | 模式 | 典型场景 | 运行时 |
|------|------|---------|--------|
| **数据流图** | Pull（按需求值） | 计算器、纹理生成、材质 | 不需要——在 Importer 中完成 |
| **执行流图** | Push（顺序触发） | 任务系统、技能系统 | 需要——MonoBehaviour + 协程 |

---

## 通用 GraphToolkit API 速查

```csharp
// 图定义
[Graph("fileext", GraphOptions.Default)]
[Serializable]
internal class MyGraph : Graph { }

// 节点定义
[Node("Display Name", "Category/Sub")]
[UseWithGraph(typeof(MyGraph))]
[Serializable]
internal class MyNode : Node
{
    protected override void OnDefinePorts(IPortDefinitionContext ctx)
    {
        ctx.AddInputPort<float>("A").Build();
        ctx.AddOutputPort<float>("Result").Build();
        // 执行流端口加箭头样式：
        ctx.AddOutputPort("Next").WithConnectorUI(PortConnectorUI.Arrowhead).Build();
    }

    protected override void OnDefineOptions(IOptionDefinitionContext ctx)
    {
        ctx.AddOption<float>("Value").Build();                      // 数值选项（即时更新）
        ctx.AddOption<string>("Label").Delayed().Build();          // 字符串选项（延迟更新，避免每帧重建）
        ctx.AddOption<string>("Text").AsTextArea().Delayed().Build(); // 多行文本区域（仅 string 类型有效）
    }
}

// 图遍历
foreach (var node in graph.GetNodes()) { }           // 枚举所有节点（不含 BlockNode）
var conn = inputPort.FirstConnectedPort;              // 输入端口 → 上游输出端口
var conn = outputPort.FirstConnectedPort;             // 输出端口 → 下游输入端口

// 变量系统（需要 GraphOptions.SupportsSubgraphs）
var v = graph.CreateVariable<Color>("MyColor", Color.white, VariableKind.Output);
v.Name; v.DataType; v.VariableKind;
v.TryGetDefaultValue(out Color c);
v.TrySetDefaultValue(Color.red);

// 子图
[Subgraph(typeof(ParentGraph))]         // 将此图标记为可被 ParentGraph 使用的子图
graph.AddSubgraphNode(subgraph, pos);   // 向主图添加框架自动生成的子图节点
// 子图节点实现 ISubgraphNode：
//   ISubgraphNode.GetSubgraph() → 返回子图实例
//   输出端口自动对应子图的 Output 变量（DisplayName = 变量名）
//   输入端口自动对应子图的 Input 变量（DisplayName = 变量名）
```

---

## 教程1: Hello Graph — 计算器图形

**文件扩展名**: `.calc`
**范式**: 数据流（Pull）
**Assembly**: `Unity.GraphToolkit.Tutorials.HelloGraph.Editor`

### 设计思想

最简单的图：每个节点都实现 `ICalculatorNode`，`CalculatorGraph.EvaluatePort(outputPort)` 递归向上游节点求值，`ScriptedImporter` 找到 `OutputNode` 后触发全图求值，把结果存入 `CalculatorResult` ScriptableObject。

### 关键组件

#### CalculatorGraph.cs
```csharp
public float EvaluatePort(IPort port)
{
    var node = FindNodeForPort(port);
    if (node is ICalculatorNode calc)
        return calc.Evaluate(port, this);
    return 0f;
}

public INode FindNodeForPort(IPort port)
{
    foreach (var node in GetNodes())
    {
        foreach (var p in node.GetInputPorts())  if (p == port) return node;
        foreach (var p in node.GetOutputPorts()) if (p == port) return node;
    }
    return null;
}

public IPort GetConnectedOutputPort(IPort inputPort)
    => inputPort?.FirstConnectedPort;
```

#### ICalculatorNode.cs
```csharp
internal interface ICalculatorNode
{
    float Evaluate(IPort port, CalculatorGraph graph);
}
```

#### AddNode.cs（典型节点）
```csharp
public float Evaluate(IPort port, CalculatorGraph graph)
{
    var connA = graph.GetConnectedOutputPort(m_A);
    var connB = graph.GetConnectedOutputPort(m_B);
    float a = connA != null ? graph.EvaluatePort(connA) : 0f;
    float b = connB != null ? graph.EvaluatePort(connB) : 0f;
    return a + b;
}
```

#### CalculatorGraphImporter.cs
```csharp
var graph  = GraphDatabase.LoadGraphForImporter<CalculatorGraph>(ctx.assetPath);
var result = ScriptableObject.CreateInstance<CalculatorResult>();
if (graph != null)
{
    foreach (var node in graph.GetNodes())
        if (node is OutputNode output) { result.value = output.Evaluate(null, graph); break; }
}
ctx.AddObjectToAsset("main", result);
ctx.SetMainObject(result);
```

### 求值流程
```
[Constant:5] → [Add] → [Output]
[Constant:3] ↗

OutputNode.Evaluate()
  → AddNode.Evaluate()
      → ConstantNode(5).Evaluate() = 5
      → ConstantNode(3).Evaluate() = 3
      → return 8
  → CalculatorResult.value = 8
```

---

## 教程2: 数据流图形 — 纹理生成器

**文件扩展名**: `.texgraph`
**范式**: 数据流（Pull）
**Assembly**: `Unity.GraphToolkit.Tutorials.DataFlow.Editor`

### 设计思想

教程1 的扩展：支持多种数据类型（Texture2D、Color、float、Vector2），每种类型有独立接口和对应的 `EvaluateXxxPort` 方法。`ScriptedImporter` 求值后直接将 `Texture2D` 保存为主资产。

### 多类型评估模式

```csharp
// TextureGraph.cs
public Texture2D EvaluateTexturePort(IPort port)
{
    var node = FindNodeForPort(port);
    if (node is ITextureNode n) return n.EvaluateTexture(port, this);
    return null;
}

public Color EvaluateColorPort(IPort port)
{
    var node = FindNodeForPort(port);
    if (node is IColorNode n) return n.EvaluateColor(port, this);
    return Color.white;
}

public float EvaluateFloatPort(IPort port)
{
    var node = FindNodeForPort(port);
    if (node is IFloatNode n) return n.EvaluateFloat(port, this);
    return 0f;
}
```

### 节点接口
```csharp
internal interface ITextureNode { Texture2D EvaluateTexture(IPort port, TextureGraph graph); }
internal interface IColorNode   { Color     EvaluateColor(IPort port, TextureGraph graph);   }
internal interface IFloatNode   { float     EvaluateFloat(IPort port, TextureGraph graph);   }
```

### 节点列表
| 节点 | 接口 | 功能 |
|------|------|------|
| `UniformColorNode` | `ITextureNode` | 纯色填充纹理 |
| `GradientNode` | `ITextureNode` | 渐变纹理（支持水平/垂直） |
| `NoiseNode` | `ITextureNode` | Perlin 噪声纹理 |
| `BlendNode` | `ITextureNode` | 混合两张纹理 |
| `ColorNode` | `IColorNode` | 颜色常量 |
| `FloatNode` | `IFloatNode` | 浮点常量 |
| `Vector2Node` | — | 二维向量常量 |
| `OutputNode` | — | 求值入口 |

---

## 教程3: 执行流图形 — 任务系统

**文件扩展名**: `.taskgraph`
**范式**: 执行流（Push）
**Assembly**: Editor + Runtime

### 设计思想

引入 Editor/Runtime 分离：编辑器节点在 Importer 中通过 `CreateRuntimeNode(graph)` 转换为纯数据的运行时节点（`[Serializable]`），存入 `TaskRuntimeGraph` ScriptableObject。运行时，`TaskScheduler`（MonoBehaviour）协程驱动，用 **执行器模式（Executor Pattern）** 分离数据与逻辑。

### Editor/Runtime 分离结构

```
Editor/
  TaskGraph.cs          ← [Graph("taskgraph")] + CreateRuntime()
  Nodes/TaskNode.cs     ← abstract 基类 + CreateRuntimeNode(TaskGraph)
  Nodes/LogNode.cs 等   ← 具体编辑器节点
  TaskGraphImporter.cs  ← ScriptedImporter

Runtime/
  TaskRuntimeGraph.cs   ← ScriptableObject，[SerializeReference] 节点列表
  Nodes/TaskRuntimeNode.cs ← 抽象基类，[Serializable]
  Nodes/LogNode.cs 等   ← 具体运行时数据类
  Executors/ITaskExecutor.cs
  Executors/LogExecutor.cs 等
  TaskScheduler.cs      ← MonoBehaviour，协程执行引擎
```

### 节点索引转换

```csharp
// TaskNode.cs（编辑器基类）
protected int GetNextNodeIndex(TaskGraph graph)
{
    var connectedInput = m_Next?.FirstConnectedPort;
    if (connectedInput == null) return -1;
    var allNodes = graph.GetNodes().ToList();
    for (int i = 0; i < allNodes.Count; i++)
        foreach (var p in allNodes[i].GetInputPorts())
            if (p == connectedInput) return i;
    return -1;
}
```

### 运行时节点设计

```csharp
[Serializable]
public abstract class TaskRuntimeNode { }

[Serializable]
public class LogRuntimeNode : TaskRuntimeNode
{
    public string message;
    public int    next = -1;
}

[Serializable]
public class BranchRuntimeNode : TaskRuntimeNode
{
    public bool condition;
    public int  trueBranch  = -1;
    public int  falseBranch = -1;
}
```

### 执行器模式

```csharp
// ITaskExecutor
public interface ITaskExecutor
{
    IEnumerator Execute(TaskRuntimeGraph graph, TaskScheduler scheduler, int nodeIndex);
}

// LogExecutor
public class LogExecutor : ITaskExecutor
{
    public IEnumerator Execute(TaskRuntimeGraph graph, TaskScheduler scheduler, int nodeIndex)
    {
        var node = graph.nodes[nodeIndex] as LogRuntimeNode;
        Debug.Log(node.message);
        yield return node.next;   // 用 int yield 传递下一个索引
    }
}

// TaskScheduler — 执行引擎
private IEnumerator ExecuteGraph()
{
    int current = m_Graph.startNodeIndex;
    while (current >= 0)
    {
        var executor  = GetExecutor(m_Graph.nodes[current]);
        var coroutine = executor.Execute(m_Graph, this, current);
        int next = -1;
        while (coroutine.MoveNext())
        {
            if (coroutine.Current is int idx) next = idx;
            else yield return coroutine.Current;
        }
        current = next;
    }
}
```

---

## 教程4: 变量与子图 — URP 材质生成器

**文件扩展名**: `.matgraph` / `.matconstsubgraph` / `.matvarsubgraph`
**范式**: 数据流（Pull）
**Assembly**: `Unity.GraphToolkit.Tutorials.VariablesSubgraphs.Editor`

### 设计思想

演示 GraphToolkit 的**变量系统**（`IVariable`）和**子图系统**（`[Subgraph]` + 框架自动生成的 `ISubgraphNode`）。`.matgraph` 导入后生成真实 URP Material 资产。

子图分两种：
- **`MaterialConstSubgraph`**（`.matconstsubgraph`）：只有 Output 变量——展示 Output 变量如何成为父图子图节点的输出端口
- **`MaterialVariableSubgraph`**（`.matvarsubgraph`）：有 Input + Output 变量——展示 Input 变量如何成为父图子图节点的输入端口

### 变量系统

```csharp
// 需要 GraphOptions.SupportsSubgraphs 才能使用 Input/Output 变量
[Graph("matgraph", GraphOptions.SupportsSubgraphs)]

// 创建变量（在 Graph.OnEnable / delayCall 中调用）
var v = CreateVariable<Color>("BaseColor", Color.white, VariableKind.Output);
var v = CreateVariable<Color>("Tint",      Color.white, VariableKind.Input);

// 读/写默认值
v.TryGetDefaultValue(out Color c);
v.TrySetDefaultValue(Color.red);

// 枚举所有变量
foreach (var v in GetVariables())
    Debug.Log($"{v.Name} : {v.DataType} [{v.VariableKind}]");
```

### 框架自动生成的节点

对于 `Color`、`float` 等已知类型，框架自动生成：
- **常量节点**（实现 `IConstantNode`）：无需连线，在 Inspector 直接填值
- **变量节点**（实现 `IVariableNode`）：从 Blackboard 读取变量值

`MaterialGraph` 的评估方法处理这两种接口：
```csharp
public Color EvaluateColorPort(IPort port)
{
    var node = FindNodeForPort(port);
    if (node is IColorNode    cn) return cn.EvaluateColor(port, this);
    if (node is IConstantNode c)  { c.TryGetValue(out Color col); return col; }
    if (node is IVariableNode v)  { v.Variable?.TryGetDefaultValue(out Color vc); return vc; }
    if (node is ISubgraphNode s)  return EvaluateSubgraphColorPort(node, s, port);
    return Color.white;
}
```

### 子图系统

```csharp
// 标记为子图类型（框架自动生成子图节点）
[Graph("matconstsubgraph", GraphOptions.SupportsSubgraphs)]
[Subgraph(typeof(MaterialGraph))]
public class MaterialConstSubgraph : MaterialGraph { }

// 向主图添加子图节点（返回的节点实现 ISubgraphNode）
var subgraphNode = AddSubgraphNode(subgraph, position);
var iNode = subgraphNode as INode;

// 子图节点的端口 DisplayName = 变量名，Name = 变量 GUID（需用 DisplayName 查找）
var port = GetPortByDisplayName(iNode.GetOutputPorts(), "BaseColor");
```

### 子图求值

```csharp
private Color EvaluateSubgraphColorPort(INode sgNode, ISubgraphNode sg, IPort outputPort)
{
    var subgraph = sg.GetSubgraph() as MaterialGraph;
    if (subgraph == null) return Color.white;

    // 1. 将主图连到子图输入端口的值注入子图 Input 变量
    InjectSubgraphInputs(sgNode, subgraph);

    // 2. 在子图中找到 Output 变量节点，沿连线求值
    foreach (var node in subgraph.GetNodes())
    {
        if (node is IVariableNode vn
            && vn.Variable?.Name == outputPort.DisplayName
            && vn.Variable.VariableKind == VariableKind.Output)
        {
            foreach (var inPort in node.GetInputPorts())
            {
                var conn = subgraph.GetConnectedOutputPort(inPort);
                if (conn != null) return subgraph.EvaluateColorPort(conn);
            }
            Color c = Color.white;
            vn.Variable.TryGetDefaultValue(out c);
            return c;
        }
    }
    return Color.white;
}
```

### 生成 URP Material

```csharp
// MaterialGraphImporter.cs
var graph = GraphDatabase.LoadGraphForImporter<MaterialGraph>(ctx.assetPath);
var mat   = graph?.CreateMaterial();   // EvaluateAll + new Material("URP/Lit")

if (mat != null)
{
    mat.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
    ctx.AddObjectToAsset("material", mat);
    ctx.SetMainObject(mat);
}
```

---

## 教程5: ContextNode 与 BlockNode — 着色器函数图

**文件扩展名**: `.shaderfunc`
**范式**: 数据流（Pull，纯 Editor）
**Assembly**: `Unity.GraphToolkit.Tutorials.ContextBlocks.Editor`

### 设计思想

演示 `ContextNode`（函数容器）和 `BlockNode`（可组合操作单元）。设计类似 Shader Graph 的自定义函数节点：`FunctionContextNode` 接收 `Input`（Vector3），依次应用内部各 BlockNode 变换，从 `Result` 端口输出。外部的 `OutputNode` 连接到 `Result` 端口后触发全图求值。

### ContextNode vs BlockNode

```csharp
// ContextNode：有 [UseWithGraph]，可在图中放置，可容纳 BlockNode
[Node("Function", "")]
[UseWithGraph(typeof(ShaderFunctionGraph))]
internal class FunctionContextNode : ContextNode
{
    // 框架内置 API：
    //   BlockNodes        — IEnumerable<BlockNode>，遍历子块（Graph.GetNodes() 不含 BlockNode）
    //   BlockCount        — 子块数量
    //   GetBlock(int idx) — 按索引取子块
}

// BlockNode：有 [UseWithContext]，只能放在指定 ContextNode 内
[Node("Operation", "")]
[UseWithContext(typeof(FunctionContextNode))]
internal class AddBlockNode : BlockNode
{
    // 框架内置 API：
    //   ContextNode — 父上下文节点（框架自动维护）
    //   Index       — 在父块列表中的位置
}
```

### TryGetValue 的关键限制

`IPort.TryGetValue<T>()` 在 INPUT 端口上只读**内联常量**（编辑器直接填的值），**不会**沿连线读上游节点：

```csharp
// 错误示范 —— 无法读取连线上游的值
m_Input.TryGetValue<Vector3>(out result);   // 始终返回内联常量，忽略连线

// 正确做法 —— 先取连线，再求值
var upstream = graph.GetConnectedOutputPort(m_Input);
if (upstream != null)
    result = graph.EvaluateVectorPort(upstream);
else
    m_Input.TryGetValue<Vector3>(out result);  // 只有真正没有连线时才用 TryGetValue
```

### 求值流程

```csharp
// FunctionContextNode.EvaluateVector()
public Vector3 EvaluateVector(ShaderFunctionGraph graph)
{
    Vector3 result = Vector3.zero;
    var upstream = graph.GetConnectedOutputPort(m_Input);
    if (upstream != null) result = graph.EvaluateVectorPort(upstream);
    else m_Input.TryGetValue<Vector3>(out result);

    foreach (var block in BlockNodes)
    {
        if (block is AddBlockNode       add)  result = add.Apply(result);
        else if (block is MultiplyBlockNode m) result = m.Apply(result);
        else if (block is CrossBlockNode    c) result = c.Apply(result);
        else if (block is NormalizeBlockNode n)result = n.Apply(result);
    }
    return result;
}
```

### 节点列表

| 类 | 类型 | 功能 |
|----|------|------|
| `FunctionContextNode` | `ContextNode` | 函数容器，持有 Input/Result 端口 |
| `AddBlockNode` | `BlockNode` | `accumulated + B` |
| `MultiplyBlockNode` | `BlockNode` | `accumulated * Factor` |
| `CrossBlockNode` | `BlockNode` | `Cross(accumulated, B)` |
| `NormalizeBlockNode` | `BlockNode` | `normalize(accumulated)` |
| `OutputNode` | `Node` | 接收 Result，触发 `Evaluate()` 并打印 |

---

## 教程6: 技能系统 — 事件驱动与并行执行

**文件扩展名**: `.ability`
**范式**: 执行流（Push）
**Assembly**: Editor + Runtime（含 `Unity.InputSystem` 依赖）

### 设计思想

在教程3 的基础上引入两个新概念：
1. **事件驱动入口**：`OnEventNode` 监听具名事件，`AbilityRunner.FireEvent(name)` 匹配触发
2. **并行执行**：`ParallelNode` 同时启动两条协程分支，用 `WaitUntil` 等待全部完成

Editor/Runtime 转换沿用"两步扫描"方案：第一步建 `INode → index` 映射，第二步调用 `CreateRuntimeNode` 解析连接索引。

### Editor 节点

```csharp
// AbilityGraph.cs — FindNextIndex 工具
internal static int FindNextIndex(IPort outputPort, List<INode> allNodes, Dictionary<INode, int> indexMap)
{
    var connectedInput = outputPort?.FirstConnectedPort;
    if (connectedInput == null) return -1;
    foreach (var node in allNodes)
        foreach (var p in node.GetInputPorts())
            if (p == connectedInput)
                return indexMap.TryGetValue(node, out int idx) ? idx : -1;
    return -1;
}

// 节点声明示例（OnEventNode）
[Node("AbilityGraph", "")]
[UseWithGraph(typeof(AbilityGraph))]
internal class OnEventNode : Node, IAbilityEditorNode
{
    public AbilityRuntimeNode CreateRuntimeNode(List<INode> allNodes, Dictionary<INode, int> indexMap)
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

### AbilityImporter — 两步扫描

```csharp
var allNodes = graph.GetNodes().ToList();

// 第一步：建立索引映射
var indexMap = new Dictionary<INode, int>();
for (int i = 0; i < allNodes.Count; i++)
    if (allNodes[i] is IAbilityEditorNode)
        indexMap[allNodes[i]] = i;

// 第二步：所有索引就绪后，创建运行时节点
foreach (var node in allNodes)
    if (node is IAbilityEditorNode an)
        runtimeGraph.nodes.Add(an.CreateRuntimeNode(allNodes, indexMap));
```

> 为什么要两步？第二步 `CreateRuntimeNode` 需要通过 `FindNextIndex` 查找下游节点的索引。一步扫描时后续节点可能尚未加入 `indexMap`，导致索引缺失。

### 运行时节点

```csharp
// [SerializeReference] 是必须的：支持 List<AbilityRuntimeNode> 的多态序列化
[SerializeReference]
public List<AbilityRuntimeNode> nodes = new();

[Serializable] public abstract class AbilityRuntimeNode { }
[Serializable] public class OnEventRuntimeNode  : AbilityRuntimeNode { public string eventName; public int next; }
[Serializable] public class ParallelRuntimeNode : AbilityRuntimeNode { public int branchA, branchB, done; }
[Serializable] public class WaitRuntimeNode     : AbilityRuntimeNode { public float duration; public int next; }
[Serializable] public class LogActionRuntimeNode: AbilityRuntimeNode { public string message;  public int next; }
```

### AbilityRunner — 协程执行引擎

```csharp
public void FireEvent(string eventName)
{
    int idx = graph.FindTrigger(eventName);
    if (idx < 0) return;
    var trigger = graph.nodes[idx] as OnEventRuntimeNode;
    StartCoroutine(ExecuteFrom(trigger.next));
}

private IEnumerator ExecuteFrom(int nodeIndex)
{
    while (nodeIndex >= 0 && nodeIndex < graph.nodes.Count)
    {
        switch (graph.nodes[nodeIndex])
        {
            case WaitRuntimeNode w:
                yield return new WaitForSeconds(w.duration);
                nodeIndex = w.next; break;
            case LogActionRuntimeNode log:
                Debug.Log($"[Ability] {log.message}");
                nodeIndex = log.next; break;
            case ParallelRuntimeNode p:
                yield return StartCoroutine(ExecuteParallel(p));
                nodeIndex = p.done; break;
            default: yield break;
        }
    }
}

private IEnumerator ExecuteParallel(ParallelRuntimeNode p)
{
    bool aDone = p.branchA < 0, bDone = p.branchB < 0;
    if (!aDone) StartCoroutine(ExecuteAndSignal(p.branchA, () => aDone = true));
    if (!bDone) StartCoroutine(ExecuteAndSignal(p.branchB, () => bDone = true));
    yield return new WaitUntil(() => aDone && bDone);
}
```

### 节点列表

| 节点 | 属性 | 功能 |
|------|------|------|
| `OnEventNode` | `[Node("AbilityGraph", "")]` | 事件入口，匹配 `FireEvent` |
| `ParallelNode` | `[Node("AbilityGraph", "")]` | 并行分支（Branch A / Branch B / Done） |
| `WaitNode` | `[Node("Wait", "Ability/Action")]` | 等待 N 秒 |
| `LogActionNode` | `[Node("AbilityGraph", "")]` | 打印消息（模拟技能效果） |

---

## 教程7: 行为树系统 — AI 决策树

**文件扩展名**: `.behaviortree`
**范式**: 执行流（Push）
**Assembly**: Editor + Runtime（含 `Unity.InputSystem` 依赖）

### 设计思想

三层节点架构（Composite / Decorator / Leaf）+ 运行时黑板（Blackboard）+ Executor 模式。核心差异：每个节点有**成功/失败**语义（`NodeStatus`），父节点根据子节点返回值决定控制流，而不是教程6 那样的简单链式跳转。

### 三层节点架构

```
BTNode（抽象基类）
├── CompositeNode  — 多子节点（Children 端口，多连接）
│   ├── SequenceNode  — AND：顺序执行，任一失败即失败
│   ├── SelectorNode  — OR：顺序尝试，任一成功即成功
│   └── ParallelNode  — 同时执行，SuccessPolicy 控制成功条件
├── DecoratorNode  — 单子节点（Child 端口）
│   ├── InverterNode    — 反转子节点结果
│   ├── RepeaterNode    — 重复 N 次（或无限循环）
│   ├── SucceederNode   — 强制返回成功
│   └── ConditionalNode — 检查 Blackboard bool 变量
└── LeafNode       — 无子节点，执行具体行为
    ├── WaitNode               — 等待 duration 秒
    ├── LogNode                — 打印日志（支持 LogType + AsTextArea）
    ├── SetBlackboardValueNode — 写 Blackboard 字符串值
    ├── CheckBlackboardValueNode — 比较 Blackboard 字符串值
    └── RandomSuccessNode      — 按概率随机返回成功/失败
```

### 端口设计

```csharp
// BTNode.cs — 所有非根节点都有 Parent 输入端口
protected void AddParentPort(IPortDefinitionContext context)
    => m_ParentPort = context.AddInputPort("Parent").WithConnectorUI(PortConnectorUI.Arrowhead).Build();

// CompositeNode — Children 输出端口（多连接）
protected void AddChildrenPort(IPortDefinitionContext context)
    => m_ChildrenPort = context.AddOutputPort("Children").WithConnectorUI(PortConnectorUI.Arrowhead).Build();

// DecoratorNode / RootNode — Child 输出端口（单连接）
protected void AddChildPort(IPortDefinitionContext context)
    => m_ChildPort = context.AddOutputPort("Child").WithConnectorUI(PortConnectorUI.Arrowhead).Build();
```

### GetConnectedInputPorts — 多子节点遍历

GraphToolkit 的 `outputPort.FirstConnectedPort` 只能找到单条连线，多连线需要反向遍历：

```csharp
// BehaviorTreeGraph.cs
public List<IPort> GetConnectedInputPorts(IPort outputPort)
{
    var ports = new List<IPort>();
    foreach (var node in GetNodes())
        foreach (var inputPort in node.GetInputPorts())
            if (inputPort.FirstConnectedPort == outputPort)   // 反向匹配
                ports.Add(inputPort);
    return ports;
}
```

子节点顺序由 `graph.GetNodes()` 枚举顺序决定（即图中节点创建顺序）。

### Importer 单步转换

与教程6 的两步扫描不同，行为树直接单步转换：

```csharp
// BehaviorTreeImporter.cs
var allNodes = new List<INode>(graph.GetNodes());
for (int i = 0; i < allNodes.Count; i++)
    if (allNodes[i] is BTNode btNode)
        runtimeTree.nodes.Add(btNode.CreateRuntimeNode(graph));
```

`CreateRuntimeNode` 内部调用 `child.GetNodeIndex(graph)` 即时计算索引（每次重新遍历节点列表），无需预建 indexMap。

```csharp
// BTNode.cs
public int GetNodeIndex(BehaviorTreeGraph graph)
{
    var allNodes = new List<INode>(graph.GetNodes());
    for (int i = 0; i < allNodes.Count; i++)
        if (allNodes[i] == this) return i;
    return -1;
}
```

### 节点状态与执行器模式

```csharp
// 核心状态枚举
public enum NodeStatus { Running, Success, Failure }

// IBTExecutor — 协程通过 yield return NodeStatus.xxx 传递结果
public interface IBTExecutor
{
    IEnumerator Execute(BehaviorTreeRuntime tree, int nodeIndex, Blackboard blackboard);
}

// 父节点读取子协程状态的标准模式
NodeStatus childStatus = NodeStatus.Running;
while (childCoroutine.MoveNext())
{
    if (childCoroutine.Current is NodeStatus status)
        childStatus = status;        // 捕获最终状态
    else
        yield return childCoroutine.Current;  // 透传 WaitForSeconds 等
}
// 使用 childStatus 决定后续逻辑
```

### BehaviorTreeRunner — 静态执行器字典

```csharp
// 初始化时建立 Type → IBTExecutor 映射（单例，跨帧复用）
s_Executors = new Dictionary<Type, IBTExecutor>
{
    { typeof(SequenceNode),            new SequenceExecutor() },
    { typeof(SelectorNode),            new SelectorExecutor() },
    { typeof(ParallelNode),            new ParallelExecutor() },
    { typeof(InverterNode),            new InverterExecutor() },
    { typeof(RepeaterNode),            new RepeaterExecutor() },
    { typeof(SucceederNode),           new SucceederExecutor() },
    { typeof(ConditionalNode),         new ConditionalExecutor() },
    { typeof(WaitNode),                new WaitExecutor() },
    { typeof(LogNode),                 new LogExecutor() },
    { typeof(SetBlackboardValueNode),  new SetBlackboardValueExecutor() },
    { typeof(CheckBlackboardValueNode),new CheckBlackboardValueExecutor() },
    { typeof(RandomSuccessNode),       new RandomSuccessExecutor() }
};

// 执行入口：从根节点的子节点开始
private IEnumerator ExecuteBehaviorTree()
{
    do
    {
        var rootNode = m_BehaviorTree.GetRootNode();
        var executor = GetExecutor(m_BehaviorTree.GetNode(rootNode.childIndex));
        var coroutine = executor.Execute(m_BehaviorTree, rootNode.childIndex, m_Blackboard);
        // ... 透传协程直至完成
    } while (m_Loop);   // m_Loop = true 时循环执行
}
```

### Blackboard — 泛型键值存储

```csharp
// 写入（支持任意类型）
blackboard.SetValue("state", "patrol");
blackboard.SetValue("isAlert", true);

// 读取（带默认值）
string state = blackboard.GetValue("state", string.Empty);
bool isAlert  = blackboard.GetValue("isAlert", false);
```

### 节点列表

| 节点（Editor） | `[Node]` 属性 | 运行时类 | 功能 |
|----------------|--------------|---------|------|
| `RootNode` | `[Node("Root", "Behavior Tree")]` | `RootNode` | 树的起点，单子节点 |
| `SequenceNode` | `[Node("Composite", "")]` | `SequenceNode` | 顺序执行，任一失败则失败 |
| `SelectorNode` | `[Node("Composite", "")]` | `SelectorNode` | 顺序选择，任一成功则成功 |
| `ParallelNode` | `[Node("Composite", "")]` | `ParallelNode` | 并行执行，`SuccessPolicy` 控制 |
| `InverterNode` | `[Node("Decorator", "")]` | `InverterNode` | 反转子节点结果 |
| `RepeaterNode` | `[Node("Decorator", "")]` | `RepeaterNode` | 重复 N 次（或无限循环） |
| `SucceederNode` | `[Node("Decorator", "")]` | `SucceederNode` | 强制返回成功 |
| `ConditionalNode` | `[Node("Conditional", "Behavior Tree/Decorator")]` | `ConditionalNode` | 检查 Blackboard bool 变量 |
| `WaitNode` | `[Node("Action", "")]` | `WaitNode` | 等待 duration 秒 |
| `LogNode` | `[Node("Action", "")]` | `LogNode` | 打印日志，支持 `LogType` |
| `SetBlackboardValueNode` | `[Node("Action", "")]` | `SetBlackboardValueNode` | 设置 Blackboard 字符串值 |
| `CheckBlackboardValueNode` | `[Node("Action", "")]` | `CheckBlackboardValueNode` | 比较 Blackboard 字符串值 |
| `RandomSuccessNode` | `[Node("Action", "")]` | `RandomSuccessNode` | 按概率随机成功/失败 |

---

## 各教程关键文件对照表

| 教程 | 文件扩展名 | Graph 类 | Runtime? | 主要输出 |
|------|-----------|---------|----------|---------|
| 01 | `.calc` | `CalculatorGraph` | 否 | `CalculatorResult` SO |
| 02 | `.texgraph` | `TextureGraph` | 否 | `Texture2D` 资产 |
| 03 | `.taskgraph` | `TaskGraph` | 是（Executor 模式） | `TaskRuntimeGraph` SO |
| 04 | `.matgraph` / `.mat*subgraph` | `MaterialGraph` | 否 | URP `Material` 资产 |
| 05 | `.shaderfunc` | `ShaderFunctionGraph` | 否 | `ShaderFunctionData` SO |
| 06 | `.ability` | `AbilityGraph` | 是（协程引擎） | `AbilityRuntimeGraph` SO |
| 07 | `.behaviortree` | `BehaviorTreeGraph` | 是（Executor + Blackboard） | `BehaviorTreeRuntime` SO |
| 08 | `.dialogue` | `DialogueGraph` | 是（UnityEvent + MonoBehaviour UI） | `DialogueRuntimeGraph` SO |
| 09 | `.rendergraph` | `RenderGraph` | 是（ScriptableRendererFeature） | `RenderGraphRuntime` SO |
| 10 | `.urpgraph` | `URPGraph` | 是（完整 URP RenderGraph） | `URPGraphRuntime` SO |
| 11 | `.newrendergraph` | `NewRenderGraph` | 是（ContextNode + BlockNode 模式） | `NewRenderGraphRuntime` SO |
| 12 | `.balancegraph` | `BalanceGraph` | 否（纯 Pull，直接输出 SO） | `BalanceConfig` SO |

---

## 常见陷阱与解决方案

### 1. `TryGetValue` 无法读取连线上游值
**问题**：对 INPUT 端口调用 `TryGetValue` 只读内联常量，连线上游的节点值读不到。
**解决**：先用 `GetConnectedOutputPort(inputPort)` 取上游输出端口，再调用 `EvaluateXxxPort()`。

### 2. `[SerializeReference]` vs `[SerializeField]`
**问题**：`[SerializeField]` 不能序列化多态引用（子类字段会丢失）。
**解决**：存储抽象基类列表时用 `[SerializeReference]`。

### 3. 子图节点端口名是变量 GUID，不是变量名
**问题**：框架自动生成的子图节点，`IPort.Name` 是变量 GUID，`IPort.DisplayName` 才是变量名。
**解决**：用 `DisplayName` 做匹配查找。

### 4. `AddOption<string>().Delayed()` 的必要性
**问题**：不加 `.Delayed()` 时，每次按键都会触发节点重建，体验很差。
**解决**：字符串和数值选项加 `.Delayed().Build()`，失焦时才触发更新。

### 5. 执行流索引转换需要两步扫描
**问题**：`CreateRuntimeNode` 需要查找下游节点索引，但一步扫描时索引可能还没建立。
**解决**：先扫一遍建 `indexMap`，再扫一遍调用 `CreateRuntimeNode`。

### 6. `SaveGraph` 后不能再访问 `this`
**问题**：`GraphDatabase.SaveGraph(this)` 触发 reimport，会销毁当前的 `GraphObjectImp` 内部对象，之后的任何 API 调用都会抛异常。
**解决**：`SaveGraph` 必须是方法的最后一行。

### 7. `AsTextArea()` 让字符串选项显示为多行文本框
**用法**：`context.AddOption<string>("Message").AsTextArea().Delayed().Build()`
**适用**：仅 `string` 类型选项，结合 `.Delayed()` 避免每次击键重建节点。
**示例**：教程7 的 `LogNode` 用此方式显示多行日志内容。

---

---

## 教程8: 对话系统 — 分支对话与 UnityEvent

**文件扩展名**: `.dialogue`
**范式**: 执行流（Push）
**Assembly**: Editor + Runtime（含 `Unity.InputSystem` 依赖）

### 设计思想

对话系统将图的执行流与 Unity UI 解耦——运行时图只触发 `UnityEvent`（如 `OnDialogueText`、`OnChoice`、`OnEvent`），具体 UI 实现挂在场景中由 `DialogueUI` 等组件订阅这些事件。核心挑战：如何在有多条输出线（Choice 分支）时正确查找下游节点索引。

### 关键技术点

```csharp
// graph.FindNodeForPort — IPort 无 .Node 属性，必须遍历查找
public INode FindNodeForPort(IPort port)
{
    foreach (var node in GetNodes())
    {
        foreach (var p in node.GetInputPorts())  if (p == port) return node;
        foreach (var p in node.GetOutputPorts()) if (p == port) return node;
    }
    return null;
}

// ChoiceNode — 2个固定输出端口 + 2个 INodeOption（选项文本）
// BranchNode — "True"/"False" 输出端口 + "Condition Key"/"Expected Value" 选项
// DialogueRunner — UnityEvent<string> OnDialogueText, OnChoice, OnEvent
```

### 节点列表

| 节点 | 功能 |
|------|------|
| `StartNode` | 对话入口（只有 Out） |
| `DialogueTextNode` | 显示台词（Speaker + Text，AsTextArea） |
| `ChoiceNode` | 玩家选择（2个固定输出 + INodeOption 文本） |
| `BranchNode` | 条件分支（检查变量值） |
| `SetVariableNode` | 设置对话变量 |
| `TriggerEventNode` | 触发命名事件（UnityEvent） |
| `EndNode` | 对话结束 |

---

## 教程9: 渲染图基础 — URP RenderGraph API 入门

**文件扩展名**: `.rendergraph`
**范式**: 执行流（Push）
**Assembly**: Editor + Runtime（URP）

### 设计思想

最小化的 URP RenderGraph 演示：将图执行流映射到 `RecordRenderGraph`（Unity 6 唯一入口），每个节点调用独立的 `AddRasterRenderPass`，使每个节点在 Frame Debugger 中有独立条目。

### 关键技术点

```csharp
// 每节点一个 AddRasterRenderPass — Frame Debugger 可见性的核心
// SetRenderAttachment 触发 RenderGraph 自动插入 SetRenderTarget GPU 命令
// AllowPassCulling(false) — 防止 Pass 被 RenderGraph 裁剪
// Blitter 双 Pass 模式 — 同一纹理不能同时 UseTexture(读) 和 SetRenderAttachment(写)
// [SerializeReference] — 运行时节点多态列表必须使用
```

### 节点列表

| 节点 | 功能 |
|------|------|
| `CameraNode` | 图的起点，配置相机 Tag |
| `RenderPassNode` | 绘制场景（PassName / PassEvent / LayerMask） |
| `BlitNode` | 全屏后处理（Material） |
| `OutputNode` | 图的终点 |

---

## 教程10: 完整图形化 URP 渲染管线

**文件扩展名**: `.urpgraph`
**范式**: 执行流（Push）
**Assembly**: Editor + Runtime（URP）

### 设计思想

生产级图形化渲染管线：多种 Pass 节点（Opaque / Transparent / Shadow / Skybox / PostProcess / Custom）+ 条件分支节点（Quality / Platform），`RecordRenderGraph` CPU 阶段决策分支，每个真实 GPU 节点调用独立 `AddRasterRenderPass`，Shadow/Skybox/PostProcess 节点为标记节点（不注入 GPU 命令，由 URP 内置处理）。

### 关键技术点

```csharp
// GetNodeIndex 只计数 URPNode 实例（与 CreateRuntimeGraph 过滤逻辑对齐）
// graph.FindNodeForPort — IPort 无 .Node 属性
// [SerializeReference] on List<URPRuntimeNode> — 多态序列化
// QualityBranchNode — CPU 阶段 QualitySettings.GetQualityLevel() 决策
// PlatformBranchNode — CPU 阶段 Application.isMobilePlatform 决策
// 标记节点（Shadow/Skybox/PostProcess）— current = node.nextNodeIndex，不发 GPU 命令
// AddOption 只有 INodeOption + TryGetValue 一种模式，getter/setter 3 参数形式不支持
```

### 节点分类

| 分类 | 节点 | 运行时行为 |
|------|------|-----------|
| 管线控制 | `PipelineStart` / `PipelineEnd` | 入口 / 终止遍历 |
| 真实 GPU | `OpaquePass` / `TransparentPass` / `CustomPass` | `DrawRendererList` / `Blitter` |
| 标记节点 | `ShadowPass` / `SkyboxPass` / `PostProcessPass` | 跳过，URP 内置处理 |
| 资源 | `RenderTexture` | 占位符（RenderGraph 内部管理） |
| 控制 | `QualityBranch` / `PlatformBranch` | CPU 阶段分支 |

---

## 教程11: ContextNode + BlockNode — 新 RenderGraph 为例

**文件扩展名**: `.newrendergraph`
**范式**: 执行流（Push）
**Assembly**: Editor + Runtime（URP）

### 设计思想

演示 `ContextNode`（可容纳 BlockNode 的容器）和 `BlockNode`（受 `[UseWithContext]` 限制的子操作单元）模式。`RenderPassNode` 是一个 ContextNode，用户可以在其内部任意组合 Clear / DrawOpaque / DrawTransparent / Blit 等 BlockNode，运行时将非 Blit 操作合并为一个 `AddRasterRenderPass`，减少 SetRenderTarget 切换。

### 核心 API 对比（T10 vs T11）

| 维度 | T10（URPGraph） | T11（NewRenderGraph） |
|------|----------------|----------------------|
| 节点粒度 | 每种操作独立节点 | ContextNode 内嵌多个 BlockNode |
| 新增操作方式 | 图中添加节点并连线 | RenderPassNode 内拖入 BlockNode |
| 子节点访问 | N/A | `ContextNode.BlockNodes`（`GetNodes()` 不含 BlockNode） |
| GPU Pass 策略 | 每节点一个 Pass | 非 Blit 合并为 1 Pass；Blit 独立双 Pass |

### 关键技术点

```csharp
// ContextNode — 继承 Node，有 BlockNodes 属性，可容纳 BlockNode
// BlockNode — [UseWithContext(typeof(RenderPassNode))] 限制放置位置
// GetNodes() — 不含 BlockNode，遍历 BlockNode 必须用 ContextNode.BlockNodes
// port.TryGetValue<T> — 读取端口内联常量（值类型）
// INodeOption.TryGetValue<T> — 读取选项值（引用类型如 Material）
// [SerializeReference] on List<PassOperation> — 操作多态列表
// 合并策略：Clear + DrawOpaque + DrawTransparent → 1 个 AddRasterRenderPass
//            每个 BlitOperation → 独立双 Pass（读写约束）
```

### BlockNode 列表

| BlockNode | 端口/选项 | 运行时操作 |
|-----------|-----------|-----------|
| `ClearBlockNode` | Port: Color, bool | `ClearOperation { clearColor, clearDepth }` |
| `DrawOpaqueBlockNode` | Port: LayerMask | `DrawOpaqueOperation { layerMask }` |
| `DrawTransparentBlockNode` | Port: LayerMask | `DrawTransparentOperation { layerMask }` |
| `BlitBlockNode` | Option: Material | `BlitOperation { material }` |

---

## 教程 12：节点图序列化与版本迁移（Graph Versioning）

**文件扩展名**：`.balancegraph`
**图形范式**：Pull 模型（无连线，按类型扫描节点）
**运行时**：无（直接输出 `BalanceConfig` ScriptableObject）

### 设计思想

演示随项目迭代出现的节点结构变更如何通过版本号机制实现向后兼容。采用"无连线纯数据节点"模式——节点只有选项，没有端口，Importer 直接按类型扫描所有节点。

### 核心知识点

1. **双版本号机制**：
   - `[ScriptedImporter(version, "ext")]`：Importer 版本，递增触发全量重导入
   - `BalanceGraph.m_SchemaVersion`：资产内部版本，记录该文件保存时的 schema

2. **选项序列化规则**：GraphToolkit 以 `__option_<名称>` 为键存储选项，新增选项不破坏旧资产，重命名则会丢失已有值

3. **迁移逻辑**：V1 资产 `WeaponStatsNode` 无 Range 选项，`TryGetValue` 返回 false，Importer 按 `BaseDamage >= 30 ? 2.5 : 1.0` 推断范围

4. **标记节点（Marker Node）**：`BalanceOutputNode` 无端口无选项，仅作视觉标识，Importer 不依赖它

5. **运行时纯 SO 输出**：`BalanceConfig` + `EnemyConfig[]` + `WeaponConfig[]` 在 Runtime assembly 中，不引用任何 Editor API

---

## 后续教程规划（待实现）

暂无规划——全系列 12 个教程已全部完成。如需扩展，可考虑：
- 教程13：多摄像机渲染图（Multi-Camera RenderGraph）
- 教程14：节点图单元测试（Graph Unit Testing）
