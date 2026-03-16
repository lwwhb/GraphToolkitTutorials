# 教程6: 自定义编辑器UI

## 概述

本教程将探讨如何自定义GraphToolkit的编辑器界面。虽然GraphToolkit提供了内置的图形编辑器，但你可以通过各种方式自定义节点的外观、行为和交互方式。

## 学习目标

- 理解GraphToolkit的UI定制选项
- 掌握NodeAttribute的高级用法
- 学会自定义节点选项（Options）
- 理解端口的UI配置
- 掌握节点颜色和样式定制
- 了解GraphView扩展的可能性

## 核心概念

### GraphToolkit的UI架构

GraphToolkit使用Unity的UI Toolkit（原UIElements）构建编辑器界面：

```
GraphToolkit UI 层次
├─ Graph Window (图形窗口)
│  ├─ Toolbar (工具栏)
│  ├─ GraphView (图形视图)
│  │  ├─ Nodes (节点)
│  │  ├─ Connections (连接线)
│  │  └─ Grid (网格背景)
│  └─ Blackboard (变量面板)
└─ Inspector (检查器)
   └─ Node Options (节点选项)
```

**注意**: 当前版本的GraphToolkit主要通过属性和选项来定制UI，完全自定义GraphView需要更深入的扩展。

---

## NodeAttribute高级用法

### 基本属性

```csharp
[Node("Node Name", "Category")]
internal class MyNode : Node { }
```

### 完整属性

```csharp
[Node("Styled Node", "Custom",
    Description = "A node with custom styling",  // 节点描述
    Color = "#FF6B6B")]                          // 节点颜色（十六进制）
internal class StyledNode : Node { }
```

**支持的属性**:
- `Name`: 节点显示名称
- `Category`: 节点分类（用于创建菜单）
- `Description`: 节点描述（显示在提示中）
- `Color`: 节点颜色（十六进制格式，如 "#FF6B6B"）

### 颜色示例

```csharp
// 红色节点
[Node("Red Node", "Custom", Color = "#FF6B6B")]

// 蓝色节点
[Node("Blue Node", "Custom", Color = "#4ECDC4")]

// 绿色节点
[Node("Green Node", "Custom", Color = "#95E1D3")]

// 紫色节点
[Node("Purple Node", "Custom", Color = "#A78BFA")]
```

---

## 节点选项定制

### 基本选项类型

```csharp
protected override void OnDefineOptions(IOptionDefinitionContext context)
{
    // 浮点数
    context.AddOption("Float Value", () => m_FloatValue, v => m_FloatValue = v).Build();

    // 整数
    context.AddOption("Int Value", () => m_IntValue, v => m_IntValue = v).Build();

    // 布尔值
    context.AddOption("Bool Value", () => m_BoolValue, v => m_BoolValue = v).Build();

    // 字符串
    context.AddOption("String Value", () => m_StringValue, v => m_StringValue = v).Build();

    // 颜色
    context.AddOption("Color", () => m_ColorValue, v => m_ColorValue = v).Build();

    // 向量
    context.AddOption("Vector", () => m_VectorValue, v => m_VectorValue = v).Build();

    // 枚举
    context.AddOption("Mode", () => m_Mode, v => m_Mode = v).Build();
}
```

### 延迟更新选项

适合文本输入等需要延迟更新的场景：

```csharp
context.AddOption("String Value", () => m_StringValue, v => m_StringValue = v)
    .Delayed()  // 延迟更新，直到失去焦点或按Enter
    .Build();
```

### 自定义标签和提示

```csharp
context.AddOption("internalName", () => m_Value, v => m_Value = v)
    .WithLabel("Display Name")           // 自定义显示标签
    .WithTooltip("This is a tooltip")    // 添加提示文本
    .Build();
```

### 只读选项

```csharp
// 只读选项（没有setter）
context.AddOption("Node Count", () => Graph.Nodes.Count, null).Build();
```

### 带验证的选项

```csharp
context.AddOption("Normalized Value",
    () => m_Value,
    v => m_Value = Mathf.Clamp01(v)  // 自动限制范围
).Build();
```

---

## 端口UI定制

### 端口容量

```csharp
protected override void OnDefinePorts(IPortDefinitionContext context)
{
    // 单连接端口（默认）
    context.AddInputPort<float>("Single")
        .WithCapacity(PortCapacity.Single)
        .Build();

    // 多连接端口
    context.AddInputPort<float>("Multiple")
        .WithCapacity(PortCapacity.Multiple)
        .Build();
}
```

**使用场景**:
- `Single`: 大多数数据端口（一个输入只能连接一个输出）
- `Multiple`: 事件端口、聚合端口（一个输入可以连接多个输出）

### 端口连接器样式

```csharp
protected override void OnDefinePorts(IPortDefinitionContext context)
{
    // 默认样式（圆点）
    context.AddInputPort<float>("Data")
        .Build();

    // 箭头样式（用于执行流）
    context.AddInputPort("Execution")
        .WithConnectorUI(PortConnectorUI.Arrowhead)
        .Build();
}
```

**样式选择**:
- `Default`: 数据流端口（圆点）
- `Arrowhead`: 执行流端口（箭头）

### 端口命名规范

```csharp
// ✅ 好的命名
context.AddInputPort<Color>("Base Color").Build();
context.AddInputPort<float>("Metallic").Build();
context.AddOutputPort<Vector3>("Normal").Build();

// ❌ 避免的命名
context.AddInputPort<Color>("input1").Build();
context.AddInputPort<float>("x").Build();
```

---

## 项目结构

```
06_CustomUI/
├─ Editor/
│  ├── CustomGraph.cs                  # 自定义图形
│  ├── CustomGraphImporter.cs          # 资产导入器
│  ├── Nodes/
│  │   ├── ICustomNode.cs              # 节点接口
│  │   ├── StyledNode.cs               # 样式化节点
│  │   ├── OptionsNode.cs              # 选项演示节点
│  │   ├── MultiPortNode.cs            # 多端口节点
│  │   └── PreviewNode.cs              # 预览节点
│  ├── UI/
│  │   └── (预留用于自定义UI扩展)
│  └── Unity.GraphToolkit.Tutorials.CustomUI.Editor.asmdef
└─ Examples/
    └── (将在Unity编辑器中创建.customgraph文件)
```

---

## 节点详解

### StyledNode（样式化节点）

展示如何使用NodeAttribute自定义节点外观：

```csharp
[Node("Styled Node", "Custom",
    Description = "A node with custom styling",
    Color = "#FF6B6B")]
[UseWithGraph(typeof(CustomGraph))]
internal class StyledNode : Node, IFloatNode
{
    [SerializeField]
    private float m_Value = 1f;

    [SerializeField]
    private string m_Label = "Styled";

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort<float>("Input")
            .WithCapacity(PortCapacity.Multiple)  // 允许多个连接
            .Build();

        context.AddOutputPort<float>("Output").Build();
    }

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption("Value", () => m_Value, v => m_Value = v).Build();

        context.AddOption("Label", () => m_Label, v => m_Label = v)
            .Delayed()  // 延迟更新
            .Build();
    }
}
```

**特点**:
- 自定义节点颜色（#FF6B6B 红色）
- 节点描述
- 多连接输入端口
- 延迟更新的文本选项

---

### OptionsNode（选项演示节点）

展示各种类型的节点选项：

```csharp
[Node("Options Demo", "Custom")]
internal class OptionsNode : Node, IColorNode
{
    // 基本类型
    [SerializeField] private float m_FloatValue = 0.5f;
    [SerializeField] private int m_IntValue = 10;
    [SerializeField] private bool m_BoolValue = true;
    [SerializeField] private string m_StringValue = "Hello";

    // Unity类型
    [SerializeField] private Color m_ColorValue = Color.red;
    [SerializeField] private Vector3 m_VectorValue = Vector3.zero;

    // 枚举
    public enum OperationMode { Add, Multiply, Mix }
    [SerializeField] private OperationMode m_Mode = OperationMode.Add;

    // 范围限制
    [SerializeField][Range(0f, 1f)] private float m_NormalizedValue = 0.5f;

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        // 基本选项
        context.AddOption("Float Value", () => m_FloatValue, v => m_FloatValue = v).Build();
        context.AddOption("Int Value", () => m_IntValue, v => m_IntValue = v).Build();
        context.AddOption("Bool Value", () => m_BoolValue, v => m_BoolValue = v).Build();

        // 延迟更新的字符串
        context.AddOption("String Value", () => m_StringValue, v => m_StringValue = v)
            .Delayed()
            .Build();

        // Unity类型
        context.AddOption("Color", () => m_ColorValue, v => m_ColorValue = v).Build();
        context.AddOption("Vector", () => m_VectorValue, v => m_VectorValue = v).Build();

        // 枚举
        context.AddOption("Mode", () => m_Mode, v => m_Mode = v).Build();

        // 带范围限制
        context.AddOption("Normalized", () => m_NormalizedValue, 
            v => m_NormalizedValue = Mathf.Clamp01(v)).Build();

        // 带标签和提示
        context.AddOption("Custom Label", () => m_FloatValue, v => m_FloatValue = v)
            .WithLabel("My Custom Label")
            .WithTooltip("This is a custom tooltip")
            .Build();
    }
}
```

**展示的选项类型**:
- 基本类型（float, int, bool, string）
- Unity类型（Color, Vector3）
- 枚举类型
- 范围限制
- 自定义标签和提示

---

### MultiPortNode（多端口节点）

展示不同的端口配置：

```csharp
[Node("Multi Port", "Custom")]
internal class MultiPortNode : Node, IFloatNode
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        // 单连接输入端口
        context.AddInputPort<float>("Single Input")
            .WithCapacity(PortCapacity.Single)
            .Build();

        // 多连接输入端口
        context.AddInputPort<float>("Multiple Input")
            .WithCapacity(PortCapacity.Multiple)
            .Build();

        // 执行流端口（箭头样式）
        context.AddInputPort("Execution")
            .WithConnectorUI(PortConnectorUI.Arrowhead)
            .Build();

        // 输出端口
        context.AddOutputPort<float>("Output").Build();
    }

    public float EvaluateFloat(IPort port, CustomGraph graph)
    {
        float result = 0f;

        // 评估单连接输入
        var connectedPort = graph.GetConnectedOutputPort(m_SingleInput);
        if (connectedPort != null)
        {
            result += graph.EvaluateFloatPort(connectedPort);
        }

        // 评估多连接输入（遍历所有连接）
        foreach (var connection in graph.Connections)
        {
            if (connection.InputPort == m_MultipleInput)
            {
                result += graph.EvaluateFloatPort(connection.OutputPort);
            }
        }

        return result;
    }
}
```

**特点**:
- 单连接端口
- 多连接端口
- 执行流端口（箭头样式）
- 处理多个连接的逻辑

---

### PreviewNode（预览节点）

展示如何在节点上显示预览：

```csharp
[Node("Preview Node", "Custom", Color = "#4ECDC4")]
internal class PreviewNode : Node, IColorNode
{
    [SerializeField] private Color m_Color = Color.white;
    [SerializeField] private bool m_ShowPreview = true;

    public Color GetPreviewColor()
    {
        return m_Color;
    }

    public bool ShouldShowPreview()
    {
        return m_ShowPreview;
    }
}
```

**注意**: 实际的预览UI需要通过自定义GraphView扩展来实现，这里提供了数据接口。

---

## 实践步骤

### 示例1: 创建彩色节点

创建一组不同颜色的节点：

```csharp
[Node("Red Node", "Colors", Color = "#FF6B6B")]
internal class RedNode : Node { }

[Node("Blue Node", "Colors", Color = "#4ECDC4")]
internal class BlueNode : Node { }

[Node("Green Node", "Colors", Color = "#95E1D3")]
internal class GreenNode : Node { }
```

### 示例2: 创建配置节点

创建一个包含多种配置选项的节点：

```csharp
[Node("Config", "Custom")]
internal class ConfigNode : Node
{
    [SerializeField] private string m_Name = "Default";
    [SerializeField] private int m_Priority = 0;
    [SerializeField] private bool m_Enabled = true;

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption("Name", () => m_Name, v => m_Name = v)
            .Delayed()
            .WithTooltip("Configuration name")
            .Build();

        context.AddOption("Priority", () => m_Priority, v => m_Priority = v)
            .WithTooltip("Execution priority")
            .Build();

        context.AddOption("Enabled", () => m_Enabled, v => m_Enabled = v)
            .WithTooltip("Enable/disable this configuration")
            .Build();
    }
}
```

### 示例3: 创建聚合节点

创建一个可以接收多个输入的聚合节点：

```csharp
[Node("Sum", "Math")]
internal class SumNode : Node, IFloatNode
{
    private IPort m_Inputs;
    private IPort m_Output;

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        m_Inputs = context.AddInputPort<float>("Values")
            .WithCapacity(PortCapacity.Multiple)
            .Build();

        m_Output = context.AddOutputPort<float>("Sum").Build();
    }

    public float EvaluateFloat(IPort port, CustomGraph graph)
    {
        float sum = 0f;

        // 遍历所有连接的输入
        foreach (var connection in graph.Connections)
        {
            if (connection.InputPort == m_Inputs)
            {
                sum += graph.EvaluateFloatPort(connection.OutputPort);
            }
        }

        return sum;
    }
}
```

---

## GraphView扩展（高级）

### 当前限制

GraphToolkit的当前版本主要通过属性和选项来定制UI。完全自定义GraphView需要：

1. 访问GraphToolkit的内部API
2. 继承或扩展GraphView类
3. 自定义节点视图（NodeView）
4. 自定义连接视图（EdgeView）

### 未来可能的扩展

```csharp
// 理论上的自定义GraphView（需要GraphToolkit支持）
public class CustomGraphView : GraphView
{
    protected override NodeView CreateNodeView(INode node)
    {
        if (node is PreviewNode previewNode)
        {
            return new PreviewNodeView(previewNode);
        }
        return base.CreateNodeView(node);
    }
}

public class PreviewNodeView : NodeView
{
    private VisualElement m_PreviewElement;

    public PreviewNodeView(PreviewNode node)
    {
        // 创建预览UI
        m_PreviewElement = new VisualElement();
        m_PreviewElement.style.backgroundColor = node.GetPreviewColor();
        Add(m_PreviewElement);
    }
}
```

**注意**: 这需要GraphToolkit提供相应的扩展点，当前版本可能不支持。

---

## 最佳实践

### 1. 一致的颜色方案

```csharp
// 定义颜色常量
public static class NodeColors
{
    public const string Input = "#4ECDC4";
    public const string Output = "#FF6B6B";
    public const string Process = "#95E1D3";
    public const string Control = "#A78BFA";
}

[Node("Input", "Custom", Color = NodeColors.Input)]
internal class InputNode : Node { }
```

### 2. 有意义的选项名称

```csharp
// ✅ 好的命名
context.AddOption("Blend Factor", () => m_BlendFactor, v => m_BlendFactor = v).Build();
context.AddOption("Use Alpha", () => m_UseAlpha, v => m_UseAlpha = v).Build();

// ❌ 避免的命名
context.AddOption("value1", () => m_Value1, v => m_Value1 = v).Build();
context.AddOption("flag", () => m_Flag, v => m_Flag = v).Build();
```

### 3. 添加提示文本

```csharp
context.AddOption("Threshold", () => m_Threshold, v => m_Threshold = v)
    .WithTooltip("Values below this threshold will be discarded")
    .Build();
```

### 4. 验证输入值

```csharp
context.AddOption("Scale", () => m_Scale, v => {
    if (v <= 0f)
    {
        Debug.LogWarning("Scale must be positive");
        v = 0.01f;
    }
    m_Scale = v;
}).Build();
```

### 5. 使用枚举而非布尔值（当有多个选项时）

```csharp
// ✅ 使用枚举
public enum BlendMode { Add, Multiply, Screen, Overlay }
[SerializeField] private BlendMode m_Mode;

// ❌ 使用多个布尔值
[SerializeField] private bool m_IsAdd;
[SerializeField] private bool m_IsMultiply;
[SerializeField] private bool m_IsScreen;
```

---

## 性能考虑

### 1. 避免在OnDefineOptions中执行复杂计算

```csharp
// ❌ 避免
protected override void OnDefineOptions(IOptionDefinitionContext context)
{
    // 每次Inspector刷新都会调用
    var expensiveValue = CalculateExpensiveValue();
    context.AddOption("Value", () => expensiveValue, null).Build();
}

// ✅ 推荐
[SerializeField] private float m_CachedValue;

protected override void OnDefineOptions(IOptionDefinitionContext context)
{
    context.AddOption("Value", () => m_CachedValue, v => {
        m_CachedValue = v;
        // 只在值改变时重新计算
    }).Build();
}
```

### 2. 限制多连接端口的数量

```csharp
public float EvaluateFloat(IPort port, CustomGraph graph)
{
    int connectionCount = 0;
    float sum = 0f;

    foreach (var connection in graph.Connections)
    {
        if (connection.InputPort == m_MultipleInput)
        {
            if (connectionCount >= 100)
            {
                Debug.LogWarning("Too many connections!");
                break;
            }
            sum += graph.EvaluateFloatPort(connection.OutputPort);
            connectionCount++;
        }
    }

    return sum;
}
```

---

## 常见问题

### Q: 如何改变节点的大小？

A: 当前版本的GraphToolkit不直接支持自定义节点大小。节点大小由内容（端口和选项）自动决定。

### Q: 可以在节点上显示图片吗？

A: 通过选项系统可以显示Texture2D等Unity对象，但完全自定义节点内容需要扩展GraphView。

### Q: 如何创建自定义的端口样式？

A: 当前只支持Default和Arrowhead两种样式。自定义样式需要扩展GraphView。

### Q: 节点颜色支持渐变吗？

A: 当前只支持纯色。渐变需要自定义NodeView。

### Q: 如何在节点上显示实时预览？

A: 需要自定义NodeView并添加预览元素。当前版本的GraphToolkit可能不完全支持。

---

## 总结

教程6展示了GraphToolkit的UI定制选项：

1. **NodeAttribute**: 自定义节点名称、颜色、描述
2. **选项系统**: 丰富的Inspector选项类型
3. **端口配置**: 容量和连接器样式
4. **最佳实践**: 一致性、可读性、性能

**限制**:
- 完全自定义GraphView需要更深入的扩展
- 当前主要通过属性和选项来定制

**下一步**: 教程7将进入实战项目，创建完整的行为树系统！

---

## 练习题

### 练习1: 创建颜色主题
创建一组使用统一颜色主题的节点。

### 练习2: 配置节点
创建一个包含10种不同类型选项的配置节点。

### 练习3: 聚合节点
创建一个可以接收无限个输入并计算平均值的节点。

---

**Sources**:
- Unity GraphToolkit Documentation
- UI Toolkit Documentation
PART2
ro;

    // 枚举
    public enum OperationMode { Add, Multiply, Mix }
    [SerializeField] private OperationMode m_Mode = OperationMode.Add;

    // 范围限制
    [SerializeField] [Range(0f, 1f)] private float m_NormalizedValue = 0.5f;

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        // 基本选项
        context.AddOption("Float Value", () => m_FloatValue, v => m_FloatValue = v).Build();
        context.AddOption("Int Value", () => m_IntValue, v => m_IntValue = v).Build();
        context.AddOption("Bool Value", () => m_BoolValue, v => m_BoolValue = v).Build();

        // 延迟更新的字符串
        context.AddOption("String Value", () => m_StringValue, v => m_StringValue = v)
            .Delayed()
            .Build();

        // Unity类型
        context.AddOption("Color", () => m_ColorValue, v => m_ColorValue = v).Build();
        context.AddOption("Vector", () => m_VectorValue, v => m_VectorValue = v).Build();

        // 枚举
        context.AddOption("Mode", () => m_Mode, v => m_Mode = v).Build();

        // 带范围限制
        context.AddOption("Normalized", () => m_NormalizedValue, 
            v => m_NormalizedValue = Mathf.Clamp01(v)).Build();

        // 带标签和提示
        context.AddOption("Custom Label", () => m_FloatValue, v => m_FloatValue = v)
            .WithLabel("My Custom Label")
            .WithTooltip("This is a custom tooltip")
            .Build();
    }
}
```

**展示的选项类型**:
- 基本类型（float、int、bool、string）
- Unity类型（Color、Vector3）
- 枚举类型
- 带验证的值
- 自定义标签和提示

---

### MultiPortNode（多端口节点）

展示不同的端口配置：

```csharp
[Node("Multi Port", "Custom")]
internal class MultiPortNode : Node, IFloatNode
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        // 单连接输入端口
        context.AddInputPort<float>("Single Input")
            .WithCapacity(PortCapacity.Single)
            .Build();

        // 多连接输入端口
        context.AddInputPort<float>("Multiple Input")
            .WithCapacity(PortCapacity.Multiple)
            .Build();

        // 执行流端口（箭头样式）
        context.AddInputPort("Execution")
            .WithConnectorUI(PortConnectorUI.Arrowhead)
            .Build();

        // 输出端口
        context.AddOutputPort<float>("Output").Build();
    }

    public float EvaluateFloat(IPort port, CustomGraph graph)
    {
        float result = 0f;

        // 评估单连接输入
        var connectedPort = graph.GetConnectedOutputPort(m_SingleInput);
        if (connectedPort != null)
        {
            result += graph.EvaluateFloatPort(connectedPort);
        }

        // 评估多连接输入（遍历所有连接）
        foreach (var connection in graph.Connections)
        {
            if (connection.InputPort == m_MultipleInput)
            {
                result += graph.EvaluateFloatPort(connection.OutputPort);
            }
        }

        return result;
    }
}
```

**特点**:
- 单连接端口
- 多连接端口
- 执行流端口（箭头样式）
- 处理多个连接的逻辑

---

### PreviewNode（预览节点）

展示如何在节点上显示预览：

```csharp
[Node("Preview Node", "Custom", Color = "#4ECDC4")]
internal class PreviewNode : Node, IColorNode
{
    [SerializeField] private Color m_Color = Color.white;
    [SerializeField] private bool m_ShowPreview = true;

    public Color EvaluateColor(IPort port, CustomGraph graph)
    {
        // 评估输入颜色
        var connectedPort = graph.GetConnectedOutputPort(m_ColorInput);
        if (connectedPort != null)
        {
            m_Color = graph.EvaluateColorPort(connectedPort);
        }
        return m_Color;
    }

    // 用于自定义UI的辅助方法
    public Color GetPreviewColor() => m_Color;
    public bool ShouldShowPreview() => m_ShowPreview;
}
```

**注意**: 实际的预览UI需要通过GraphView扩展实现（高级主题）。

---

## 实践步骤

### 示例1: 创建彩色节点

创建一组不同颜色的节点：

```csharp
[Node("Red Node", "Colors", Color = "#FF6B6B")]
internal class RedNode : Node { }

[Node("Blue Node", "Colors", Color = "#4ECDC4")]
internal class BlueNode : Node { }

[Node("Green Node", "Colors", Color = "#95E1D3")]
internal class GreenNode : Node { }
```

### 示例2: 创建带丰富选项的节点

```csharp
[Node("Config Node", "Custom")]
internal class ConfigNode : Node
{
    [SerializeField] private string m_Name = "Default";
    [SerializeField] private float m_Speed = 1f;
    [SerializeField] private bool m_Enabled = true;
    [SerializeField] private Color m_Color = Color.white;

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption("Name", () => m_Name, v => m_Name = v)
            .Delayed()
            .WithTooltip("Node name")
            .Build();

        context.AddOption("Speed", () => m_Speed, v => m_Speed = Mathf.Max(0f, v))
            .WithTooltip("Speed multiplier")
            .Build();

        context.AddOption("Enabled", () => m_Enabled, v => m_Enabled = v).Build();
        context.AddOption("Color", () => m_Color, v => m_Color = v).Build();
    }
}
```

### 示例3: 创建聚合节点

创建一个可以接收多个输入的节点：

```csharp
[Node("Sum", "Math")]
internal class SumNode : Node, IFloatNode
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort<float>("Values")
            .WithCapacity(PortCapacity.Multiple)
            .Build();

        context.AddOutputPort<float>("Sum").Build();
    }

    public float EvaluateFloat(IPort port, CustomGraph graph)
    {
        float sum = 0f;
        foreach (var connection in graph.Connections)
        {
            if (connection.InputPort == m_ValuesInput)
            {
                sum += graph.EvaluateFloatPort(connection.OutputPort);
            }
        }
        return sum;
    }
}
```

---

## 高级主题：GraphView扩展

### 理解GraphView

GraphView是Unity UI Toolkit中用于显示图形的组件。GraphToolkit使用它来渲染节点和连接。

**注意**: 完全自定义GraphView需要深入了解Unity UI Toolkit和GraphToolkit的内部实现。

### 可能的扩展点

```csharp
// 自定义GraphView（高级）
public class CustomGraphView : GraphView
{
    public CustomGraphView()
    {
        // 自定义网格
        // 自定义缩放
        // 自定义选择
    }

    // 自定义节点创建
    public override Node CreateNode(INode node)
    {
        // 返回自定义的节点视图
    }
}
```

### 自定义节点视图

```csharp
// 自定义节点视图（高级）
public class CustomNodeView : Node
{
    public CustomNodeView(INode node)
    {
        // 添加自定义UI元素
        var preview = new VisualElement();
        preview.style.backgroundColor = Color.red;
        extensionContainer.Add(preview);
    }
}
```

**限制**: 当前版本的GraphToolkit可能不完全支持自定义GraphView。这是一个高级主题，需要等待Unity提供更多文档。

---

## 最佳实践

### 1. 一致的颜色方案

```csharp
// 定义颜色常量
public static class NodeColors
{
    public const string Input = "#4ECDC4";
    public const string Output = "#FF6B6B";
    public const string Math = "#95E1D3";
    public const string Logic = "#A78BFA";
}

[Node("Input", "IO", Color = NodeColors.Input)]
internal class InputNode : Node { }
```

### 2. 有意义的选项名称

```csharp
// ✅ 好的命名
context.AddOption("Blend Factor", () => m_BlendFactor, v => m_BlendFactor = v).Build();
context.AddOption("Use Alpha", () => m_UseAlpha, v => m_UseAlpha = v).Build();

// ❌ 避免的命名
context.AddOption("Value1", () => m_Value1, v => m_Value1 = v).Build();
context.AddOption("Flag", () => m_Flag, v => m_Flag = v).Build();
```

### 3. 添加提示文本

```csharp
context.AddOption("Threshold", () => m_Threshold, v => m_Threshold = v)
    .WithTooltip("Values below this threshold will be discarded")
    .Build();
```

### 4. 验证输入值

```csharp
context.AddOption("Count", () => m_Count, v => m_Count = Mathf.Max(1, v))
    .WithTooltip("Must be at least 1")
    .Build();
```

### 5. 使用延迟更新

```csharp
// 对于文本输入，使用Delayed()
context.AddOption("Description", () => m_Description, v => m_Description = v)
    .Delayed()
    .Build();
```

---

## 常见问题

### Q: 如何改变节点的大小？

A: 节点大小由内容自动决定。你可以通过添加更多端口或选项来增加节点大小。完全自定义大小需要扩展GraphView。

### Q: 可以在节点上显示图片吗？

A: 当前版本的GraphToolkit主要通过选项系统定制UI。显示图片需要扩展GraphView（高级主题）。

### Q: 如何创建自定义的端口样式？

A: 当前支持两种样式：Default（圆点）和Arrowhead（箭头）。自定义样式需要扩展GraphView。

### Q: 节点颜色支持渐变吗？

A: 当前只支持纯色（十六进制格式）。渐变需要自定义节点视图。

### Q: 如何隐藏某些选项？

A: 可以通过条件判断动态添加选项：

```csharp
protected override void OnDefineOptions(IOptionDefinitionContext context)
{
    context.AddOption("Mode", () => m_Mode, v => m_Mode = v).Build();

    // 只在特定模式下显示
    if (m_Mode == OperationMode.Advanced)
    {
        context.AddOption("Advanced Setting", () => m_AdvancedValue, v => m_AdvancedValue = v).Build();
    }
}
```

### Q: 可以在节点上添加按钮吗？

A: 当前版本不直接支持。需要通过扩展GraphView实现。

---

## 性能考虑

### 1. 避免过多的选项

```csharp
// ❌ 避免
protected override void OnDefineOptions(IOptionDefinitionContext context)
{
    for (int i = 0; i < 100; i++)
    {
        context.AddOption($"Value {i}", () => m_Values[i], v => m_Values[i] = v).Build();
    }
}

// ✅ 推荐
// 使用数组或列表，通过自定义编辑器显示
```

### 2. 缓存端口引用

```csharp
private IPort m_CachedInput;

protected override void OnDefinePorts(IPortDefinitionContext context)
{
    m_CachedInput = context.AddInputPort<float>("Input").Build();
}

// 使用缓存的引用，而不是每次查找
```

### 3. 延迟更新文本输入

```csharp
// 使用Delayed()避免每次按键都触发更新
context.AddOption("Text", () => m_Text, v => m_Text = v)
    .Delayed()
    .Build();
```

---

## 总结

教程6展示了GraphToolkit的UI定制选项：

1. **NodeAttribute**: 自定义节点颜色、描述
2. **选项系统**: 丰富的Inspector选项
3. **端口配置**: 容量、样式定制
4. **最佳实践**: 一致性、可用性

**限制**: 完全自定义GraphView需要更深入的扩展，这超出了当前教程的范围。

**下一步**: 教程7将进入实战项目 - 行为树系统！

---

## 练习题

### 练习1: 创建颜色主题
创建一组使用统一颜色主题的节点（输入、输出、处理、工具）。

### 练习2: 创建配置节点
创建一个包含10种不同类型选项的配置节点。

### 练习3: 创建聚合节点
创建一个可以接收任意数量输入的平均值节点。

---

**Sources**:
- Unity GraphToolkit Documentation
- Unity UI Toolkit Documentation
