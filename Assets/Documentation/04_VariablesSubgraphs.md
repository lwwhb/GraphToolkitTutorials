# 教程4: 变量与子图 — URP 材质生成器

## 概述

本教程将 GraphToolkit 的**变量系统**和**子图系统**应用于一个实际场景：构建一个基于图形的 **URP 材质生成器**。`.matgraph` 文件导入后会自动生成真实可用的 `Material` 资源，可以直接拖到场景中的 Mesh 使用。

这是一个**数据流图（Pull 模式）**，与教程2类似，求值在 `ScriptedImporter` 中完成，无需运行时代码。

## 学习目标

- 理解 `IVariable` 接口及 `VariableKind`（Local、Input、Output）的用途
- 掌握 `ISubgraphNode` 接口，将子图作为 **PBR 参数包** 封装复用
- 区分两种子图类型：**常量子图**（只有 Output 变量）和**变量子图**（有 Input + Output 变量）
- 学会在 `ScriptedImporter` 中生成真实 `Material`（URP/Lit Shader）
- 理解框架自动生成常量节点与变量节点的机制，避免重复造轮子

---

## 核心概念

### 变量系统

GraphToolkit 的变量在**图形面板的 Blackboard** 中创建，按名称和类型定义。框架会**自动生成**对应的变量节点——无需手动编写 `VariableNode` 类。修改一次 Blackboard 中的变量默认值，所有引用它的节点同步更新。

#### VariableKind

| 种类 | 含义 | 典型用途 |
|------|------|----------|
| `Local` | 图形内部变量 | 在同一图中多处复用同一颜色/数值 |
| `Input` | 子图参数（从父图注入） | 子图对外暴露的输入接口，成为父图子图节点的输入端口 |
| `Output` | 子图返回值（传给父图） | 子图对外暴露的输出接口，成为父图子图节点的输出端口 |

> **⚠️ 重要**：创建 `Input` 或 `Output` 类型的变量，要求图形的 `[Graph]` 特性使用 `GraphOptions.SupportsSubgraphs`。使用 `GraphOptions.Default` 时，框架会将 Input/Output 降级为 Local 并输出警告。

#### IVariable 接口（实际可用 API）

```csharp
// GraphToolkit 实际提供的属性/方法（Unity 6000.5.0a8）
variable.Name           // string — 变量名称
variable.DataType       // Type   — 变量数据类型（typeof(Color) 等）
variable.VariableKind   // VariableKind — Local / Input / Output

variable.TryGetDefaultValue(out T value)   // 读取默认值
variable.TrySetDefaultValue(T value)       // 写入默认值

// ⚠️ 注意：IVariable 没有 .Guid 属性，也没有 .Value 属性
// 用 variable.Name 唯一标识变量
```

#### 框架自动生成的常量节点与变量节点

对于 `Color`、`float` 等 Unity 已知类型，框架会自动生成：
- **常量节点**（实现 `IConstantNode`）：在 Inspector 中直接设置值，无需连接
- **变量节点**（实现 `IVariableNode`）：从 Blackboard 读取变量值

`MaterialGraph` 的评估方法已处理这两种接口：

```csharp
// EvaluateColorPort 中自动处理框架生成的常量/变量节点
if (node is IConstantNode constantNode)
{
    Color color = Color.white;
    constantNode.TryGetValue(out color);   // 读取常量值
    return color;
}
if (node is IVariableNode variableNode)
{
    Color varColor = Color.white;
    variableNode.Variable?.TryGetDefaultValue(out varColor);   // 读取变量默认值
    return varColor;
}
```

---

### 子图系统：PBR 参数包

本教程将 PBR 材质参数（BaseColor、Metallic、Smoothness、EmissionColor）封装在独立的子图文件中，主图（`.matgraph`）通过框架自动生成的子图节点引用它们。

#### 两种子图类型

本教程提供两个专用子图类型，分别演示不同的变量用法：

| 类型 | 扩展名 | 变量种类 | 教学重点 |
|------|--------|---------|---------|
| `MaterialConstSubgraph` | `.matconstsubgraph` | 仅 Output | Output 变量如何成为父图子图节点的输出端口 |
| `MaterialVariableSubgraph` | `.matvarsubgraph` | Input + Output | Input 变量如何成为输入端口，由父图或 Node Properties 注入值 |

两者均继承自 `MaterialGraph`，且都使用 `GraphOptions.SupportsSubgraphs`：

```csharp
// 常量子图 — 只有 Output 变量
[Graph("matconstsubgraph", GraphOptions.SupportsSubgraphs)]
[Subgraph(typeof(MaterialGraph))]
[Serializable]
public class MaterialConstSubgraph : MaterialGraph { ... }

// 变量子图 — 有 Input + Output 变量
[Graph("matvarsubgraph", GraphOptions.SupportsSubgraphs)]
[Subgraph(typeof(MaterialGraph))]
[Serializable]
public class MaterialVariableSubgraph : MaterialGraph { ... }
```

#### 框架自动生成子图节点的条件

**不需要手动编写 `SubgraphNode` 类。** 满足以下两个条件后，框架自动处理：

1. 父图声明 `GraphOptions.SupportsSubgraphs` — 启用子图节点自动生成
2. 子图类声明 `[Subgraph(typeof(MaterialGraph))]` — 声明该子图可被 `MaterialGraph` 引用

#### 工作原理

```
VarSubgraph.matvarsubgraph（变量子图）
  Input  变量: Tint (Color) → 成为子图节点的输入端口（可由父图注入，或在 Node Properties 设默认值）
  Output 变量: BaseColor, Metallic, Smoothness, EmissionColor → 成为子图节点的输出端口
       ↓
框架自动生成的子图节点（实现 ISubgraphNode，出现在 MaterialGraph.matgraph 中）
  输入端口:  Tint ←─ 父图中其他节点（可选）或 Node Properties 默认值
  输出端口:  BaseColor ─→ MaterialOutputNode.BaseColor
            Metallic  ─→ MaterialOutputNode.Metallic
            Smoothness ─→ MaterialOutputNode.Smoothness
            EmissionColor ─→ MaterialOutputNode.Emission Color
```

#### IPort.Name 与 IPort.DisplayName 的区别

框架自动生成的子图节点的端口，`Name` 存储的是变量的 **GUID 字符串**，`DisplayName` 才是变量名称。按名称查找端口时必须使用 `DisplayName`：

```csharp
// ❌ 错误：IPort.Name 是 GUID，不是变量名
var port = ports.FirstOrDefault(p => p.Name == "BaseColor");

// ✅ 正确：用 DisplayName 匹配
private static IPort GetPortByDisplayName(IEnumerable<IPort> ports, string displayName)
{
    foreach (var p in ports)
        if (p.DisplayName == displayName) return p;
    return null;
}
```

#### MaterialGraph 中的子图求值逻辑

框架生成的子图节点实现 `ISubgraphNode`，`MaterialGraph` 在 `EvaluateColorPort` / `EvaluateFloatPort` 中检测到该接口后，委托给专门的子图求值方法：

```csharp
// EvaluateColorPort 中的子图分支
if (node is ISubgraphNode subgraphNode)
    return EvaluateSubgraphColorPort(node, subgraphNode, port);

private Color EvaluateSubgraphColorPort(INode subgraphNodeAsINode,
    ISubgraphNode subgraphNode, IPort outputPort)
{
    // GetSubgraph() 获取子图实例，as MaterialGraph 统一处理两种子图类型
    var subgraph = subgraphNode.GetSubgraph() as MaterialGraph;
    if (subgraph == null) return Color.white;

    // 1. 注入 Input 变量值（有连线取上游计算值，无连线取 Node Properties 默认值）
    InjectSubgraphInputs(subgraphNodeAsINode, subgraph);

    // 2. 按 DisplayName 找到子图中对应的 Output IVariableNode，触发子图内部求值链
    foreach (var node in subgraph.GetNodes())
    {
        if (node is IVariableNode vn
            && vn.Variable?.Name == outputPort.DisplayName   // DisplayName = 变量名
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

// Input 变量注入：有连线取上游值，无连线取 Node Properties 面板中设置的默认值
private void InjectSubgraphInputs(INode subgraphNodeAsINode, MaterialGraph subgraph)
{
    foreach (var inputPort in subgraphNodeAsINode.GetInputPorts())
    {
        var connected = GetConnectedOutputPort(inputPort);

        foreach (var variable in subgraph.GetVariables())
        {
            if (variable.Name != inputPort.DisplayName || variable.VariableKind != VariableKind.Input)
                continue;

            if (variable.DataType == typeof(Color))
            {
                Color value = Color.white;
                if (connected != null) value = EvaluateColorPort(connected);
                else inputPort.TryGetValue(out value);   // 读取 Node Properties 默认值
                variable.TrySetDefaultValue(value);
            }
            else if (variable.DataType == typeof(float))
            {
                float value = 0f;
                if (connected != null) value = EvaluateFloatPort(connected);
                else inputPort.TryGetValue(out value);
                variable.TrySetDefaultValue(value);
            }
            break;
        }
    }
}
```

---

### MaterialOutputNode：输出真实 Material

`MaterialOutputNode` 是图形的终点，持有 5 个输入端口和对应的缓存字段。`MaterialGraph.CreateMaterial()` 调用 `EvaluateAll()` 触发整图求值后用 URP/Lit Shader 创建 `Material`。

```csharp
internal class MaterialOutputNode : Node
{
    // 5 个输入端口
    // Base Color(Color), Metallic(float), Smoothness(float),
    // Emission Color(Color), Emission Intensity(float, default 0)

    // 2 个 INodeOption（Metallic/Smoothness 的默认值，当端口未连接时使用）
    private INodeOption m_DefaultMetallic;
    private INodeOption m_DefaultSmoothness;

    public void EvaluateAll(MaterialGraph graph) { /* 求值并缓存所有参数 */ }

    public Color GetBaseColor()         { ... }
    public float GetMetallic()          { ... }
    public float GetSmoothness()        { ... }
    public Color GetEmission()          { ... }
    public float GetEmissionIntensity() { ... }
}
```

### MaterialGraph.CreateMaterial()

```csharp
public Material CreateMaterial()
{
    var outputNode = FindOutputNode();
    if (outputNode == null) return null;

    outputNode.EvaluateAll(this);     // 触发整图求值

    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
    mat.SetColor("_BaseColor",  outputNode.GetBaseColor());
    mat.SetFloat("_Metallic",   Mathf.Clamp01(outputNode.GetMetallic()));
    mat.SetFloat("_Smoothness", Mathf.Clamp01(outputNode.GetSmoothness()));

    float intensity = outputNode.GetEmissionIntensity();
    if (intensity > 0f)
    {
        mat.SetColor("_EmissionColor", outputNode.GetEmission() * intensity);
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
    }
    return mat;
}
```

---

## 项目结构

```
04_VariablesSubgraphs/
└─ Editor/
   ├── MaterialGraph.cs                    # [Graph("matgraph", SupportsSubgraphs)] + CreateMaterial() + 子图求值逻辑
   ├── MaterialGraphImporter.cs            # 导入 .matgraph → 生成 Material 资源
   ├── MaterialConstSubgraph.cs            # [Graph("matconstsubgraph")] 常量子图，仅 Output 变量
   ├── MaterialConstSubgraphAsset.cs       # 常量子图的占位资产对象
   ├── MaterialConstSubgraphImporter.cs    # 导入 .matconstsubgraph
   ├── MaterialVariableSubgraph.cs         # [Graph("matvarsubgraph")] 变量子图，Input + Output 变量
   ├── MaterialVariableSubgraphAsset.cs    # 变量子图的占位资产对象
   ├── MaterialVariableSubgraphImporter.cs # 导入 .matvarsubgraph
   └── Nodes/
       ├── IMaterialNode.cs                # IColorNode、IFloatNode 接口（自定义节点分发用）
       ├── MixColorNode.cs                 # Color.Lerp(A, B, Factor)
       ├── MultiplyFloatNode.cs            # A × B（数值调整）
       └── MaterialOutputNode.cs           # 终点节点，EvaluateAll() + GetXxx()
```

> **注意**：`ColorConstantNode`、`FloatConstantNode`、`VariableNode` 均已移除——框架会为 `Color`/`float` 等已知类型自动生成常量节点（`IConstantNode`）和变量节点（`IVariableNode`），无需手动实现。子图节点同理，由 `[Subgraph]` + `SupportsSubgraphs` 自动生成。

---

## 节点详解

### 常量节点与变量节点（框架自动生成）

框架为 `Color`、`float` 等已知 Unity 类型自动生成：
- **常量节点**（实现 `IConstantNode`）：在 Inspector 字段中直接输入数值或颜色
- **变量节点**（实现 `IVariableNode`）：从 Blackboard 读取变量值，修改 Blackboard 默认值，所有引用节点同步更新

无需手动编写这些节点类，在图形编辑器的节点创建菜单中直接使用即可。

### MixColorNode

```
[Color A] ─┐
           ├─→ Color.Lerp(A, B, Factor) ─→ [Result]
[Color B] ─┘
[Factor: float, default 0.5]
```

Factor 端口使用 `.WithDefaultValue(0.5f)` + `TryGetValue()` 回退：

```csharp
m_Factor = context.AddInputPort<float>("Factor").WithDefaultValue(0.5f).Build();

// 未连接时：
float factor = 0.5f;
m_Factor.TryGetValue(out factor);
```

### MultiplyFloatNode

```
[A: float, default 1] ─┐
                       ├─→ A × B ─→ [Result]
[B: float, default 1] ─┘
```

用于在主图中对子图输出的 PBR 参数进行缩放，例如：
- `SubgraphNode.Metallic(0.9) × FloatConstant(0.8) = 0.72`（子图输出金属度 × 系数，模拟磨损效果）

### 子图节点（框架自动生成）

**子图要求**：被引用的子图文件必须定义至少一个 Output 变量（否则节点无输出端口）。Input 变量可选，有则生成对应输入端口，可由父图连线注入值，也可在 Node Properties 面板直接设置默认值。

**两种子图的端口结构**：

| 子图类型 | 输入端口 | 输出端口 |
|---------|---------|---------|
| `MaterialConstSubgraph` | 无 | BaseColor, Metallic, Smoothness, EmissionColor |
| `MaterialVariableSubgraph` | Tint (Color) | BaseColor, Metallic, Smoothness, EmissionColor |

**求值顺序**：
1. 主图求值到子图节点的某个输出端口
2. `MaterialGraph.EvaluateColorPort/FloatPort` 检测到 `ISubgraphNode`，委托给 `EvaluateSubgraphColorPort/FloatPort`
3. 调用 `InjectSubgraphInputs`：有连线取上游计算值，无连线取 Node Properties 中的默认值
4. 按 `outputPort.DisplayName` 在子图中找到对应名称的 Output `IVariableNode`
5. 追溯其上游连接递归求值；若无连接则读取变量默认值

### MaterialOutputNode

**URP/Lit Shader 属性对照**：

| 输入端口 | URP Shader 属性 | 说明 |
|---------|----------------|------|
| Base Color | `_BaseColor` | PBR 基础颜色 |
| Metallic | `_Metallic` | 金属度 0–1，未连接时用 INodeOption 默认值 |
| Smoothness | `_Smoothness` | 光滑度 0–1，未连接时用 INodeOption 默认值 |
| Emission Color | `_EmissionColor` | 自发光颜色 |
| Emission Intensity | （乘以 EmissionColor）| 强度 > 0 时启用 `_EMISSION` 关键字 |

---

## API 迁移注意事项（Unity 6000.5.0a8）

| 旧写法（已失效） | 新写法 |
|----------------|--------|
| `GraphOptions.None` | `GraphOptions.Default` |
| `internal class MyGraph` | `public class MyGraph` |
| `context.AddOption("X", getter, setter)` | `INodeOption opt = context.AddOption<T>("X").Build()` |
| `opt.TryGetValue(out val)` | 同上，API 未变 |
| `graph.Variables` | `graph.GetVariables()` |
| `node.Ports` | `node.GetInputPorts()` / `node.GetOutputPorts()` |
| `variable.Guid` | ❌ 不存在，用 `variable.Name` |
| `variable.Value` | ❌ 不存在，用 `TryGetDefaultValue` / `TrySetDefaultValue` |
| `ctx.AddObjectToAsset("graph", graph)` | ❌ Graph 非 UnityEngine.Object，不可添加为子资产 |
| 手动编写 `SubgraphNode : Node, ISubgraphNode` | ❌ 改用 `[Subgraph(typeof(ParentGraph))]` + `GraphOptions.SupportsSubgraphs` |
| `subgraphNode.Subgraph` | `subgraphNode.GetSubgraph()` |
| `inputPort.Name` 匹配变量名 | ❌ `Name` 是 GUID，改用 `inputPort.DisplayName` |
| `GraphOptions.Default` 用于子图（含 Input/Output 变量） | `GraphOptions.SupportsSubgraphs`（否则 Input/Output 变量被降级为 Local） |

---

## 实践步骤

### 步骤1：创建常量子图（ConstSubgraph）

1. 右键 → **Create → Graph Toolkit → Material Const Subgraph**，命名为 `ConstSubgraph`
2. Unity 导入时自动初始化：创建 4 个 Output 变量 + 常量节点连线：
   ```
   ColorConstant(#FFD700 金色) ──→ VariableNode[Output: BaseColor]
   FloatConstant(0.9)          ──→ VariableNode[Output: Metallic]
   FloatConstant(0.7)          ──→ VariableNode[Output: Smoothness]
   ColorConstant(黑色)          ──→ VariableNode[Output: EmissionColor]
   ```
3. 打开图形验证结构，可修改常量节点的值调整材质参数

### 步骤2：创建变量子图（VarSubgraph）

1. 右键 → **Create → Graph Toolkit → Material Variable Subgraph**，命名为 `VarSubgraph`
2. Unity 导入时自动初始化：创建 1 个 Input 变量 + 4 个 Output 变量 + 连线：
   ```
   Input  变量: Tint (Color, 默认白色) → 子图节点上生成输入端口
   Output 变量: BaseColor, Metallic, Smoothness, EmissionColor

   VariableNode[Input: Tint] ──→ VariableNode[Output: BaseColor]
   FloatConstant(0.9)         ──→ VariableNode[Output: Metallic]
   FloatConstant(0.7)         ──→ VariableNode[Output: Smoothness]
   ColorConstant(黑色)         ──→ VariableNode[Output: EmissionColor]
   ```
3. 打开图形，在 Blackboard 中可看到 Tint（Input）和 4 个 Output 变量

### 步骤3：创建主图（MaterialGraph）

1. 右键 → **Create → Graph Toolkit → MaterialGraph**
2. Unity 导入时自动初始化：找到同目录下的 `.matvarsubgraph` 或 `.matconstsubgraph`，自动添加子图节点并连线到 `MaterialOutputNode`
3. 打开图形可以看到子图节点的输入端口（Tint）和输出端口（BaseColor 等）均已连接

### 步骤4：验证两种注入方式

**方式A — Node Properties 默认值（无连线）**：
1. 打开主图，选中 `VarSubgraph` 子图节点
2. 在 Node Properties / Inspector 面板中修改 `Tint` 颜色
3. 保存图形 → `MaterialGraph.matgraph` 重新导入，观察生成的 Material 颜色变化

**方式B — 父图连线注入**：
1. 在主图 Blackboard 添加 **Local** 变量 `TintOverride`（Color）
2. 添加该变量的 VariableNode，将其输出端口连接到子图节点的 `Tint` 输入端口
3. 修改 `TintOverride` 变量默认值，保存，观察 Material 颜色变化

---

## 常见问题

### Q: 创建 Input/Output 变量时出现 "exposed scope" 警告，变量被降级为 Local？

A: 子图类的 `[Graph]` 特性必须使用 `GraphOptions.SupportsSubgraphs`，`GraphOptions.Default` 不支持 Input/Output 变量。检查 `MaterialConstSubgraph` 和 `MaterialVariableSubgraph` 的特性声明。

### Q: 在 Node Properties 修改 Tint 颜色后 Material 没有变化？

A: 确认 `InjectSubgraphInputs` 在无连线时调用了 `inputPort.TryGetValue(out value)`。旧版实现遇到 `connected == null` 时直接跳过，导致默认值不被注入。

### Q: 保存图形后 Material 没有更新？

A: 在 Project 面板右键 `.matgraph` 文件 → **Reimport**，或修改文件触发自动重新导入。

### Q: 子图节点没有输出端口？

A: 检查被引用的子图是否定义了 **Output** 类型的变量（VariableKind.Output）。框架根据子图的 Output 变量动态生成输出端口，没有 Output 变量时节点不会有可连接的输出端口。

### Q: Emission 没有生效？

A: 确保 `Emission Intensity` 端口连接的值 > 0（默认值为 0，自发光关闭）。将 FloatConstantNode 设为 1.0 以上再测试。

### Q: 能否用 Lit 以外的 Shader？

A: `MaterialGraphImporter` 中修改 `Shader.Find(...)` 参数即可，如 `"Universal Render Pipeline/Unlit"` 或自定义 Shader 路径。

---

## 总结

| 特性 | 本教程的实现 |
|------|-------------|
| **变量系统** | `VariableKind.Input/Output` 需要 `GraphOptions.SupportsSubgraphs`；`IPort.DisplayName` 是变量名，`Name` 是 GUID |
| **常量子图** | `MaterialConstSubgraph`（`.matconstsubgraph`）仅有 Output 变量，封装固定 PBR 参数包 |
| **变量子图** | `MaterialVariableSubgraph`（`.matvarsubgraph`）有 Input + Output 变量，支持父图注入或 Node Properties 设默认值 |
| **Input 注入** | `InjectSubgraphInputs`：有连线取上游求值；无连线取 `inputPort.TryGetValue`（Node Properties 值） |
| **图形输出** | `MaterialOutputNode.EvaluateAll()` + `MaterialGraph.CreateMaterial()` → 真实 URP Material |

## 下一步

教程5将介绍 **ContextBlock 系统**，学习如何用 Block 节点组织 Shader 函数的输入/输出，构建类似 Shader Graph 中 Fragment/Vertex 上下文的结构。

---

## 参考资源

- [IVariable 接口](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Unity.GraphToolkit.Editor.IVariable.html)
- [IVariableNode 接口](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Unity.GraphToolkit.Editor.IVariableNode.html)
- [ISubgraphNode 接口](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Unity.GraphToolkit.Editor.ISubgraphNode.html)
- [URP Lit Shader 属性参考](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html)
