# 教程4: 变量和子图系统

## 概述

本教程将深入探讨GraphToolkit的高级特性：变量系统和子图系统。这些功能让你能够创建可复用的图形模块，实现参数化的图形，以及构建复杂的图形层次结构。

## 学习目标

- 理解GraphToolkit的变量系统
- 掌握IVariable接口的使用
- 学会VariableKind（Local、Input、Output）的区别
- 实现IVariableNode接口
- 掌握子图系统和ISubgraphNode接口
- 理解子图的参数传递机制
- 避免循环引用问题

## 核心概念

### 变量系统

GraphToolkit的变量系统类似于编程语言中的变量，可以在图形中存储和传递数据。

#### IVariable接口

```csharp
public interface IVariable
{
    string Name { get; set; }        // 变量名称
    Type Type { get; }                // 变量类型
    VariableKind Kind { get; }        // 变量种类
    object Value { get; set; }        // 变量值
    string Guid { get; }              // 唯一标识符
}
```

#### VariableKind枚举

```csharp
public enum VariableKind
{
    Local,   // 局部变量 - 仅在图形内部使用
    Input,   // 输入变量 - 从外部接收值（用于子图）
    Output   // 输出变量 - 向外部提供值（用于子图）
}
```

**使用场景**:

- **Local**: 图形内部的临时存储，类似于函数中的局部变量
- **Input**: 子图的参数，从父图接收值
- **Output**: 子图的返回值，向父图提供值

#### 创建变量

在图形中创建变量：

```csharp
// 在Unity编辑器的Blackboard面板中创建
// 或通过代码创建：
var variable = graph.AddVariable("MyColor", typeof(Color), VariableKind.Local);
variable.Value = Color.red;
```

### IVariableNode接口

变量节点用于在图形中访问变量的值。

```csharp
public interface IVariableNode
{
    IVariable Variable { get; set; }
}
```

**实现示例**:

```csharp
[Node("Variable", "Material")]
internal class VariableNode : Node, IVariableNode, IColorNode
{
    [SerializeField]
    private string m_VariableGuid;

    public IVariable Variable
    {
        get
        {
            // 通过GUID查找变量
            foreach (var variable in Graph.Variables)
            {
                if (variable.Guid == m_VariableGuid)
                    return variable;
            }
            return null;
        }
        set => m_VariableGuid = value?.Guid;
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        var variable = Variable;
        if (variable == null) return;

        // 根据变量种类创建端口
        if (variable.Kind == VariableKind.Input || variable.Kind == VariableKind.Local)
        {
            context.AddInputPort(variable.Type, "Value").Build();
        }
        if (variable.Kind == VariableKind.Output || variable.Kind == VariableKind.Local)
        {
            context.AddOutputPort(variable.Type, "Value").Build();
        }
    }

    public Color EvaluateColor(IPort port, MaterialGraph graph)
    {
        var variable = Variable;
        if (variable == null) return Color.white;

        // 如果有输入端口，先评估输入
        if (m_ValueInput != null)
        {
            var connectedPort = graph.GetConnectedOutputPort(m_ValueInput);
            if (connectedPort != null)
            {
                var color = graph.EvaluateColorPort(connectedPort);
                variable.Value = color; // 更新变量值
                return color;
            }
        }

        // 返回变量的当前值
        return (Color)variable.Value;
    }
}
```

**关键点**:
1. 使用GUID而不是直接引用，因为变量可能被删除或重命名
2. 根据VariableKind动态创建端口
3. 输入端口用于设置变量值
4. 输出端口用于读取变量值

---

### 子图系统

子图允许你将一个图形嵌入到另一个图形中，实现模块化和复用。

#### ISubgraphNode接口

```csharp
public interface ISubgraphNode
{
    Graph Subgraph { get; set; }
}
```

#### 子图的工作原理

```
父图（Parent Graph）
├─ [Input] → [Subgraph Node] → [Output]
                    ↓
            子图（Subgraph）
            ├─ Input Variables (接收父图的值)
            ├─ 内部处理
            └─ Output Variables (返回给父图)
```

**实现示例**:

```csharp
[Node("Subgraph", "Material")]
internal class SubgraphNode : Node, ISubgraphNode, IColorNode
{
    [SerializeField]
    private MaterialGraph m_Subgraph;

    public Graph Subgraph
    {
        get => m_Subgraph;
        set => m_Subgraph = value as MaterialGraph;
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        if (m_Subgraph == null) return;

        // 为子图的输入变量创建输入端口
        foreach (var variable in m_Subgraph.Variables)
        {
            if (variable.Kind == VariableKind.Input)
            {
                context.AddInputPort(variable.Type, variable.Name).Build();
            }
        }

        // 为子图的输出变量创建输出端口
        foreach (var variable in m_Subgraph.Variables)
        {
            if (variable.Kind == VariableKind.Output)
            {
                context.AddOutputPort(variable.Type, variable.Name).Build();
            }
        }
    }

    public Color EvaluateColor(IPort port, MaterialGraph graph)
    {
        if (m_Subgraph == null) return Color.white;

        // 1. 传递输入参数到子图
        foreach (var inputPort in Ports)
        {
            if (inputPort.Direction == PortDirection.Input)
            {
                var connectedPort = graph.GetConnectedOutputPort(inputPort);
                if (connectedPort != null)
                {
                    var variable = FindSubgraphVariable(inputPort.Name, VariableKind.Input);
                    if (variable != null)
                    {
                        variable.Value = graph.EvaluateColorPort(connectedPort);
                    }
                }
            }
        }

        // 2. 评估子图
        var result = m_Subgraph.FindOutputNode().EvaluateColor(null, m_Subgraph);

        // 3. 从子图获取输出
        return result;
    }

    protected override void OnValidate()
    {
        // 防止循环引用
        if (m_Subgraph == Graph)
        {
            Debug.LogError("Cannot reference self as subgraph!");
            m_Subgraph = null;
        }
    }
}
```

**关键点**:
1. 子图的Input变量 → 父图中的输入端口
2. 子图的Output变量 → 父图中的输出端口
3. 必须防止循环引用
4. 子图改变时需要重新定义端口

---

## 项目结构

```
04_VariablesSubgraphs/
├─ Editor/
│  ├── MaterialGraph.cs                # 材质图形
│  ├── MaterialGraphImporter.cs        # 资产导入器
│  ├── Nodes/
│  │   ├── IMaterialNode.cs            # 节点接口
│  │   ├── VariableNode.cs             # 变量节点（IVariableNode）
│  │   ├── SubgraphNode.cs             # 子图节点（ISubgraphNode）
│  │   ├── ColorConstantNode.cs        # 颜色常量（IConstantNode）
│  │   ├── FloatConstantNode.cs        # 浮点常量（IConstantNode）
│  │   ├── MixColorNode.cs             # 颜色混合节点
│  │   └── MaterialOutputNode.cs       # 材质输出节点
│  └── Unity.GraphToolkit.Tutorials.VariablesSubgraphs.Editor.asmdef
└─ Examples/
    └── (将在Unity编辑器中创建.matgraph文件)
```

## 节点详解

### VariableNode（变量节点）

访问图形变量的节点。

**特点**:
- 实现IVariableNode接口
- 根据变量类型和种类动态创建端口
- 支持读取和写入变量值

**端口配置**:
```
Local变量:
  [Input] → [Variable Node] → [Output]

Input变量:
  [Input] → [Variable Node]

Output变量:
  [Variable Node] → [Output]
```

### SubgraphNode（子图节点）

引用另一个图形作为子图。

**特点**:
- 实现ISubgraphNode接口
- 根据子图的Input/Output变量动态创建端口
- 自动传递参数
- 防止循环引用

**工作流程**:
1. 将父图的输入传递给子图的Input变量
2. 评估子图
3. 从子图的Output变量获取结果

### IConstantNode实现

常量节点实现IConstantNode接口：

```csharp
public interface IConstantNode
{
    object Value { get; set; }
}
```

这允许在编辑器中直接编辑常量值。

---

## 实践步骤

### 示例1: 使用局部变量

创建一个使用局部变量的简单图形：

**步骤**:
1. 创建.matgraph文件
2. 在Blackboard中添加局部变量：
   - 名称: "TempColor"
   - 类型: Color
   - 种类: Local
3. 添加节点：
   - Color常量节点（红色）
   - Variable节点（关联到TempColor）
   - Material Output节点
4. 连接：
   - Color → Variable(Input)
   - Variable(Output) → Material Output

**结果**: 颜色通过变量传递到输出

### 示例2: 创建可复用的颜色混合子图

**步骤A: 创建子图**
1. 创建ColorMix.matgraph
2. 添加Input变量：
   - "ColorA" (Color)
   - "ColorB" (Color)
   - "MixFactor" (float)
3. 添加Output变量：
   - "Result" (Color)
4. 构建图形：
   ```
   [Variable: ColorA] ─┐
                       ├─> [Mix Color] → [Variable: Result] → [Output]
   [Variable: ColorB] ─┤        ↑
                       │        │
   [Variable: MixFactor] ───────┘
   ```

**步骤B: 使用子图**
1. 创建MyMaterial.matgraph
2. 添加节点：
   - 2个Color常量
   - 1个Float常量
   - 1个Subgraph节点（引用ColorMix.matgraph）
   - 1个Material Output
3. 连接：
   ```
   [Color: Red] ────────> [Subgraph.ColorA]
   [Color: Blue] ───────> [Subgraph.ColorB]
   [Float: 0.5] ────────> [Subgraph.MixFactor]
   [Subgraph.Result] ──> [Material Output]
   ```

**结果**: 子图被复用，输出混合后的颜色

### 示例3: 嵌套子图

创建更复杂的层次结构：

```
MainGraph
├─ Subgraph A
│  └─ Subgraph B
│     └─ 基础处理
└─ Output
```

**注意**: 必须避免循环引用（A引用B，B又引用A）

---

## 变量系统最佳实践

### 1. 变量命名规范

```csharp
// ✅ 好的命名
"BaseColor"
"MixFactor"
"OutputResult"

// ❌ 避免的命名
"var1"
"temp"
"x"
```

### 2. 选择正确的VariableKind

```csharp
// Local - 图形内部使用
var tempColor = graph.AddVariable("TempColor", typeof(Color), VariableKind.Local);

// Input - 子图参数
var inputColor = graph.AddVariable("InputColor", typeof(Color), VariableKind.Input);

// Output - 子图返回值
var outputColor = graph.AddVariable("OutputColor", typeof(Color), VariableKind.Output);
```

### 3. 变量初始化

```csharp
// 始终为变量设置默认值
variable.Value = Color.white;  // Color类型
variable.Value = 0f;           // float类型
variable.Value = Vector3.zero; // Vector3类型
```

### 4. 类型安全

```csharp
// 评估前检查类型
public Color EvaluateColor(IPort port, MaterialGraph graph)
{
    var variable = Variable;
    if (variable == null || variable.Type != typeof(Color))
        return Color.white;

    if (variable.Value is Color colorValue)
        return colorValue;

    return Color.white;
}
```

---

## 子图系统最佳实践

### 1. 防止循环引用

```csharp
protected override void OnValidate()
{
    base.OnValidate();

    // 检查直接循环引用
    if (m_Subgraph == Graph)
    {
        Debug.LogError("Cannot reference self!");
        m_Subgraph = null;
        return;
    }

    // 检查间接循环引用（可选，更复杂）
    if (HasCircularReference(m_Subgraph, Graph))
    {
        Debug.LogError("Circular reference detected!");
        m_Subgraph = null;
    }
}

private bool HasCircularReference(Graph subgraph, Graph targetGraph)
{
    if (subgraph == null) return false;

    foreach (var node in subgraph.Nodes)
    {
        if (node is ISubgraphNode subgraphNode)
        {
            if (subgraphNode.Subgraph == targetGraph)
                return true;

            if (HasCircularReference(subgraphNode.Subgraph, targetGraph))
                return true;
        }
    }
    return false;
}
```

### 2. 子图版本管理

```csharp
// 当子图的接口（Input/Output变量）改变时
// 需要通知父图重新定义端口
protected override void OnDefineOptions(IOptionDefinitionContext context)
{
    context.AddOption("Subgraph",
        () => m_Subgraph,
        v => {
            m_Subgraph = v;
            Graph?.OnGraphChanged(); // 触发重新定义端口
        }
    ).Build();
}
```

### 3. 子图参数传递

```csharp
// 确保所有Input变量都被赋值
public Color EvaluateColor(IPort port, MaterialGraph graph)
{
    if (m_Subgraph == null) return Color.white;

    // 传递所有输入参数
    foreach (var variable in m_Subgraph.Variables)
    {
        if (variable.Kind == VariableKind.Input)
        {
            var inputPort = FindPort(variable.Name, PortDirection.Input);
            if (inputPort != null)
            {
                var connectedPort = graph.GetConnectedOutputPort(inputPort);
                if (connectedPort != null)
                {
                    // 根据类型评估
                    if (variable.Type == typeof(Color))
                        variable.Value = graph.EvaluateColorPort(connectedPort);
                    else if (variable.Type == typeof(float))
                        variable.Value = graph.EvaluateFloatPort(connectedPort);
                }
                else
                {
                    // 使用默认值
                    Debug.LogWarning($"Input '{variable.Name}' not connected, using default value");
                }
            }
        }
    }

    // 评估子图...
}
```

### 4. 子图缓存

```csharp
// 对于复杂的子图，可以缓存评估结果
private Dictionary<string, object> m_SubgraphCache = new Dictionary<string, object>();

public Color EvaluateColor(IPort port, MaterialGraph graph)
{
    // 生成缓存键
    string cacheKey = GenerateCacheKey();

    if (m_SubgraphCache.TryGetValue(cacheKey, out var cached))
    {
        return (Color)cached;
    }

    // 评估子图
    var result = EvaluateSubgraph();

    // 缓存结果
    m_SubgraphCache[cacheKey] = result;

    return result;
}
```

---

## 高级话题

### 1. 黑板系统（Blackboard）

GraphToolkit提供了Blackboard UI来管理变量：

- 在图形编辑器中打开Blackboard面板
- 添加/删除/重命名变量
- 设置变量类型和种类
- 设置默认值

### 2. 变量作用域

```
Graph
├─ Local Variables (仅在当前图形中可见)
├─ Input Variables (从父图接收)
└─ Output Variables (返回给父图)

Subgraph Node
├─ 为每个Input Variable创建输入端口
└─ 为每个Output Variable创建输出端口
```

### 3. 子图的递归评估

子图可以嵌套，评估时会递归处理：

```
MainGraph.Evaluate()
└─> SubgraphA.Evaluate()
    └─> SubgraphB.Evaluate()
        └─> 基础节点评估
```

### 4. 性能优化

**变量访问优化**:
```csharp
// 缓存变量引用
private IVariable m_CachedVariable;

public IVariable Variable
{
    get
    {
        if (m_CachedVariable == null || m_CachedVariable.Guid != m_VariableGuid)
        {
            m_CachedVariable = FindVariable(m_VariableGuid);
        }
        return m_CachedVariable;
    }
}
```

**子图评估优化**:
```csharp
// 只在输入改变时重新评估
private int m_LastInputHash;

public Color EvaluateColor(IPort port, MaterialGraph graph)
{
    int currentHash = CalculateInputHash();

    if (currentHash == m_LastInputHash && m_CachedResult != null)
    {
        return m_CachedResult;
    }

    m_LastInputHash = currentHash;
    m_CachedResult = EvaluateSubgraph();
    return m_CachedResult;
}
```

---

## 练习题

### 练习1: 创建参数化的渐变子图

创建一个子图，接受两个颜色和一个混合因子，返回渐变颜色。

**要求**:
- 3个Input变量（ColorA, ColorB, Factor）
- 1个Output变量（Result）
- 使用MixColorNode实现

### 练习2: 实现变量节点的Set/Get模式

扩展VariableNode，添加模式选择：
- Get模式：只有输出端口
- Set模式：只有输入端口
- GetSet模式：同时有输入和输出端口

### 练习3: 创建材质库系统

使用子图创建一个材质库：
- BaseColor子图
- MetallicRoughness子图
- Normal子图
- 主材质图形组合这些子图

---

## 常见问题

### Q: 变量和端口有什么区别？

A:
- **端口**：节点之间的连接点，用于传递数据
- **变量**：图形级别的数据存储，可以被多个节点访问

### Q: 什么时候使用Local变量？

A: 当你需要在图形中多个地方使用同一个值，但不想通过端口连接时。类似于编程中的局部变量。

### Q: 子图的Input/Output变量必须都使用吗？

A: 不必须。未连接的Input变量会使用默认值，未使用的Output变量会被忽略。

### Q: 如何调试子图？

A:
1. 在子图中添加Debug节点输出中间值
2. 使用变量节点查看变量的当前值
3. 在代码中添加Debug.Log

### Q: 子图可以嵌套多少层？

A: 理论上没有限制，但过深的嵌套会影响性能和可维护性。建议不超过3-4层。

### Q: 如何处理子图版本更新？

A:
1. 保持Input/Output变量的向后兼容
2. 添加新变量时提供默认值
3. 删除变量前检查是否有父图在使用

---

## 总结

本教程介绍了GraphToolkit的两个高级特性：

1. **变量系统**
   - IVariable接口
   - VariableKind（Local, Input, Output）
   - IVariableNode实现
   - 黑板管理

2. **子图系统**
   - ISubgraphNode接口
   - 参数传递机制
   - 循环引用防止
   - 模块化和复用

这些特性让你能够创建更复杂、更可维护的图形系统。

## 下一步

在下一个教程中，我们将学习ContextNode和BlockNode，它们提供了更高级的节点组织方式，类似于Shader Graph中的函数节点。

---

## 参考资源

- [IVariable API文档](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Unity.GraphToolkit.Editor.IVariable.html)
- [IVariableNode接口](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Unity.GraphToolkit.Editor.IVariableNode.html)
- [ISubgraphNode接口](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Unity.GraphToolkit.Editor.ISubgraphNode.html)
- [IConstantNode接口](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Unity.GraphToolkit.Editor.IConstantNode.html)
