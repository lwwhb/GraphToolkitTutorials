# 教程5: ContextNode和BlockNode

## 概述

本教程将探讨GraphToolkit中最高级的节点类型：ContextNode和BlockNode。这些特殊节点允许你创建具有作用域和嵌套结构的图形，类似于Shader Graph中的自定义函数节点。

## 学习目标

- 理解ContextNode和BlockNode的概念
- 掌握ContextNode作为容器的使用
- 学会BlockNode的特殊性和限制
- 实现作用域和嵌套结构
- 创建类似Shader Graph的函数节点

## 核心概念

### ContextNode vs 普通Node

#### 普通Node
```csharp
// 普通节点 - 独立存在
[Node("Add", "Math")]
internal class AddNode : Node
{
    // 可以在图形的任何地方创建
}
```

#### ContextNode
```csharp
// 上下文节点 - 可以包含BlockNode
[Node("Function", "Shader")]
internal class FunctionContextNode : ContextNode
{
    // 可以包含BlockNode作为子节点
    // 提供作用域和封装
}
```

**关键区别**:
- **普通Node**: 独立的处理单元
- **ContextNode**: 节点容器，定义作用域

### BlockNode的特殊性

BlockNode是只能存在于ContextNode内部的特殊节点。

```csharp
// BlockNode - 只能在ContextNode内部
[Node("Input A", "Shader/Block")]
internal class InputABlockNode : BlockNode
{
    // 必须有一个父ContextNode
    // 不能独立存在于图形中
}
```

**BlockNode的限制**:
1. 必须属于某个ContextNode
2. 不能在ContextNode外部创建
3. 不能在不同的ContextNode之间移动
4. 删除ContextNode时，其所有BlockNode也会被删除

### 架构模式

```
ShaderFunctionGraph
├─ FunctionContextNode (ContextNode)
│  ├─ InputABlockNode (BlockNode)
│  ├─ InputBBlockNode (BlockNode)
│  ├─ AddBlockNode (BlockNode)
│  ├─ MultiplyBlockNode (BlockNode)
│  └─ OutputBlockNode (BlockNode)
├─ Vector3Node (普通Node)
└─ FloatNode (普通Node)
```

**工作流程**:
1. ContextNode定义函数边界
2. InputBlockNode接收外部输入
3. OperationBlockNode执行内部计算
4. OutputBlockNode返回结果
5. 普通Node可以连接到ContextNode

---

## 项目结构

```
05_ContextBlocks/
├─ Editor/
│  ├── ShaderFunctionGraph.cs          # 着色器函数图形
│  ├── ShaderFunctionImporter.cs       # 资产导入器
│  ├── Nodes/
│  │   ├── IShaderNode.cs              # 节点接口
│  │   ├── FunctionContextNode.cs      # 函数上下文节点（ContextNode）
│  │   ├── InputBlockNode.cs           # 输入块节点（BlockNode）
│  │   ├── OutputBlockNode.cs          # 输出块节点（BlockNode）
│  │   ├── OperationBlockNode.cs       # 操作块节点（BlockNode）
│  │   └── ConstantNode.cs             # 常量节点（普通Node）
│  └── Unity.GraphToolkit.Tutorials.ContextBlocks.Editor.asmdef
└─ Examples/
    └── (将在Unity编辑器中创建.shaderfunc文件)
```

---

## 节点详解

### FunctionContextNode（函数上下文节点）

定义一个自定义函数，可以包含多个BlockNode。

```csharp
[Node("Function", "Shader")]
internal class FunctionContextNode : ContextNode, IVectorNode
{
    [SerializeField]
    private string m_FunctionName = "MyFunction";

    private IPort m_Output;

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        // 函数的输出端口
        m_Output = context.AddOutputPort<Vector3>("Result").Build();
    }

    public Vector3 EvaluateVector(IPort port, ShaderFunctionGraph graph)
    {
        // 评估函数内部的块节点
        Vector3 result = Vector3.zero;

        // 查找输出块节点
        foreach (var block in GetBlocks())
        {
            if (block is OutputBlockNode outputBlock)
            {
                result = outputBlock.EvaluateVector(null, graph);
                break;
            }
        }

        return result;
    }

    /// <summary>
    /// 获取所有块节点
    /// </summary>
    public IReadOnlyList<BlockNode> GetBlocks()
    {
        var blocks = new List<BlockNode>();
        foreach (var node in Graph.Nodes)
        {
            if (node is BlockNode block && block.Context == this)
            {
                blocks.Add(block);
            }
        }
        return blocks;
    }
}
```

**关键点**:
- 继承自`ContextNode`
- 可以包含多个BlockNode
- 通过`GetBlocks()`获取所有子块
- 评估时遍历内部块节点

---

### InputBlockNode（输入块节点）

代表函数的输入参数。

```csharp
[Node("Input A", "Shader/Block")]
internal class InputABlockNode : InputBlockNode
{
    [SerializeField]
    private Vector3 m_DefaultValue = Vector3.zero;

    /// <summary>
    /// 所属的上下文节点
    /// </summary>
    public FunctionContextNode Context
    {
        get
        {
            // 查找父ContextNode
            foreach (var node in Graph.Nodes)
            {
                if (node is FunctionContextNode context)
                {
                    foreach (var block in context.GetBlocks())
                    {
                        if (block == this)
                            return context;
                    }
                }
            }
            return null;
        }
    }

    public override Vector3 EvaluateVector(IPort port, ShaderFunctionGraph graph)
    {
        // 返回默认值或从外部传入的值
        return m_DefaultValue;
    }
}
```

**关键点**:
- 继承自`BlockNode`
- 必须有父ContextNode
- 代表函数参数
- 可以有默认值

---

### OutputBlockNode（输出块节点）

定义函数的返回值。

```csharp
[Node("Output", "Shader/Block")]
internal class OutputBlockNode : BlockNode, IVectorNode
{
    private IPort m_Input;

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        m_Input = context.AddInputPort<Vector3>("Result").Build();
    }

    public Vector3 EvaluateVector(IPort port, ShaderFunctionGraph graph)
    {
        // 评估输入端口
        var connectedPort = graph.GetConnectedOutputPort(m_Input);
        if (connectedPort != null)
        {
            return graph.EvaluateVectorPort(connectedPort);
        }

        return Vector3.zero;
    }
}
```

**关键点**:
- 只有输入端口
- 评估连接的节点
- 将结果返回给ContextNode

---

### OperationBlockNode（操作块节点）

在函数内部执行计算。

```csharp
[Node("Add", "Shader/Block")]
internal class AddBlockNode : BlockNode, IVectorNode
{
    private IPort m_InputA;
    private IPort m_InputB;
    private IPort m_Output;

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        m_InputA = context.AddInputPort<Vector3>("A").Build();
        m_InputB = context.AddInputPort<Vector3>("B").Build();
        m_Output = context.AddOutputPort<Vector3>("Result").Build();
    }

    public Vector3 EvaluateVector(IPort port, ShaderFunctionGraph graph)
    {
        Vector3 a = EvaluateInput(m_InputA, graph);
        Vector3 b = EvaluateInput(m_InputB, graph);
        return a + b;
    }
}
```

**关键点**:
- 有输入和输出端口
- 执行具体的计算
- 可以连接其他BlockNode

---

## 实践步骤

### 示例1: 简单的向量加法函数

创建一个函数：`Result = A + B`

**步骤**:
1. 创建.shaderfunc文件
2. 添加FunctionContextNode
3. 在ContextNode内部添加：
   - InputABlockNode
   - InputBBlockNode
   - AddBlockNode
   - OutputBlockNode
4. 连接：
   - InputA → Add的A端口
   - InputB → Add的B端口
   - Add → Output
5. 保存并查看评估结果

**图形结构**:
```
[Function Context]
  ├─ [Input A] ─┐
  │             ├─> [Add] ─> [Output]
  └─ [Input B] ─┘
```

---

### 示例2: 复杂的向量运算

创建一个函数：`Result = Normalize((A + B) * C)`

**步骤**:
1. 在FunctionContextNode内部添加：
   - InputABlockNode
   - InputBBlockNode
   - InputCBlockNode (需要创建)
   - AddBlockNode
   - MultiplyBlockNode
   - NormalizeBlockNode
   - OutputBlockNode
2. 连接：
   - InputA → Add的A
   - InputB → Add的B
   - Add → Multiply的A
   - InputC → Multiply的B
   - Multiply → Normalize
   - Normalize → Output

**图形结构**:
```
[Function Context]
  ├─ [Input A] ─┐
  │             ├─> [Add] ─> [Multiply] ─> [Normalize] ─> [Output]
  ├─ [Input B] ─┘              ↑
  └─ [Input C] ────────────────┘
```

---

## 作用域和封装

### 作用域隔离

BlockNode只能访问同一个ContextNode内的其他节点：

```csharp
// ✅ 正确 - 同一个ContextNode内的连接
[Function Context A]
  [Input A] → [Add] → [Output]

// ❌ 错误 - 不能跨ContextNode连接
[Function Context A]
  [Input A] ─┐
             X (不允许)
[Function Context B]  │
  [Add] ←─────────────┘
```

### 封装优势

1. **模块化**: 函数内部实现对外部隐藏
2. **复用**: 可以创建多个相同的ContextNode实例
3. **清晰性**: 逻辑分组，易于理解
4. **维护性**: 修改内部实现不影响外部

---

## 与Shader Graph的对比

### Shader Graph的Custom Function

```
Custom Function Node
├─ Inputs (参数)
├─ Body (HLSL代码)
└─ Outputs (返回值)
```

### GraphToolkit的ContextNode

```
FunctionContextNode
├─ InputBlockNodes (参数)
├─ OperationBlockNodes (计算逻辑)
└─ OutputBlockNode (返回值)
```

**相似之处**:
- 都提供函数封装
- 都有输入和输出
- 都可以复用

**不同之处**:
- Shader Graph使用代码，GraphToolkit使用节点
- Shader Graph编译为Shader，GraphToolkit在编辑器评估
- GraphToolkit更灵活，可以动态修改

---

## 高级用法

### 嵌套ContextNode

理论上可以在ContextNode内部创建另一个ContextNode（如果GraphToolkit支持）：

```
[Outer Function Context]
  ├─ [Input A]
  ├─ [Inner Function Context]
  │  ├─ [Input X]
  │  ├─ [Add]
  │  └─ [Output Y]
  └─ [Output]
```

### 条件执行

结合BranchNode实现条件函数：

```
[Function Context]
  ├─ [Input Condition]
  ├─ [Branch]
  │  ├─ True → [Operation A]
  │  └─ False → [Operation B]
  └─ [Output]
```

---

## 练习题

### 练习1: 向量点积函数
创建一个计算两个向量点积的函数。

**提示**:
```
Dot(A, B) = A.x * B.x + A.y * B.y + A.z * B.z
```

需要的BlockNode:
- 2个InputBlockNode
- 3个MultiplyBlockNode
- 2个AddBlockNode
- 1个OutputBlockNode

### 练习2: 向量插值函数
创建一个向量线性插值函数：`Lerp(A, B, t) = A + (B - A) * t`

### 练习3: 自定义光照函数
创建一个简单的Lambert光照计算函数。

---

## 常见问题

### Q: BlockNode可以独立存在吗？
A: 不可以。BlockNode必须属于某个ContextNode，否则会报错。

### Q: 如何在代码中创建BlockNode？
A: 
```csharp
var contextNode = graph.AddNode<FunctionContextNode>();
var blockNode = graph.AddNode<InputABlockNode>();
// BlockNode会自动关联到最近的ContextNode
```

### Q: ContextNode可以嵌套吗？
A: 理论上可以，但需要GraphToolkit的支持。当前版本可能有限制。

### Q: BlockNode可以有子图吗？
A: 可以，BlockNode也可以实现ISubgraphNode接口。

### Q: 如何调试ContextNode内部的执行？
A: 在BlockNode的Evaluate方法中添加Debug.Log，追踪执行流程。

---

## 最佳实践

### 1. 合理使用ContextNode

**✅ 适合使用ContextNode的场景**:
- 复杂的计算逻辑需要封装
- 需要创建可复用的函数
- 需要隔离作用域

**❌ 不适合使用ContextNode的场景**:
- 简单的单节点操作
- 不需要封装的线性流程

### 2. BlockNode命名规范

```csharp
// ✅ 好的命名
[Node("Input Position", "Shader/Block")]
[Node("Calculate Normal", "Shader/Block")]
[Node("Output Color", "Shader/Block")]

// ❌ 避免的命名
[Node("Block1", "Shader/Block")]
[Node("Node", "Shader/Block")]
```

### 3. 验证ContextNode完整性

```csharp
protected override void OnValidate()
{
    base.OnValidate();

    // 确保有输出块
    var outputBlock = FindOutputBlock();
    if (outputBlock == null)
    {
        Debug.LogWarning($"Function '{m_FunctionName}' has no output block");
    }

    // 确保有至少一个输入块
    var inputBlocks = GetBlocks().OfType<InputBlockNode>();
    if (!inputBlocks.Any())
    {
        Debug.LogWarning($"Function '{m_FunctionName}' has no input blocks");
    }
}
```

### 4. 性能考虑

- ContextNode的评估会遍历所有BlockNode
- 避免创建过深的嵌套结构
- 缓存评估结果（如果需要）

---

## 总结

ContextNode和BlockNode是GraphToolkit中最高级的特性：

**ContextNode**:
- 作为节点容器
- 提供作用域隔离
- 实现函数封装

**BlockNode**:
- 只能在ContextNode内部
- 实现函数内部逻辑
- 支持输入、输出、操作等类型

**应用场景**:
- 自定义Shader函数
- 复杂的数学运算
- 可复用的逻辑模块

掌握这些特性后，你可以创建更加模块化和可维护的图形系统！

---

## 下一步

在下一个教程中，我们将学习如何自定义GraphView UI，创建专业的图形编辑器界面。

---

**Sources**:
- [Unity GraphToolkit Documentation](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Unity.GraphToolkit.Editor.html)
= InputA + InputB`

**步骤**:
1. 创建.shaderfunc文件
2. 添加FunctionContextNode
3. 在函数内部添加：
   - InputABlockNode
   - InputBBlockNode
   - AddBlockNode
   - OutputBlockNode
4. 连接：
   - InputA → Add的A端口
   - InputB → Add的B端口
   - Add → Output
5. 保存并查看评估结果

### 示例2: 复杂的向量运算

创建函数：`Result = Normalize((InputA + InputB) * InputA)`

```
[InputA] ─┐
          ├─> [Add] ─┐
[InputB] ─┘          ├─> [Multiply] ─> [Normalize] ─> [Output]
                     │
[InputA] ────────────┘
```

**步骤**:
1. 添加FunctionContextNode
2. 添加块节点：
   - 2个InputABlockNode（可以复用）
   - 1个InputBBlockNode
   - 1个AddBlockNode
   - 1个MultiplyBlockNode
   - 1个NormalizeBlockNode
   - 1个OutputBlockNode
3. 按照上图连接
4. 评估函数

### 示例3: 多个函数

在同一个图形中创建多个函数：

```
ShaderFunctionGraph
├─ Function1 (ContextNode)
│  ├─ Input blocks
│  ├─ Operation blocks
│  └─ Output block
└─ Function2 (ContextNode)
   ├─ Input blocks
   ├─ Operation blocks
   └─ Output block
```

**注意**: 每个ContextNode是独立的作用域，BlockNode不能跨越。

---

## 作用域和封装

### 作用域规则

```csharp
// BlockNode只能访问：
// 1. 同一个ContextNode内的其他BlockNode
// 2. 图形中的普通Node（通过端口连接）

// BlockNode不能访问：
// 1. 其他ContextNode内的BlockNode
// 2. 其他ContextNode本身（除非通过端口）
```

### 封装示例

```
Function Context
┌─────────────────────────────────┐
│ [Input A] ─┐                    │
│            ├─> [Add] ─> [Output]│ ─> 外部可以访问
│ [Input B] ─┘                    │
└─────────────────────────────────┘
     ↑                    ↑
     │                    │
  外部输入            内部实现被封装
```

**优势**:
- 隐藏实现细节
- 提供清晰的接口
- 便于复用和维护

---

## 与Shader Graph的对比

### Shader Graph的Custom Function

```
Custom Function Node
├─ Inputs (参数)
├─ Body (HLSL代码)
└─ Outputs (返回值)
```

### GraphToolkit的ContextNode

```
ContextNode
├─ InputBlockNode (参数)
├─ OperationBlockNode (逻辑)
└─ OutputBlockNode (返回值)
```

**相似之处**:
- 都提供函数封装
- 都有输入和输出
- 都隐藏内部实现

**不同之处**:
- Shader Graph使用代码（HLSL）
- GraphToolkit使用可视化节点
- GraphToolkit更灵活，可以动态修改

---

## 高级用法

### 1. 嵌套ContextNode

理论上可以嵌套ContextNode（ContextNode包含另一个ContextNode），但GraphToolkit当前版本可能不支持。

```csharp
// 未来可能的用法
OuterContextNode
└─ InnerContextNode (BlockNode)
   └─ BlockNodes
```

### 2. 动态创建BlockNode

```csharp
// 在ContextNode中动态添加BlockNode
public void AddOperationBlock()
{
    var block = Graph.AddNode<AddBlockNode>();
    // 将block关联到this ContextNode
}
```

### 3. BlockNode之间的通信

```csharp
// BlockNode可以通过端口连接
[InputA] → [Operation1] → [Operation2] → [Output]

// 也可以通过共享的ContextNode数据
public class FunctionContextNode : ContextNode
{
    private Dictionary<string, object> m_SharedData;
    
    public void SetSharedData(string key, object value)
    {
        m_SharedData[key] = value;
    }
}
```

---

## 练习题

### 练习1: 点积函数
创建一个计算两个向量点积的函数。

**提示**:
```csharp
// 点积公式: dot(A, B) = A.x * B.x + A.y * B.y + A.z * B.z
// 需要的块节点:
// - 2个InputBlockNode
// - 3个MultiplyBlockNode
// - 2个AddBlockNode
// - 1个OutputBlockNode
```

### 练习2: 条件函数
创建一个根据条件选择不同输入的函数。

**提示**:
```csharp
// 伪代码: Result = condition ? InputA : InputB
// 需要添加条件判断的BlockNode
```

### 练习3: 循环函数
创建一个执行多次操作的函数。

**提示**:
```csharp
// 伪代码: for (int i = 0; i < count; i++) { result += input; }
// 需要添加循环控制的BlockNode
```

---

## 常见问题

### Q: BlockNode和普通Node有什么区别？

A: 
- **BlockNode**: 只能在ContextNode内部，提供封装和作用域
- **普通Node**: 可以在图形的任何地方，独立存在

### Q: 为什么需要ContextNode？

A: 
1. **封装**: 隐藏实现细节
2. **复用**: 创建可复用的函数
3. **组织**: 更好地组织复杂图形
4. **作用域**: 提供变量和数据的作用域

### Q: 可以在BlockNode中创建子图吗？

A: 可以，BlockNode可以实现ISubgraphNode接口，引用其他图形。

### Q: ContextNode可以嵌套吗？

A: 理论上可以，但当前版本的GraphToolkit可能不完全支持。需要测试具体行为。

### Q: 如何调试ContextNode内部的执行？

A: 
```csharp
public Vector3 EvaluateVector(IPort port, ShaderFunctionGraph graph)
{
    Debug.Log($"Evaluating function: {m_FunctionName}");
    
    foreach (var block in GetBlocks())
    {
        Debug.Log($"Block: {block.Name}");
    }
    
    // 评估逻辑...
}
```

### Q: BlockNode可以访问外部节点吗？

A: 可以，通过端口连接。BlockNode的端口可以连接到图形中的任何节点（包括ContextNode外部的普通节点）。

---

## 最佳实践

### 1. 清晰的命名

```csharp
// ✅ 好的命名
[Node("Calculate Normal", "Shader")]
internal class CalculateNormalContextNode : ContextNode { }

[Node("Tangent Input", "Shader/Block")]
internal class TangentInputBlockNode : BlockNode { }

// ❌ 避免的命名
[Node("Node1", "Shader")]
internal class Node1 : ContextNode { }
```

### 2. 验证BlockNode的父节点

```csharp
protected override void OnValidate()
{
    base.OnValidate();
    
    var context = Context;
    if (context == null)
    {
        Debug.LogError($"BlockNode '{Name}' has no parent ContextNode!");
    }
}
```

### 3. 限制BlockNode的数量

```csharp
public class FunctionContextNode : ContextNode
{
    private const int MaxBlocks = 20;
    
    protected override void OnValidate()
    {
        if (GetBlocks().Count > MaxBlocks)
        {
            Debug.LogWarning($"Function has too many blocks ({GetBlocks().Count})");
        }
    }
}
```

### 4. 提供默认的BlockNode

```csharp
protected override void OnEnable()
{
    base.OnEnable();
    
    // 自动创建输入和输出块
    if (GetBlocks().Count == 0)
    {
        Graph.AddNode<InputABlockNode>();
        Graph.AddNode<OutputBlockNode>();
    }
}
```

---

## 性能考虑

### 1. 避免过深的嵌套

```csharp
// ❌ 避免
ContextNode1
└─ ContextNode2
   └─ ContextNode3
      └─ ContextNode4 (太深)

// ✅ 推荐
ContextNode1
├─ BlockNodes
└─ BlockNodes
```

### 2. 缓存BlockNode列表

```csharp
private List<BlockNode> m_CachedBlocks;

public IReadOnlyList<BlockNode> GetBlocks()
{
    if (m_CachedBlocks == null)
    {
        m_CachedBlocks = new List<BlockNode>();
        // 填充列表...
    }
    return m_CachedBlocks;
}
```

### 3. 优化评估顺序

```csharp
// 按依赖关系排序BlockNode
public void SortBlocksByDependency()
{
    var sorted = new List<BlockNode>();
    var visited = new HashSet<BlockNode>();
    
    foreach (var block in GetBlocks())
    {
        TopologicalSort(block, visited, sorted);
    }
    
    m_CachedBlocks = sorted;
}
```

---

## 总结

ContextNode和BlockNode是GraphToolkit中最高级的特性：

1. **ContextNode**: 提供作用域和封装
2. **BlockNode**: 只能在ContextNode内部
3. **作用域**: 清晰的边界和隔离
4. **封装**: 隐藏实现细节
5. **复用**: 创建可复用的函数模块

这些特性让你能够构建复杂的、模块化的图形系统，类似于Shader Graph的自定义函数节点。

---

## 下一步

在下一个教程中，我们将学习如何自定义GraphView UI，创建专业的图形编辑器界面。

---

**Sources**:
- [Unity GraphToolkit Documentation](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Unity.GraphToolkit.Editor.html)
