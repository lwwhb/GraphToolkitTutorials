# GraphToolkit API 参考文档

基于Unity 6000.5.a8的GraphToolkit模块

## 官方文档链接

- [Graph类](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Unity.GraphToolkit.Editor.Graph.html)
- [GraphDatabase类](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Unity.GraphToolkit.Editor.GraphDatabase.html)
- [IVariable接口](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Unity.GraphToolkit.Editor.IVariable.html)
- [NodeAttribute](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Unity.GraphToolkit.Editor.NodeAttribute.html)
- [IOptionBuilder](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Unity.GraphToolkit.Editor.IOptionBuilder.html)

---

## 核心类

### Graph

图形基类，所有自定义图形都继承自此类。

#### 属性

```csharp
// 节点集合
public IReadOnlyList<INode> Nodes { get; }

// 连接集合
public IReadOnlyList<IConnection> Connections { get; }

// 变量集合
public IReadOnlyList<IVariable> Variables { get; }

// 图形名称
public string Name { get; set; }
```

#### 方法

**节点管理**:
```csharp
// 添加节点
public T AddNode<T>() where T : Node

// 移除节点
public void RemoveNode(INode node)

// 通过索引获取节点
public INode GetNode(int index)

// 获取所有节点
public IReadOnlyList<INode> GetNodes()
```

**连接管理**:
```csharp
// 连接两个端口
public IConnection Connect(IPort outputPort, IPort inputPort)

// 断开连接
public void Disconnect(IConnection connection)

// 断开端口的所有连接
public void DisconnectPort(IPort port)
```

**变量管理**:
```csharp
// 添加变量
public IVariable AddVariable(string name, Type type, VariableKind kind)

// 移除变量
public void RemoveVariable(IVariable variable)

// 通过索引获取变量
public IVariable GetVariable(int index)

// 获取所有变量
public IReadOnlyList<IVariable> GetVariables()
```

#### 生命周期方法

```csharp
// 图形启用时调用
protected virtual void OnEnable()

// 图形禁用时调用
protected virtual void OnDisable()

// 图形发生变化时调用
protected virtual void OnGraphChanged()
```

#### 属性标记

```csharp
// 定义图形类型
[Graph("fileExtension", GraphOptions.None)]
public class MyGraph : Graph { }
```

**GraphOptions枚举**:
- `None` - 无特殊选项
- `AllowMultipleInstances` - 允许多个实例

---

### Node

节点基类，所有自定义节点都继承自此类。

#### 属性

```csharp
// 节点名称
public string Name { get; set; }

// 节点所属的图形
public Graph Graph { get; }

// 节点的所有端口
public IReadOnlyList<IPort> Ports { get; }

// 节点的所有选项
public IReadOnlyList<INodeOption> Options { get; }

// 节点GUID
public string Guid { get; }
```

#### 核心方法

```csharp
// 定义端口（必须重写）
protected abstract void OnDefinePorts(IPortDefinitionContext context)

// 定义选项（可选）
protected virtual void OnDefineOptions(IOptionDefinitionContext context)

// 节点启用时调用
protected virtual void OnEnable()

// 节点禁用时调用
protected virtual void OnDisable()

// 节点验证
protected virtual void OnValidate()
```

#### 属性标记

```csharp
// 定义节点类型
[Node("显示名称", "分类")]
public class MyNode : Node { }

// 指定可用的图形类型
[UseWithGraph(typeof(MyGraph))]
public class MyNode : Node { }
```

---

### 特殊节点类型

#### BlockNode

只能存在于ContextNode内部的节点。

```csharp
public abstract class BlockNode : Node
{
    // BlockNode特有的方法
}
```

#### ContextNode

可以包含BlockNode的容器节点。

```csharp
public abstract class ContextNode : Node
{
    // 获取内部的BlockNode
    public IReadOnlyList<BlockNode> GetBlocks()
}
```

---

## 接口

### IPort

端口接口，表示节点的输入或输出。

```csharp
public interface IPort
{
    // 端口名称
    string Name { get; }

    // 端口方向（Input/Output）
    PortDirection Direction { get; }

    // 端口所属的节点
    INode Node { get; }

    // 端口类型
    Type Type { get; }

    // 端口是否已连接
    bool IsConnected { get; }
}
```

**PortDirection枚举**:
- `Input` - 输入端口
- `Output` - 输出端口

---

### IPortDefinitionContext

端口定义上下文，用于在`OnDefinePorts`中定义端口。

```csharp
public interface IPortDefinitionContext
{
    // 添加输入端口
    IInputPortBuilder AddInputPort(string name)
    IInputPortBuilder AddInputPort<T>(string name)

    // 添加输出端口
    IOutputPortBuilder AddOutputPort(string name)
    IOutputPortBuilder AddOutputPort<T>(string name)
}
```

---

### IInputPortBuilder / IOutputPortBuilder

端口构建器，用于配置端口属性。

```csharp
public interface IInputPortBuilder
{
    // 设置端口连接器UI样式
    IInputPortBuilder WithConnectorUI(PortConnectorUI ui)

    // 设置端口容量（单连接/多连接）
    IInputPortBuilder WithCapacity(PortCapacity capacity)

    // 构建端口
    IPort Build()
}
```

**PortConnectorUI枚举**:
- `Default` - 默认样式（圆点）
- `Arrowhead` - 箭头样式（用于执行流）

**PortCapacity枚举**:
- `Single` - 单连接
- `Multiple` - 多连接

---

### IVariable

变量接口，表示图形中的变量。

```csharp
public interface IVariable
{
    // 变量名称
    string Name { get; set; }

    // 变量类型
    Type Type { get; }

    // 变量种类
    VariableKind Kind { get; }

    // 变量值
    object Value { get; set; }

    // 变量GUID
    string Guid { get; }
}
```

**VariableKind枚举**:
- `Local` - 局部变量
- `Input` - 输入变量
- `Output` - 输出变量

---

### IConstantNode

常量节点接口。

```csharp
public interface IConstantNode
{
    // 常量值
    object Value { get; set; }
}
```

---

### IVariableNode

变量节点接口。

```csharp
public interface IVariableNode
{
    // 关联的变量
    IVariable Variable { get; set; }
}
```

---

### ISubgraphNode

子图节点接口。

```csharp
public interface ISubgraphNode
{
    // 子图资产
    Graph Subgraph { get; set; }
}
```

---

## 选项系统

### IOptionDefinitionContext

选项定义上下文，用于在`OnDefineOptions`中定义节点选项。

```csharp
public interface IOptionDefinitionContext
{
    // 添加选项
    IOptionBuilder AddOption<T>(string name, Func<T> getter, Action<T> setter)
}
```

### IOptionBuilder

选项构建器，用于配置选项属性。

```csharp
public interface IOptionBuilder
{
    // 延迟更新（用于文本输入等）
    IOptionBuilder Delayed()

    // 设置选项标签
    IOptionBuilder WithLabel(string label)

    // 设置选项提示
    IOptionBuilder WithTooltip(string tooltip)

    // 构建选项
    INodeOption Build()
}
```

---

## GraphDatabase

图形数据库，用于管理图形资产。

```csharp
public static class GraphDatabase
{
    // 创建新图形
    public static T CreateGraph<T>() where T : Graph

    // 加载图形（通过路径）
    public static T LoadGraph<T>(string path) where T : Graph

    // 为导入器加载图形
    public static T LoadGraphForImporter<T>(string assetPath) where T : Graph

    // 保存图形
    public static void SaveGraph(Graph graph)

    // 在Project窗口中提示创建图形
    public static void PromptInProjectWindow<T>() where T : Graph
}
```

---

## 连接系统

### IConnection

连接接口，表示两个端口之间的连接。

```csharp
public interface IConnection
{
    // 输出端口
    IPort OutputPort { get; }

    // 输入端口
    IPort InputPort { get; }

    // 连接所属的图形
    Graph Graph { get; }
}
```

---

## 实用扩展方法

### INodeExtensions

```csharp
public static class INodeExtensions
{
    // 从端口获取节点
    public static INode GetNode(this IPort port)

    // 获取节点的所有输入端口
    public static IEnumerable<IPort> GetInputPorts(this INode node)

    // 获取节点的所有输出端口
    public static IEnumerable<IPort> GetOutputPorts(this INode node)
}
```

---

## 使用示例

### 创建自定义图形

```csharp
using Unity.GraphToolkit.Editor;

[Graph("mygraph", GraphOptions.None)]
public class MyGraph : Graph
{
    protected override void OnEnable()
    {
        base.OnEnable();
        // 初始化逻辑
    }

    protected override void OnGraphChanged()
    {
        base.OnGraphChanged();
        // 图形变化时的逻辑
    }
}
```

### 创建自定义节点

```csharp
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Node("My Node", "Custom")]
[UseWithGraph(typeof(MyGraph))]
public class MyNode : Node
{
    [SerializeField]
    private float m_Value = 1.0f;

    private IPort m_InputPort;
    private IPort m_OutputPort;

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        m_InputPort = context.AddInputPort<float>("Input").Build();
        m_OutputPort = context.AddOutputPort<float>("Output").Build();
    }

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption("Value", () => m_Value, v => m_Value = v)
            .WithTooltip("The node value")
            .Build();
    }
}
```

### 创建执行流节点

```csharp
[Node("Execute", "Flow")]
public class ExecuteNode : Node
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort("In")
            .WithConnectorUI(PortConnectorUI.Arrowhead)
            .Build();

        context.AddOutputPort("Out")
            .WithConnectorUI(PortConnectorUI.Arrowhead)
            .Build();
    }
}
```

### 使用变量系统

```csharp
public class MyGraph : Graph
{
    public void CreateVariable()
    {
        // 创建局部变量
        var localVar = AddVariable("MyVar", typeof(float), VariableKind.Local);
        localVar.Value = 10.0f;

        // 创建输入变量
        var inputVar = AddVariable("Input", typeof(int), VariableKind.Input);

        // 创建输出变量
        var outputVar = AddVariable("Output", typeof(string), VariableKind.Output);
    }
}
```

### 遍历图形

```csharp
public void TraverseGraph(Graph graph)
{
    // 遍历所有节点
    foreach (var node in graph.Nodes)
    {
        Debug.Log($"Node: {node.Name}");

        // 遍历节点的所有端口
        foreach (var port in node.Ports)
        {
            Debug.Log($"  Port: {port.Name} ({port.Direction})");
        }
    }

    // 遍历所有连接
    foreach (var connection in graph.Connections)
    {
        Debug.Log($"Connection: {connection.OutputPort.Node.Name}.{connection.OutputPort.Name} -> {connection.InputPort.Node.Name}.{connection.InputPort.Name}");
    }
}
```

---

## 最佳实践

### 1. 端口命名

使用清晰、描述性的端口名称：
```csharp
// 好的命名
context.AddInputPort<float>("Duration").Build();
context.AddOutputPort<Texture2D>("Result").Build();

// 避免
context.AddInputPort<float>("In1").Build();
context.AddOutputPort<Texture2D>("Out").Build();
```

### 2. 选项配置

为选项添加提示和标签：
```csharp
context.AddOption("Speed", () => m_Speed, v => m_Speed = v)
    .WithLabel("Movement Speed")
    .WithTooltip("The speed at which the object moves")
    .Build();
```

### 3. 节点验证

实现`OnValidate`来验证节点状态：
```csharp
protected override void OnValidate()
{
    base.OnValidate();

    if (m_Duration < 0)
    {
        Debug.LogWarning($"Duration cannot be negative in node {Name}");
        m_Duration = 0;
    }
}
```

### 4. 图形变化响应

使用`OnGraphChanged`来响应图形变化：
```csharp
protected override void OnGraphChanged()
{
    base.OnGraphChanged();

    // 重新计算缓存
    // 更新依赖关系
    // 验证图形完整性
}
```

### 5. 资源清理

在`OnDisable`中清理资源：
```csharp
protected override void OnDisable()
{
    base.OnDisable();

    // 清理临时纹理
    // 释放缓存
    // 断开事件监听
}
```

---

## 常见模式

### 递归端口评估（数据流）

```csharp
public float EvaluatePort(IPort port)
{
    if (port == null || port.Direction != PortDirection.Output)
        return 0f;

    var node = port.Node as ICalculatorNode;
    if (node != null)
    {
        return node.Evaluate(port, this);
    }

    return 0f;
}
```

### 执行流遍历

```csharp
public INode GetNextNode(IPort executionOut)
{
    foreach (var connection in Connections)
    {
        if (connection.OutputPort == executionOut)
        {
            return connection.InputPort.Node;
        }
    }
    return null;
}
```

### 节点索引映射

```csharp
public int GetNodeIndex(INode node)
{
    return Nodes.IndexOf(node);
}

public INode GetNodeByIndex(int index)
{
    if (index >= 0 && index < Nodes.Count)
    {
        return Nodes[index];
    }
    return null;
}
```

---

## 限制和注意事项

1. **序列化限制**: GraphToolkit的Node类不能直接序列化到场景，需要转换为运行时数据类
2. **编辑器专用**: 大部分API只能在编辑器中使用，运行时需要自定义数据结构
3. **循环检测**: 需要自行实现循环连接检测
4. **性能考虑**: 大型图形的遍历和评估需要优化
5. **版本兼容**: API可能随Unity版本变化，需要关注更新

---

## 参考资源

- [Unity 6000.5 Script Reference](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/index.html)
- [Graph类文档](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Unity.GraphToolkit.Editor.Graph.html)
- [GraphDatabase类文档](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Unity.GraphToolkit.Editor.GraphDatabase.html)
- [IVariable接口文档](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Unity.GraphToolkit.Editor.IVariable.html)

---

**最后更新**: 2026-03-16
**Unity版本**: 6000.5.a8
**GraphToolkit版本**: 内置模块
