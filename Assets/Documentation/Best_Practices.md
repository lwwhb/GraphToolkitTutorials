# GraphToolkit 最佳实践

基于教程1-3的经验总结

## 设计模式

### 1. 数据流图形模式

**适用场景**: 数据处理、材质生成、数学计算

**核心特点**:
- 按需评估（Pull模式）
- 递归端口评估
- 无明确执行顺序

**实现模式**:

```csharp
// 1. 定义评估接口
internal interface ICalculatorNode
{
    float Evaluate(IPort port, CalculatorGraph graph);
}

// 2. 实现节点
[Node("Add", "Calculator")]
internal class AddNode : Node, ICalculatorNode
{
    private IPort m_InputA;
    private IPort m_InputB;
    private IPort m_Output;

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        m_InputA = context.AddInputPort<float>("A").Build();
        m_InputB = context.AddInputPort<float>("B").Build();
        m_Output = context.AddOutputPort<float>("Result").Build();
    }

    public float Evaluate(IPort port, CalculatorGraph graph)
    {
        // 递归评估输入
        float a = EvaluateInput(m_InputA, graph);
        float b = EvaluateInput(m_InputB, graph);
        return a + b;
    }

    private float EvaluateInput(IPort inputPort, CalculatorGraph graph)
    {
        var connectedPort = graph.GetConnectedOutputPort(inputPort);
        if (connectedPort != null)
        {
            return graph.EvaluatePort(connectedPort);
        }
        return 0f; // 默认值
    }
}

// 3. 图形提供评估方法
[Graph("calc", GraphOptions.None)]
internal class CalculatorGraph : Graph
{
    public float EvaluatePort(IPort port)
    {
        if (port?.Node is ICalculatorNode calcNode)
        {
            return calcNode.Evaluate(port, this);
        }
        return 0f;
    }
}
```

**最佳实践**:
- ✅ 使用接口定义评估方法
- ✅ 在图形类中提供统一的评估入口
- ✅ 处理未连接端口的默认值
- ✅ 添加循环检测（生产环境）
- ❌ 避免在评估中修改图形结构

---

### 2. 执行流图形模式

**适用场景**: 行为树、对话系统、流程控制

**核心特点**:
- 主动执行（Push模式）
- 明确的执行顺序
- 编辑器→运行时转换

**实现模式**:

```csharp
// 1. 编辑器节点基类
internal abstract class TaskNode : Node
{
    protected IPort m_ExecutionIn;
    protected IPort m_ExecutionOut;

    protected void AddExecutionPorts(IPortDefinitionContext context)
    {
        m_ExecutionIn = context.AddInputPort("In")
            .WithConnectorUI(PortConnectorUI.Arrowhead)
            .Build();

        m_ExecutionOut = context.AddOutputPort("Out")
            .WithConnectorUI(PortConnectorUI.Arrowhead)
            .Build();
    }

    // 创建运行时节点
    public abstract Runtime.TaskRuntimeNode CreateRuntimeNode(TaskGraph graph);
}

// 2. 运行时节点（可序列化）
[Serializable]
public abstract class TaskRuntimeNode
{
    public int nextNodeIndex = -1;
}

// 3. 执行器接口
public interface ITaskExecutor
{
    IEnumerator Execute(TaskRuntimeGraph graph, int nodeIndex);
}

// 4. 具体执行器
public class DelayExecutor : ITaskExecutor
{
    public IEnumerator Execute(TaskRuntimeGraph graph, int nodeIndex)
    {
        var node = graph.GetNode<DelayNode>(nodeIndex);
        yield return new WaitForSeconds(node.duration);
        yield return node.nextNodeIndex; // 返回下一个节点索引
    }
}
```

**最佳实践**:
- ✅ 分离编辑器节点和运行时节点
- ✅ 使用执行器模式解耦逻辑
- ✅ 使用协程支持异步操作
- ✅ 使用箭头样式的执行流端口
- ❌ 避免在运行时节点中引用编辑器类型

---

### 3. 多类型接口模式

**适用场景**: 需要处理多种数据类型的图形

**实现模式**:

```csharp
// 定义多个接口
internal interface ITextureNode
{
    Texture2D EvaluateTexture(IPort port, TextureGraph graph);
}

internal interface IColorNode
{
    Color EvaluateColor(IPort port, TextureGraph graph);
}

internal interface IFloatNode
{
    float EvaluateFloat(IPort port, TextureGraph graph);
}

// 节点可以实现多个接口
[Node("Uniform Color", "Texture")]
internal class UniformColorNode : Node, ITextureNode, IColorNode
{
    public Texture2D EvaluateTexture(IPort port, TextureGraph graph)
    {
        Color color = EvaluateColor(port, graph);
        // 生成纹理...
    }

    public Color EvaluateColor(IPort port, TextureGraph graph)
    {
        return m_Color;
    }
}

// 图形根据端口类型调用相应方法
[Graph("texgraph", GraphOptions.None)]
internal class TextureGraph : Graph
{
    public Texture2D EvaluateTexturePort(IPort port)
    {
        if (port?.Node is ITextureNode node)
            return node.EvaluateTexture(port, this);
        return null;
    }

    public Color EvaluateColorPort(IPort port)
    {
        if (port?.Node is IColorNode node)
            return node.EvaluateColor(port, this);
        return Color.white;
    }
}
```

**最佳实践**:
- ✅ 为每种数据类型定义独立接口
- ✅ 节点可以实现多个接口
- ✅ 图形提供类型安全的评估方法
- ✅ 处理类型不匹配的情况

---

## 端口设计

### 端口命名规范

```csharp
// ✅ 好的命名
context.AddInputPort<float>("Duration").Build();
context.AddInputPort<Color>("Color A").Build();
context.AddOutputPort<Texture2D>("Texture").Build();

// ❌ 避免的命名
context.AddInputPort<float>("input1").Build();
context.AddInputPort<Color>("c").Build();
```

### 端口样式选择

```csharp
// 数据流端口 - 使用默认样式
context.AddInputPort<float>("Value").Build();

// 执行流端口 - 使用箭头样式
context.AddInputPort("In")
    .WithConnectorUI(PortConnectorUI.Arrowhead)
    .Build();
```

### 端口容量

```csharp
// 单连接（默认）
context.AddInputPort<float>("Value")
    .WithCapacity(PortCapacity.Single)
    .Build();

// 多连接（如事件系统）
context.AddOutputPort("OnComplete")
    .WithCapacity(PortCapacity.Multiple)
    .Build();
```

---

## 节点选项

### 基本选项

```csharp
protected override void OnDefineOptions(IOptionDefinitionContext context)
{
    // 简单值类型
    context.AddOption("Duration", () => m_Duration, v => m_Duration = v).Build();

    // 带验证的选项
    context.AddOption("Width",
        () => m_Width,
        v => m_Width = Mathf.Max(1, v)
    ).Build();

    // 枚举类型
    context.AddOption("Blend Mode",
        () => m_BlendMode,
        v => m_BlendMode = v
    ).Build();
}
```

### 延迟更新选项

```csharp
// 用于文本输入等需要延迟更新的场景
context.AddOption("Message",
    () => m_Message,
    v => m_Message = v
).Delayed().Build();
```

---

## ScriptedImporter集成

### 基本模式

```csharp
[ScriptedImporter(1, "myext")]
internal class MyGraphImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        // 1. 加载图形
        var graph = GraphDatabase.LoadGraphForImporter<MyGraph>(ctx.assetPath);

        // 2. 处理图形（评估、转换等）
        var result = ProcessGraph(graph);

        // 3. 添加主资产
        ctx.AddObjectToAsset("main", result);
        ctx.SetMainObject(result);

        // 4. 添加图形作为子资产
        ctx.AddObjectToAsset("graph", graph);
    }
}
```

### 版本控制

```csharp
// 修改版本号会触发重新导入
[ScriptedImporter(2, "myext")] // 从1改为2
internal class MyGraphImporter : ScriptedImporter
{
    // ...
}
```

---

## 错误处理

### 空值检查

```csharp
public Texture2D EvaluateTexture(IPort port, TextureGraph graph)
{
    if (port == null || port.Direction != PortDirection.Output)
    {
        Debug.LogError("Invalid port");
        return null;
    }

    var connectedPort = graph.GetConnectedOutputPort(m_Input);
    if (connectedPort == null)
    {
        Debug.LogWarning($"No connection to {m_Input.Name}");
        return CreateDefaultTexture();
    }

    return graph.EvaluateTexturePort(connectedPort);
}
```

### 类型检查

```csharp
public float EvaluatePort(IPort port)
{
    if (port?.Node is ICalculatorNode calcNode)
    {
        return calcNode.Evaluate(port, this);
    }

    Debug.LogError($"Node {port?.Node?.Name} does not implement ICalculatorNode");
    return 0f;
}
```

### 循环检测

```csharp
// 生产环境建议添加
private HashSet<IPort> m_EvaluatingPorts = new HashSet<IPort>();

public float EvaluatePort(IPort port)
{
    if (m_EvaluatingPorts.Contains(port))
    {
        Debug.LogError($"Circular dependency detected at port {port.Name}");
        return 0f;
    }

    m_EvaluatingPorts.Add(port);
    try
    {
        // 评估逻辑...
    }
    finally
    {
        m_EvaluatingPorts.Remove(port);
    }
}
```

---

## 性能优化

### 1. 缓存评估结果

```csharp
private Dictionary<IPort, Texture2D> m_TextureCache = new Dictionary<IPort, Texture2D>();

public Texture2D EvaluateTexturePort(IPort port)
{
    if (m_TextureCache.TryGetValue(port, out var cached))
    {
        return cached;
    }

    var result = /* 评估纹理 */;
    m_TextureCache[port] = result;
    return result;
}

// 在图形变化时清除缓存
protected override void OnGraphChanged()
{
    m_TextureCache.Clear();
}
```

### 2. 避免重复评估

```csharp
// ❌ 不好的做法
public Texture2D EvaluateTexture(IPort port, TextureGraph graph)
{
    var textureA = graph.EvaluateTexturePort(m_InputA); // 评估1次
    var textureB = graph.EvaluateTexturePort(m_InputB); // 评估1次

    // 如果多个节点连接到同一个输出，会重复评估
}

// ✅ 好的做法 - 在图形级别缓存
```

### 3. 延迟创建资源

```csharp
// ❌ 不好的做法 - 在OnDefinePorts中创建资源
protected override void OnDefinePorts(IPortDefinitionContext context)
{
    m_Texture = new Texture2D(256, 256); // 每次定义端口都创建
}

// ✅ 好的做法 - 在评估时创建
public Texture2D EvaluateTexture(IPort port, TextureGraph graph)
{
    if (m_Texture == null)
    {
        m_Texture = new Texture2D(256, 256);
    }
    return m_Texture;
}
```

---

## 调试技巧

### 1. 添加调试日志

```csharp
public float Evaluate(IPort port, CalculatorGraph graph)
{
    float a = EvaluateInput(m_InputA, graph);
    float b = EvaluateInput(m_InputB, graph);
    float result = a + b;

    #if UNITY_EDITOR
    Debug.Log($"[{Name}] {a} + {b} = {result}");
    #endif

    return result;
}
```

### 2. 可视化节点状态

```csharp
protected override void OnDefineOptions(IOptionDefinitionContext context)
{
    // 添加只读选项显示状态
    context.AddOption("Last Result", () => m_LastResult, null).Build();
    context.AddOption("Evaluation Count", () => m_EvaluationCount, null).Build();
}
```

### 3. 验证图形结构

```csharp
public void ValidateGraph()
{
    // 检查是否有输出节点
    bool hasOutput = Nodes.Any(n => n is OutputNode);
    if (!hasOutput)
    {
        Debug.LogWarning("Graph has no output node");
    }

    // 检查是否有孤立节点
    foreach (var node in Nodes)
    {
        bool hasConnections = node.Ports.Any(p => p.IsConnected);
        if (!hasConnections && !(node is StartNode))
        {
            Debug.LogWarning($"Node {node.Name} has no connections");
        }
    }
}
```

---

## 常见陷阱

### 1. 在OnDefinePorts中访问其他节点

```csharp
// ❌ 错误 - OnDefinePorts时图形可能未完全初始化
protected override void OnDefinePorts(IPortDefinitionContext context)
{
    var otherNode = Graph.Nodes.FirstOrDefault(n => n is OtherNode);
    // ...
}

// ✅ 正确 - 在评估时访问
public float Evaluate(IPort port, CalculatorGraph graph)
{
    var otherNode = graph.Nodes.FirstOrDefault(n => n is OtherNode);
    // ...
}
```

### 2. 忘记处理未连接的端口

```csharp
// ❌ 错误 - 可能返回null
public Texture2D EvaluateTexture(IPort port, TextureGraph graph)
{
    var connectedPort = graph.GetConnectedOutputPort(m_Input);
    return graph.EvaluateTexturePort(connectedPort); // connectedPort可能为null
}

// ✅ 正确
public Texture2D EvaluateTexture(IPort port, TextureGraph graph)
{
    var connectedPort = graph.GetConnectedOutputPort(m_Input);
    if (connectedPort != null)
    {
        return graph.EvaluateTexturePort(connectedPort);
    }
    return CreateDefaultTexture();
}
```

### 3. 在运行时节点中引用编辑器类型

```csharp
// ❌ 错误 - 运行时节点引用编辑器类型
[Serializable]
public class MyRuntimeNode : TaskRuntimeNode
{
    public MyEditorNode editorNode; // 无法序列化
}

// ✅ 正确 - 只存储数据
[Serializable]
public class MyRuntimeNode : TaskRuntimeNode
{
    public int nodeIndex;
    public string nodeName;
}
```

---

## 测试建议

### 单元测试

```csharp
[Test]
public void TestAddNode()
{
    var graph = ScriptableObject.CreateInstance<CalculatorGraph>();
    var constant1 = graph.AddNode<ConstantNode>();
    var constant2 = graph.AddNode<ConstantNode>();
    var addNode = graph.AddNode<AddNode>();

    // 设置值
    constant1.Value = 5f;
    constant2.Value = 3f;

    // 连接
    graph.Connect(constant1.OutputPort, addNode.InputA);
    graph.Connect(constant2.OutputPort, addNode.InputB);

    // 评估
    float result = graph.EvaluatePort(addNode.OutputPort);
    Assert.AreEqual(8f, result);
}
```

### 集成测试

```csharp
[Test]
public void TestGraphImport()
{
    // 创建测试图形文件
    string path = "Assets/Test.calc";
    // ... 创建图形 ...

    // 触发导入
    AssetDatabase.ImportAsset(path);

    // 验证结果
    var result = AssetDatabase.LoadAssetAtPath<CalculatorResult>(path);
    Assert.IsNotNull(result);
    Assert.AreEqual(expectedValue, result.result);
}
```

---

## 总结

### 数据流图形
- 使用接口定义评估方法
- 递归评估端口
- 处理默认值和错误情况
- 考虑添加缓存

### 执行流图形
- 分离编辑器和运行时节点
- 使用执行器模式
- 利用协程支持异步
- 使用箭头样式端口

### 通用建议
- 完善的错误处理
- 清晰的命名规范
- 适当的性能优化
- 充分的调试信息
- 编写单元测试

---

**参考教程**:
- 教程1: Hello Graph（数据流基础）
- 教程2: 数据流图形（复杂数据处理）
- 教程3: 执行流图形（流程控制）
