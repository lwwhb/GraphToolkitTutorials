# 教程5: ContextNode 和 BlockNode — 着色器函数图

## 概述

本教程通过 **ShaderFunctionGraph**（`.shaderfunc`）演示 GraphToolkit 中的两种特殊节点类型：**ContextNode** 和 **BlockNode**。

设计思路类似 Shader Graph 的自定义函数节点：一个 `FunctionContextNode` 作为函数容器，内部嵌套若干 `BlockNode` 依次对向量做变换，最终通过 `OutputNode` 将结果打印出来。

这仍然是一个**数据流图（Pull 模式）**，`.shaderfunc` 文件导入时在 `ScriptedImporter` 中完成求值。

---

## 学习目标

- 理解 `ContextNode` 与 `BlockNode` 的设计意图和框架内置 API
- 使用 `[UseWithContext]` 将 `BlockNode` 与 `ContextNode` 关联
- 使用 `ContextNode.BlockNodes` 遍历子块（`Graph.GetNodes()` 不包含 BlockNode）
- 掌握 **`TryGetValue` 只读内联常量** 这一关键限制及其绕过方法
- 使用 `GetConnectedOutputPort + EvaluateVectorPort` 读取连线上游的值

---

## 核心概念

### ContextNode vs BlockNode vs 普通 Node

| 类型 | 放置位置 | 能否含 BlockNode | 属性标记 |
|------|---------|----------------|---------|
| 普通 `Node` | 图中任意位置 | ❌ | `[UseWithGraph]` |
| `ContextNode` | 图中任意位置 | ✅ | `[UseWithGraph]` |
| `BlockNode` | 只能在 ContextNode 内 | ❌ | `[UseWithContext]` |

### 关键框架 API

#### ContextNode 内置成员
```csharp
IEnumerable<BlockNode> BlockNodes      // 遍历所有子块（Graph.GetNodes() 不含 BlockNode）
int                    BlockCount      // 子块数量
BlockNode              GetBlock(int i) // 按索引取子块
```

#### BlockNode 内置属性
```csharp
ContextNode ContextNode  // 父上下文节点（框架自动维护，无需手动查找）
int         Index        // 在父块列表中的位置（0 起）
```

### TryGetValue 的关键限制

**`IPort.TryGetValue<T>()`** 在 INPUT 端口上只读 **内联常量**（编辑器里直接填的值），
**不会**沿连线读取上游节点的值。

```
InputPort ──wire──> ConstantNodeModelImp(1, 0, 0.4)

port.TryGetValue<Vector3>(out v)   // v = (0,0,0)  ← 读的是内联常量，不是上游值！
```

正确做法：

```csharp
// 1. 取得上游的 output port
var upstream = graph.GetConnectedOutputPort(inputPort);   // inputPort.FirstConnectedPort

// 2. 对 output port 求值
Vector3 result = graph.EvaluateVectorPort(upstream);
```

---

## 项目结构

```
05_ContextBlocks/
└─ Editor/
   ├── ShaderFunctionGraph.cs          # [Graph("shaderfunc")] + 求值入口
   ├── ShaderFunctionData.cs           # ScriptableObject 主资产（导入结果）
   ├── ShaderFunctionImporter.cs       # ScriptedImporter：.shaderfunc → ShaderFunctionData
   └── Nodes/
       ├── FunctionContextNode.cs      # 函数上下文节点（ContextNode）
       ├── OperationBlockNode.cs       # 运算块：Add / Multiply / Cross / Normalize
       ├── OutputBlockNode.cs          # 输出节点（普通 Node，打印结果）
       ├── ConstantNode.cs             # 空文件（使用系统内置黄色常量节点替代）
       ├── InputBlockNode.cs           # 空文件（由 FunctionContextNode.Input 端口替代）
       └── IShaderNode.cs              # 空文件（接口已移除）
```

---

## 图结构

```
ShaderFunctionGraph
├─ ConstantNodeModelImp (系统内置黄色常量节点，值 = (1, 0, 0.4))
│      ↓ wire → FunctionContextNode.Input
│
├─ FunctionContextNode  (ContextNode) ── [UseWithGraph]
│  │   Input  : Vector3  ← 接收外部向量
│  │   Result : Vector3  → 输出计算结果
│  │
│  ├─ CrossBlockNode     (BlockNode)  ── [UseWithContext]  B = (0.5, 1, 1)
│  ├─ AddBlockNode       (BlockNode)  ── [UseWithContext]  B = (3, -3, 1)
│  ├─ MultiplyBlockNode  (BlockNode)  ── [UseWithContext]  Factor = 3
│  └─ NormalizeBlockNode (BlockNode)  ── [UseWithContext]  无端口
│
└─ OutputNode (普通 Node) ── [UseWithGraph]
       Result : Vector3  ← wire ← FunctionContextNode.Result
```

**数据流（导入时求值）**：

```
(1, 0, 0.4)                        ← ConstantNodeModelImp，TryGetValue 兜底读取
  → Cross(B=0.5,1,1)  = (-0.4, -0.8, 1.0)
  → Add(B=3,-3,1)     = (2.6, -3.8, 2.0)
  → Multiply(×3)      = (7.8, -11.4, 6.0)
  → Normalize         ≈ (0.518, -0.757, 0.398)
  → OutputNode.Evaluate() → Debug.Log
```

---

## 节点详解

### ShaderFunctionGraph

```csharp
[Graph("shaderfunc", GraphOptions.Default)]
[Serializable]
public class ShaderFunctionGraph : Graph
{
    [MenuItem("Assets/Create/Graph Toolkit/ShaderFunctionGraph")]
    static void CreateGraphAssetFile()
        => GraphDatabase.PromptInProjectBrowserToCreateNewAsset<ShaderFunctionGraph>();

    // 新建资产时自动添加 FunctionContextNode
    public override void OnEnable()
    {
        base.OnEnable();
        EditorApplication.delayCall += EnsureDefaultContent;
    }

    private void EnsureDefaultContent()
    {
        EditorApplication.delayCall -= EnsureDefaultContent;
        foreach (var node in GetNodes())
            if (node is FunctionContextNode) return;
        AddNode(new FunctionContextNode());
        GraphDatabase.SaveGraph(this);
        // 注意：SaveGraph 后禁止访问 this（reimport 会销毁内部对象）
    }

    // 对 output port 求值，优先级：FunctionContextNode > IVariableNode > IConstantNode > TryGetValue 兜底
    public Vector3 EvaluateVectorPort(IPort port) { ... }
    public float   EvaluateFloatPort(IPort port)  { ... }

    // 取输入端口连线的上游 output port（inputPort.FirstConnectedPort）
    public IPort GetConnectedOutputPort(IPort inputPort) { ... }
}
```

> **为什么 `OnEnable` 是 `public override`？**
> `Graph.OnEnable()` 在 DLL 中声明为 `public virtual`，子类必须用 `public override`，
> 不能用 `protected override`（否则编译报 CS0507）。

### EvaluateVectorPort 求值策略

```csharp
public Vector3 EvaluateVectorPort(IPort port)
{
    var node = FindNodeForPort(port);

    // 1. 用户计算节点 → 调用 EvaluateVector 触发计算
    if (node is FunctionContextNode fn)
        return fn.EvaluateVector(this);

    // 2. 图变量节点 → 读取变量默认值
    if (node is IVariableNode varNode)
    {
        varNode.Variable.TryGetDefaultValue(out Vector3 v);
        return v;
    }

    // 3. 用户常量节点 → 读取存储值
    if (node is IConstantNode constNode)
    {
        constNode.TryGetValue(out Vector3 v);
        return v;
    }

    // 4. 内置黄色常量节点（ConstantNodeModelImp）不在 GetNodes() 中，
    //    直接对其 output port 调用 TryGetValue 即可读取存储的常量值。
    port.TryGetValue(out Vector3 fallback);
    return fallback;
}
```

> **关键**：`FunctionContextNode` 必须排在第一位单独处理。它的 Result 是用户定义的 output port，框架不会自动写入计算值，必须显式调用 `EvaluateVector` 才能拿到结果。

### FunctionContextNode（函数上下文节点）

```csharp
[Node("Function", "")]
[UseWithGraph(typeof(ShaderFunctionGraph))]
[Serializable]
internal class FunctionContextNode : ContextNode
{
    private IPort m_Input;   // Vector3，接收外部输入
    private IPort m_Output;  // Vector3，输出计算结果

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        m_Input  = context.AddInputPort<Vector3>("Input").Build();
        m_Output = context.AddOutputPort<Vector3>("Result").Build();
    }

    public Vector3 EvaluateVector(ShaderFunctionGraph graph)
    {
        // INPUT 端口的 TryGetValue 只读内联常量，必须走 GetConnectedOutputPort
        Vector3 result = Vector3.zero;
        var upstream = graph.GetConnectedOutputPort(m_Input);
        if (upstream != null)
            result = graph.EvaluateVectorPort(upstream);
        else
            m_Input.TryGetValue<Vector3>(out result); // 无连线时读内联常量

        // 依次让每个 BlockNode 处理累积值（流水线模式）
        foreach (var block in BlockNodes)
        {
            if      (block is AddBlockNode add)        result = add.Apply(result);
            else if (block is MultiplyBlockNode mul)   result = mul.Apply(result);
            else if (block is CrossBlockNode cross)    result = cross.Apply(result);
            else if (block is NormalizeBlockNode norm) result = norm.Apply(result);
        }
        return result;
    }
}
```

### OperationBlockNode（运算块节点）

所有运算块遵循同一模式：`Apply(Vector3 accumulated)` 接收上一步的累积值，
端口参数通过 `TryGetValue` 读取内联常量（未连线时有效）。

```csharp
// 向量加法：accumulated + B
[Node("Add", "")][UseWithContext(typeof(FunctionContextNode))][Serializable]
internal class AddBlockNode : BlockNode
{
    private IPort m_B;
    protected override void OnDefinePorts(IPortDefinitionContext context)
        => m_B = context.AddInputPort<Vector3>("B").Build();
    public Vector3 Apply(Vector3 accumulated)
    {
        Vector3 b = Vector3.zero;
        m_B.TryGetValue<Vector3>(out b);
        return accumulated + b;
    }
}

// 标量乘法：accumulated * Factor
[Node("Multiply", "")][UseWithContext(typeof(FunctionContextNode))][Serializable]
internal class MultiplyBlockNode : BlockNode
{
    private IPort m_Factor;
    protected override void OnDefinePorts(IPortDefinitionContext context)
        => m_Factor = context.AddInputPort<float>("Factor").Build();
    public Vector3 Apply(Vector3 accumulated)
    {
        float f = 1f;
        m_Factor.TryGetValue<float>(out f);
        return accumulated * f;
    }
}

// 向量叉积：Cross(accumulated, B)
[Node("Cross", "")][UseWithContext(typeof(FunctionContextNode))][Serializable]
internal class CrossBlockNode : BlockNode { ... }

// 归一化：accumulated.normalized（无端口）
[Node("Normalize", "")][UseWithContext(typeof(FunctionContextNode))][Serializable]
internal class NormalizeBlockNode : BlockNode
{
    public Vector3 Apply(Vector3 accumulated) => accumulated.normalized;
}
```

> BlockNode 的 `Apply` 方法接收的 `accumulated`：
> - **第 1 个 BlockNode** → 收到的是 `FunctionContextNode.Input` 的值
> - **第 N 个 BlockNode** → 收到的是第 N-1 个块的输出

### OutputNode（输出节点）

`OutputNode` 是普通 `Node`（不是 BlockNode），放在图中接收 `FunctionContextNode.Result` 并打印。

```csharp
[Node("Output", "")]
[UseWithGraph(typeof(ShaderFunctionGraph))]
[Serializable]
internal class OutputNode : Node
{
    private IPort m_Input;

    protected override void OnDefinePorts(IPortDefinitionContext context)
        => m_Input = context.AddInputPort<Vector3>("Result").Build();

    public void Evaluate(ShaderFunctionGraph graph)
    {
        // 必须走 GetConnectedOutputPort，不能直接 TryGetValue（只读内联常量）
        Vector3 result = Vector3.zero;
        var upstream = graph.GetConnectedOutputPort(m_Input);
        if (upstream != null)
            result = graph.EvaluateVectorPort(upstream);
        else
            m_Input.TryGetValue<Vector3>(out result);
        Debug.Log($"[ShaderFunction] Result = {result}");
    }
}
```

### ShaderFunctionImporter

```csharp
[ScriptedImporter(1, "shaderfunc")]
internal class ShaderFunctionImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var graph = GraphDatabase.LoadGraphForImporter<ShaderFunctionGraph>(ctx.assetPath);

        var data = ScriptableObject.CreateInstance<ShaderFunctionData>();
        data.name = data.functionName = Path.GetFileNameWithoutExtension(ctx.assetPath);

        if (graph != null)
        {
            // 统计 BlockNode 数量（使用框架内置 BlockCount）
            var fnCtx = graph.FindFunctionContext();
            if (fnCtx != null)
                data.blockCount = fnCtx.BlockCount;

            // 触发求值并打印结果
            foreach (var node in graph.GetNodes())
            {
                if (node is OutputNode output)
                {
                    output.Evaluate(graph);
                    break;
                }
            }
        }

        ctx.AddObjectToAsset("main", data);
        ctx.SetMainObject(data);
        // 注意：Graph 不是 UnityEngine.Object，不能 AddObjectToAsset(graph)
    }
}
```

---

## 实践步骤

1. 右键 → **Create → Graph Toolkit → ShaderFunctionGraph**，命名为 `MyFunction`
2. 双击打开，图中已自动出现 `FunctionContextNode`
3. 在图中添加 **黄色常量节点**（右键 → Create Node → Constant），设置值，连接到 `FunctionContextNode.Input`
4. 在 `FunctionContextNode` 内添加 BlockNode（右键 → Operation）：
   - 依次添加 Cross、Add、Multiply、Normalize
   - 在每个块的 Input 端口填写常量值
5. 在图中添加 `OutputNode`，连接 `FunctionContextNode.Result → OutputNode.Result`
6. 保存，查看 Console 输出结果

---

## 常见问题

### Q: 为什么 `TryGetValue` 读出来是 `(0,0,0)`？

A: `IPort.TryGetValue<T>()` 在 **INPUT 端口** 上只读编辑器内联常量（`m_InputConstantsById`），
不会沿连线读上游节点的值。
- 如果端口有连线，必须用 `GetConnectedOutputPort(port)` 取到上游 output port，再调用 `EvaluateVectorPort`。
- 对于系统内置黄色常量节点（`ConstantNodeModelImp`）的 **output port**，直接调用 `TryGetValue` 则可以读到正确值。

### Q: `Graph.GetNodes()` 找不到 BlockNode？

A: 框架设计如此，`BlockNode` 不在主图节点列表中。
必须通过父 ContextNode 的 `BlockNodes` 属性访问。

### Q: BlockNode 用 `[UseWithGraph]` 还是 `[UseWithContext]`？

A: `BlockNode` 必须用 `[UseWithContext(typeof(MyContextNode))]`，否则它不会出现在对应 ContextNode 的添加菜单中。

### Q: `Graph.OnEnable()` 为何要写成 `public override`？

A: DLL 中 `Graph.OnEnable()` 声明为 `public virtual`，子类必须保持访问修饰符一致（`public override`），写成 `protected override` 会报 **CS0507**。

### Q: 为什么不能 `AddObjectToAsset("graph", graph)`？

A: `Graph` 不继承 `UnityEngine.Object`，无法作为 Unity 子资产附加到导入结果。需另建一个 `ScriptableObject`（如 `ShaderFunctionData`）作为主资产。

---

## API 速查

| 场景 | 正确写法 |
|------|---------|
| 定义块节点关联 | `[UseWithContext(typeof(FunctionContextNode))]` |
| 遍历子块 | `contextNode.BlockNodes` |
| 读取内联常量 | `inputPort.TryGetValue<T>(out v)` |
| 读取连线上游值 | `graph.GetConnectedOutputPort(inputPort)` → `EvaluateVectorPort` |
| 读取内置常量节点值 | `outputPort.TryGetValue<T>(out v)`（对 output port 有效） |
| 获取父上下文 | `blockNode.ContextNode`（框架内置） |
| 获取块数量 | `contextNode.BlockCount` |

---

## 总结

| 特性 | 实现方式 |
|------|---------|
| 函数容器 | `FunctionContextNode`（ContextNode），有 Input/Result 端口 |
| 运算步骤 | BlockNode 的 `Apply(accumulated)` 流水线，顺序累积 |
| 常量输入 | 系统内置黄色常量节点，无需自定义 |
| 连线求值 | `GetConnectedOutputPort + EvaluateVectorPort`（不能直接 TryGetValue） |
| 资产导出 | `ShaderFunctionData`（ScriptableObject），存 `functionName` + `blockCount` |

## 下一步

教程6将介绍**自定义 UI**，学习如何为节点创建自定义 Inspector 和图形编辑器界面。
