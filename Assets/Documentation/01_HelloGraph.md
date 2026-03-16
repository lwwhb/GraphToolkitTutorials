# 教程1: Hello Graph - 简单计算器图形

## 概述

这是GraphToolkit系列教程的第一课，通过创建一个简单的计算器图形系统，帮助你理解GraphToolkit的核心概念。

## 学习目标

- 理解Graph、Node、Port的基本概念
- 学会创建自定义图形类型
- 掌握节点的端口定义
- 理解数据流图形的评估机制
- 学会使用ScriptedImporter创建自定义资产类型

## 核心概念

### 1. Graph（图形）

Graph是节点的容器，管理节点之间的连接关系。在GraphToolkit中，你需要：

```csharp
[Graph("calc", GraphOptions.None)]
internal class CalculatorGraph : Graph
{
    // 图形的自定义逻辑
}
```

- `[Graph]`属性定义文件扩展名（这里是.calc）
- 继承自`Graph`基类
- 可以添加自定义方法来处理图形逻辑

### 2. Node（节点）

Node是图形中的基本处理单元。每个节点可以有输入端口和输出端口：

```csharp
[Node("Add", "Calculator")]
internal class AddNode : Node, ICalculatorNode
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort<float>("A").Build();
        context.AddInputPort<float>("B").Build();
        context.AddOutputPort<float>("Result").Build();
    }
}
```

- `[Node]`属性定义节点在创建菜单中的显示名称和分类
- `OnDefinePorts`方法定义节点的输入输出端口

### 3. Port（端口）

Port是节点之间传递数据的接口：

- **输入端口（Input Port）**: 接收来自其他节点的数据
- **输出端口（Output Port）**: 向其他节点提供数据
- 端口有类型（如`float`），确保类型匹配才能连接

### 4. 数据流评估

计算器图形使用递归评估模式：

```csharp
public float Evaluate(IPort port, CalculatorGraph graph)
{
    // 1. 获取输入端口连接的输出端口
    var connectedPort = graph.GetConnectedOutputPort(inputPort);

    // 2. 递归评估连接的端口
    if (connectedPort != null)
    {
        return graph.EvaluatePort(connectedPort);
    }

    // 3. 没有连接则返回默认值
    return 0f;
}
```

## 项目结构

```
01_HelloGraph/
├─ Editor/
│  ├─ CalculatorGraph.cs              # 图形定义
│  ├─ CalculatorImporter.cs           # 资产导入器
│  ├─ Nodes/
│  │  ├─ ICalculatorNode.cs           # 节点接口
│  │  ├─ ConstantNode.cs              # 常量节点
│  │  ├─ AddNode.cs                   # 加法节点
│  │  ├─ SubtractNode.cs              # 减法节点
│  │  ├─ MultiplyNode.cs              # 乘法节点
│  │  ├─ DivideNode.cs                # 除法节点
│  │  └─ OutputNode.cs                # 输出节点
│  └─ Unity.GraphToolkit.Tutorials.HelloGraph.Editor.asmdef
└─ Examples/
   └─ (将在Unity编辑器中创建.calc文件)
```

## 节点详解

### ConstantNode（常量节点）

最简单的节点，输出一个固定值：

```csharp
[Node("Constant", "Calculator")]
internal class ConstantNode : Node, ICalculatorNode
{
    [SerializeField]
    private float m_Value = 0f;

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddOutputPort<float>("Value").Build();
    }

    public float Evaluate(IPort port, CalculatorGraph graph)
    {
        return m_Value;
    }
}
```

**特点**:
- 只有输出端口，没有输入端口
- 值通过`[SerializeField]`序列化保存
- 评估时直接返回存储的值

### AddNode（加法节点）

接收两个输入，输出它们的和：

```csharp
public float Evaluate(IPort port, CalculatorGraph graph)
{
    float a = EvaluateInputPort(m_InputA, graph);
    float b = EvaluateInputPort(m_InputB, graph);
    return a + b;
}
```

**特点**:
- 两个输入端口（A和B）
- 一个输出端口（Result）
- 递归评估输入端口的值

### OutputNode（输出节点）

标记图形的最终输出：

```csharp
public float Evaluate(IPort port, CalculatorGraph graph)
{
    var connectedPort = graph.GetConnectedOutputPort(m_Input);
    if (connectedPort != null)
    {
        m_CachedResult = graph.EvaluatePort(connectedPort);
    }
    return m_CachedResult;
}
```

**特点**:
- 一个输入端口
- 缓存计算结果
- 通过Options显示结果值

## 使用ScriptedImporter

`CalculatorImporter`负责导入.calc文件：

```csharp
[ScriptedImporter(1, "calc")]
internal class CalculatorImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        // 1. 加载图形
        var graph = GraphDatabase.LoadGraphForImporter<CalculatorGraph>(ctx.assetPath);

        // 2. 查找输出节点并评估
        foreach (var node in graph.Nodes)
        {
            if (node is OutputNode output)
            {
                result = output.Evaluate(null, graph);
                break;
            }
        }

        // 3. 创建结果资产
        var resultAsset = ScriptableObject.CreateInstance<CalculatorResult>();
        resultAsset.result = result;
        ctx.AddObjectToAsset("main", resultAsset);
        ctx.SetMainObject(resultAsset);
    }
}
```

**工作流程**:
1. Unity检测到.calc文件
2. 调用`OnImportAsset`方法
3. 加载并评估图形
4. 生成包含结果的资产

## 实践步骤

### 1. 创建计算器图形资产

1. 在Unity编辑器中，右键点击Project窗口
2. 选择 `Create > Graph > Calculator Graph`
3. 命名为`SimpleCalculation.calc`

### 2. 添加节点

1. 双击打开图形编辑器
2. 右键点击空白区域，选择 `Create Node`
3. 添加以下节点：
   - 2个Constant节点
   - 1个Add节点
   - 1个Output节点

### 3. 配置节点

1. 选择第一个Constant节点，在Inspector中设置Value为5
2. 选择第二个Constant节点，设置Value为3

### 4. 连接节点

1. 从第一个Constant的Value端口拖线到Add的A端口
2. 从第二个Constant的Value端口拖线到Add的B端口
3. 从Add的Result端口拖线到Output的Input端口

### 5. 查看结果

1. 保存图形（Ctrl+S）
2. Unity会自动重新导入资产
3. 在Console中查看日志：`Calculator graph evaluated: 8`
4. 选择.calc资产，在Inspector中查看Result字段

## 示例：计算 (5 + 3) * 2

创建更复杂的计算：

```
[Constant: 5] ─┐
               ├─> [Add] ─> [Multiply] ─> [Output]
[Constant: 3] ─┘              ↑
                              │
[Constant: 2] ────────────────┘
```

**步骤**:
1. 添加3个Constant节点（值为5, 3, 2）
2. 添加1个Add节点和1个Multiply节点
3. 连接：Constant(5)和Constant(3) → Add
4. 连接：Add → Multiply的A端口
5. 连接：Constant(2) → Multiply的B端口
6. 连接：Multiply → Output

**结果**: 16

## 练习题

### 练习1: 基础计算
创建一个图形计算：`(10 - 4) / 2`

**提示**: 需要使用Subtract和Divide节点

### 练习2: 复杂表达式
创建一个图形计算：`(5 + 3) * (10 - 2)`

**提示**: 需要两个Add/Subtract节点和一个Multiply节点

### 练习3: 添加新节点
实现一个PowerNode（幂运算节点），计算A的B次方。

**提示**:
```csharp
[Node("Power", "Calculator")]
internal class PowerNode : Node, ICalculatorNode
{
    // 实现 A^B
    public float Evaluate(IPort port, CalculatorGraph graph)
    {
        float a = EvaluateInputPort(m_InputA, graph);
        float b = EvaluateInputPort(m_InputB, graph);
        return Mathf.Pow(a, b);
    }
}
```

## 关键要点

1. **Graph是容器**: 管理节点和连接
2. **Node是处理单元**: 通过端口接收和输出数据
3. **Port是数据接口**: 定义节点之间的数据流
4. **递归评估**: 数据流图形通过递归评估端口来计算结果
5. **ScriptedImporter**: 将自定义文件格式导入为Unity资产

## 下一步

在下一个教程中，我们将学习如何创建更复杂的数据流图形——纹理生成器，它会生成实际的纹理资产。

## 常见问题

**Q: 为什么需要ICalculatorNode接口？**

A: 接口定义了统一的评估方法，使得图形可以统一处理所有计算器节点，而不需要知道具体的节点类型。

**Q: 如果创建循环连接会怎样？**

A: 当前实现会导致无限递归。在生产环境中，需要添加循环检测机制。

**Q: 可以在运行时使用这个图形吗？**

A: 当前实现是编辑器时评估。要在运行时使用，需要创建运行时版本的图形和节点（参见教程3）。

**Q: 如何调试图形评估？**

A: 在Evaluate方法中添加Debug.Log语句，可以追踪评估过程。

## 总结

通过这个简单的计算器示例，你已经掌握了GraphToolkit的核心概念。这些概念是后续所有教程的基础，包括数据流处理、执行流控制，以及最终的图形化渲染管线。
