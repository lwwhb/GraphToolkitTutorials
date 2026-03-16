# 教程3: 执行流图形 - 任务系统

## 概述

本教程将带你创建一个基于执行流的任务系统。与前两个教程的数据流图形不同，执行流图形有明确的执行顺序，节点按顺序执行，适合实现行为树、对话系统、流程控制等场景。

## 学习目标

- 理解执行流图形与数据流图形的区别
- 掌握编辑器节点到运行时节点的转换
- 学会使用执行器模式
- 实现协程驱动的执行系统
- 将图形系统集成到MonoBehaviour

## 核心概念

### 数据流 vs 执行流

#### 数据流图形（教程1-2）
```
特点：
- 按需评估（Pull模式）
- 数据从输入流向输出
- 递归评估端口
- 无明确执行顺序
- 适合：数据处理、材质生成、数学计算

示例：
[Constant: 5] → [Add] → [Multiply] → [Output]
              ↑         ↑
[Constant: 3]─┘         │
[Constant: 2]───────────┘
```

#### 执行流图形（本教程）
```
特点：
- 主动执行（Push模式）
- 有明确的执行顺序
- 从起始节点开始
- 按连接顺序执行
- 适合：行为树、对话系统、流程控制

示例：
[Start] → [Log: "开始"] → [Delay: 1s] → [Log: "结束"]
```

### 编辑器节点 vs 运行时节点

这是执行流图形的关键设计：

#### 编辑器节点（Editor Node）
```csharp
// 在Unity编辑器中使用
// 继承自Unity.GraphToolkit.Editor.Node
// 不可序列化到场景
[Node("Delay", "Task")]
internal class DelayNode : TaskNode
{
    [SerializeField]
    private float m_Duration = 1f;

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        AddExecutionPorts(context);
    }

    // 创建对应的运行时节点
    public override Runtime.TaskRuntimeNode CreateRuntimeNode(TaskGraph graph)
    {
        return new Runtime.DelayNode { duration = m_Duration };
    }
}
```

#### 运行时节点（Runtime Node）
```csharp
// 在游戏运行时使用
// 纯数据类，可序列化
// 不依赖GraphToolkit
[Serializable]
public class DelayNode : TaskRuntimeNode
{
    public float duration = 1f;
}
```

**为什么需要分离？**
1. **序列化**: GraphToolkit的Node类不能序列化到场景
2. **性能**: 运行时不需要编辑器的复杂功能
3. **解耦**: 运行时代码不依赖编辑器DLL
4. **灵活性**: 可以在运行时动态创建和修改图形

### 执行器模式

每个运行时节点类型都有对应的执行器：

```csharp
// 执行器接口
public interface ITaskExecutor
{
    IEnumerator Execute(TaskRuntimeGraph graph, int nodeIndex);
}

// 延迟节点执行器
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

**执行器模式的优势**:
- 节点数据和执行逻辑分离
- 易于扩展新的执行行为
- 支持异步操作（协程）
- 便于单元测试

### 协程驱动执行

使用Unity协程来驱动图形执行：

```csharp
private IEnumerator ExecuteGraph()
{
    int currentNodeIndex = graph.startNodeIndex;

    while (currentNodeIndex >= 0)
    {
        var node = graph.GetNode(currentNodeIndex);
        var executor = GetExecutor(node.GetType());

        // 执行节点
        var coroutine = executor.Execute(graph, currentNodeIndex);
        while (coroutine.MoveNext())
        {
            if (coroutine.Current is int nextIndex)
            {
                currentNodeIndex = nextIndex;
            }
            else
            {
                yield return coroutine.Current; // WaitForSeconds等
            }
        }
    }
}
```

## 项目结构

```
03_ExecutionFlow/
├─ Editor/
│  ├── TaskGraph.cs                   # 编辑器图形
│  ├── TaskGraphImporter.cs           # 资产导入器
│  ├── Nodes/
│  │   ├── TaskNode.cs                # 编辑器节点基类
│  │   ├── StartNode.cs               # 起始节点
│  │   ├── DelayNode.cs               # 延迟节点
│  │   ├── LogNode.cs                 # 日志节点
│  │   └── BranchNode.cs              # 分支节点
│  └── Unity.GraphToolkit.Tutorials.ExecutionFlow.Editor.asmdef
│
├─ Runtime/
│  ├── TaskRuntimeGraph.cs            # 运行时图形
│  ├── TaskRuntimeNode.cs             # 运行时节点基类
│  ├── TaskExecutor.cs                # 任务执行器（MonoBehaviour）
│  ├── ITaskExecutor.cs               # 执行器接口
│  ├── Nodes/
│  │   ├── StartNode.cs               # 运行时起始节点
│  │   ├── DelayNode.cs               # 运行时延迟节点
│  │   ├── LogNode.cs                 # 运行时日志节点
│  │   └── BranchNode.cs              # 运行时分支节点
│  ├── Executors/
│  │   ├── StartExecutor.cs           # 起始节点执行器
│  │   ├── DelayExecutor.cs           # 延迟节点执行器
│  │   ├── LogExecutor.cs             # 日志节点执行器
│  │   └── BranchExecutor.cs          # 分支节点执行器
│  └── Unity.GraphToolkit.Tutorials.ExecutionFlow.Runtime.asmdef
│
└─ Examples/
    └── (将在Unity编辑器中创建.taskgraph文件)
```

## 节点详解

### StartNode（起始节点）

标记图形的执行起点：

**编辑器版本**:
```csharp
[Node("Start", "Task")]
internal class StartNode : TaskNode
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        // 只有输出端口
        m_ExecutionOut = context.AddOutputPort("Out")
            .WithConnectorUI(PortConnectorUI.Arrowhead) // 箭头样式
            .Build();
    }

    public override Runtime.TaskRuntimeNode CreateRuntimeNode(TaskGraph graph)
    {
        var runtimeNode = new Runtime.StartNode();
        var nextNode = GetNextNode(graph);
        runtimeNode.nextNodeIndex = nextNode != null ? graph.Nodes.IndexOf(nextNode) : -1;
        return runtimeNode;
    }
}
```

**运行时版本**:
```csharp
[Serializable]
public class StartNode : TaskRuntimeNode
{
    // 继承nextNodeIndex字段
}
```

**执行器**:
```csharp
public class StartExecutor : ITaskExecutor
{
    public IEnumerator Execute(TaskRuntimeGraph graph, int nodeIndex)
    {
        var node = graph.GetNode<StartNode>(nodeIndex);
        Debug.Log("Task graph started");
        yield return node.nextNodeIndex; // 立即执行下一个节点
    }
}
```

### DelayNode（延迟节点）

等待指定时间后继续执行：

**编辑器版本**:
```csharp
[Node("Delay", "Task")]
internal class DelayNode : TaskNode
{
    [SerializeField]
    private float m_Duration = 1f;

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        AddExecutionPorts(context); // 输入和输出端口
        context.AddInputPort<float>("Duration").Build(); // 可选的持续时间输入
    }

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption("Duration", () => m_Duration, v => m_Duration = Mathf.Max(0f, v)).Build();
    }
}
```

**运行时版本**:
```csharp
[Serializable]
public class DelayNode : TaskRuntimeNode
{
    public float duration = 1f;
}
```

**执行器**:
```csharp
public class DelayExecutor : ITaskExecutor
{
    public IEnumerator Execute(TaskRuntimeGraph graph, int nodeIndex)
    {
        var node = graph.GetNode<DelayNode>(nodeIndex);
        Debug.Log($"Delaying for {node.duration} seconds...");

        yield return new WaitForSeconds(node.duration); // Unity协程等待

        Debug.Log("Delay completed");
        yield return node.nextNodeIndex;
    }
}
```

### LogNode（日志节点）

输出日志信息：

**编辑器版本**:
```csharp
[Node("Log", "Task")]
internal class LogNode : TaskNode
{
    [SerializeField]
    private string m_Message = "Hello from Task Graph!";

    [SerializeField]
    private LogType m_LogType = LogType.Log;

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption("Message", () => m_Message, v => m_Message = v).Build();
        context.AddOption("Log Type", () => m_LogType, v => m_LogType = v).Build();
    }
}
```

**运行时版本**:
```csharp
[Serializable]
public class LogNode : TaskRuntimeNode
{
    public string message = "Hello from Task Graph!";
    public LogType logType = LogType.Log;
}
```

**执行器**:
```csharp
public class LogExecutor : ITaskExecutor
{
    public IEnumerator Execute(TaskRuntimeGraph graph, int nodeIndex)
    {
        var node = graph.GetNode<LogNode>(nodeIndex);

        switch (node.logType)
        {
            case LogType.Log:
                Debug.Log(node.message);
                break;
            case LogType.Warning:
                Debug.LogWarning(node.message);
                break;
            case LogType.Error:
                Debug.LogError(node.message);
                break;
        }

        yield return node.nextNodeIndex;
    }
}
```

### BranchNode（分支节点）

根据条件选择不同的执行路径：

**编辑器版本**:
```csharp
[Node("Branch", "Task")]
internal class BranchNode : TaskNode
{
    [SerializeField]
    private bool m_Condition = true;

    private IPort m_TrueOut;
    private IPort m_FalseOut;

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        m_ExecutionIn = context.AddInputPort("In")
            .WithConnectorUI(PortConnectorUI.Arrowhead).Build();

        context.AddInputPort<bool>("Condition").Build();

        m_TrueOut = context.AddOutputPort("True")
            .WithConnectorUI(PortConnectorUI.Arrowhead).Build();

        m_FalseOut = context.AddOutputPort("False")
            .WithConnectorUI(PortConnectorUI.Arrowhead).Build();
    }

    public override Runtime.TaskRuntimeNode CreateRuntimeNode(TaskGraph graph)
    {
        var runtimeNode = new Runtime.BranchNode { condition = m_Condition };

        // 获取True分支
        var truePort = graph.GetConnectedInputPort(m_TrueOut);
        runtimeNode.trueNodeIndex = truePort != null ? graph.Nodes.IndexOf(truePort.Node) : -1;

        // 获取False分支
        var falsePort = graph.GetConnectedInputPort(m_FalseOut);
        runtimeNode.falseNodeIndex = falsePort != null ? graph.Nodes.IndexOf(falsePort.Node) : -1;

        return runtimeNode;
    }
}
```

**运行时版本**:
```csharp
[Serializable]
public class BranchNode : TaskRuntimeNode
{
    public bool condition = true;
    public int trueNodeIndex = -1;
    public int falseNodeIndex = -1;
}
```

**执行器**:
```csharp
public class BranchExecutor : ITaskExecutor
{
    public IEnumerator Execute(TaskRuntimeGraph graph, int nodeIndex)
    {
        var node = graph.GetNode<BranchNode>(nodeIndex);
        Debug.Log($"Branch condition: {node.condition}");

        int nextIndex = node.condition ? node.trueNodeIndex : node.falseNodeIndex;

        if (nextIndex >= 0)
        {
            yield return nextIndex;
        }
    }
}
```

## TaskExecutor（任务执行器）

MonoBehaviour组件，负责执行运行时图形：

```csharp
public class TaskExecutor : MonoBehaviour
{
    [SerializeField]
    private TaskRuntimeGraph m_Graph;

    [SerializeField]
    private bool m_AutoStart = true;

    [SerializeField]
    private bool m_Loop = false;

    private Dictionary<System.Type, ITaskExecutor> m_Executors;

    private void Awake()
    {
        // 初始化执行器映射
        m_Executors = new Dictionary<System.Type, ITaskExecutor>
        {
            { typeof(StartNode), new StartExecutor() },
            { typeof(DelayNode), new DelayExecutor() },
            { typeof(LogNode), new LogExecutor() },
            { typeof(BranchNode), new BranchExecutor() }
        };
    }

    private void Start()
    {
        if (m_AutoStart && m_Graph != null)
        {
            StartExecution();
        }
    }

    public void StartExecution()
    {
        StartCoroutine(ExecuteGraph());
    }

    private IEnumerator ExecuteGraph()
    {
        do
        {
            int currentNodeIndex = m_Graph.startNodeIndex;

            while (currentNodeIndex >= 0)
            {
                var node = m_Graph.GetNode(currentNodeIndex);
                var executor = m_Executors[node.GetType()];

                var executionCoroutine = executor.Execute(m_Graph, currentNodeIndex);
                int nextNodeIndex = -1;

                while (executionCoroutine.MoveNext())
                {
                    var current = executionCoroutine.Current;

                    if (current is int index)
                    {
                        nextNodeIndex = index;
                    }
                    else
                    {
                        yield return current; // WaitForSeconds等
                    }
                }

                currentNodeIndex = nextNodeIndex;
            }

            Debug.Log("Task graph execution completed");

        } while (m_Loop);
    }
}
```

## 实践步骤

### 示例1: 简单的顺序执行

创建一个简单的任务序列：

```
[Start] → [Log: "开始任务"] → [Delay: 2s] → [Log: "任务完成"]
```

**步骤**:
1. 在Unity中创建.taskgraph文件
2. 添加节点：Start, Log, Delay, Log
3. 按顺序连接执行流端口
4. 配置Log节点的消息
5. 配置Delay节点的持续时间为2秒
6. 保存图形

**使用**:
1. 创建空GameObject
2. 添加TaskExecutor组件
3. 将.taskgraph资产拖到Graph字段
4. 运行游戏，观察Console输出

### 示例2: 条件分支

创建带有分支的任务：

```
[Start] → [Branch]
            ├─ True → [Log: "条件为真"]
            └─ False → [Log: "条件为假"]
```

**步骤**:
1. 添加Start, Branch, 两个Log节点
2. 连接Start到Branch的输入
3. 连接Branch的True输出到第一个Log
4. 连接Branch的False输出到第二个Log
5. 在Branch节点中设置Condition为true或false
6. 运行并观察不同的输出

### 示例3: 复杂流程

创建一个更复杂的任务流程：

```
[Start] → [Log: "开始"]
       ↓
    [Delay: 1s]
       ↓
    [Branch: Random]
       ├─ True → [Log: "路径A"] → [Delay: 0.5s]
       └─ False → [Log: "路径B"] → [Delay: 1.5s]
                                      ↓
                                [Log: "结束"]
```

## 扩展练习

### 练习1: 添加循环节点

实现一个LoopNode，重复执行指定次数：

```csharp
[Node("Loop", "Task")]
internal class LoopNode : TaskNode
{
    [SerializeField]
    private int m_Count = 3;

    private IPort m_LoopBody;
    private IPort m_Completed;

    // 实现端口定义和运行时节点创建
}

[Serializable]
public class LoopNode : TaskRuntimeNode
{
    public int count = 3;
    public int loopBodyIndex = -1;
    public int completedIndex = -1;
}

public class LoopExecutor : ITaskExecutor
{
    public IEnumerator Execute(TaskRuntimeGraph graph, int nodeIndex)
    {
        var node = graph.GetNode<LoopNode>(nodeIndex);

        for (int i = 0; i < node.count; i++)
        {
            Debug.Log($"Loop iteration {i + 1}/{node.count}");

            if (node.loopBodyIndex >= 0)
            {
                // 执行循环体
                yield return node.loopBodyIndex;
            }
        }

        yield return node.completedIndex;
    }
}
```

### 练习2: 添加随机分支节点

实现一个RandomBranchNode，随机选择一个输出：

```csharp
[Node("Random Branch", "Task")]
internal class RandomBranchNode : TaskNode
{
    private List<IPort> m_Outputs = new List<IPort>();

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        m_ExecutionIn = context.AddInputPort("In")
            .WithConnectorUI(PortConnectorUI.Arrowhead).Build();

        // 添加多个输出端口
        for (int i = 0; i < 3; i++)
        {
            var output = context.AddOutputPort($"Option {i + 1}")
                .WithConnectorUI(PortConnectorUI.Arrowhead).Build();
            m_Outputs.Add(output);
        }
    }
}
```

### 练习3: 添加并行执行节点

实现一个ParallelNode，同时执行多个分支：

```csharp
public class ParallelExecutor : ITaskExecutor
{
    public IEnumerator Execute(TaskRuntimeGraph graph, int nodeIndex)
    {
        var node = graph.GetNode<ParallelNode>(nodeIndex);

        // 启动所有分支的协程
        List<Coroutine> coroutines = new List<Coroutine>();
        foreach (var branchIndex in node.branchIndices)
        {
            if (branchIndex >= 0)
            {
                // 需要在MonoBehaviour上下文中启动协程
                // 这里需要传入MonoBehaviour引用
            }
        }

        // 等待所有分支完成
        // ...

        yield return node.nextNodeIndex;
    }
}
```

## 关键要点

1. **执行流 vs 数据流**: 执行流有明确的执行顺序，数据流按需评估
2. **编辑器/运行时分离**: 编辑器节点用于编辑，运行时节点用于执行
3. **执行器模式**: 将节点数据和执行逻辑分离
4. **协程驱动**: 使用Unity协程支持异步操作
5. **索引引用**: 运行时使用节点索引而不是对象引用

## 性能优化

### 1. 节点索引缓存

在构建运行时图形时，预先计算所有节点索引：

```csharp
public void BuildFromEditorGraph(TaskGraph editorGraph)
{
    // 第一遍：创建所有运行时节点
    foreach (var editorNode in editorGraph.Nodes)
    {
        var runtimeNode = CreateRuntimeNode(editorNode);
        nodes.Add(runtimeNode);
    }

    // 第二遍：解析连接和索引
    for (int i = 0; i < editorGraph.Nodes.Count; i++)
    {
        ResolveConnections(editorGraph.Nodes[i], nodes[i], editorGraph);
    }
}
```

### 2. 执行器复用

不要每次执行都创建新的执行器实例：

```csharp
// 好的做法
private Dictionary<Type, ITaskExecutor> m_Executors;

private void Awake()
{
    m_Executors = new Dictionary<Type, ITaskExecutor>
    {
        { typeof(DelayNode), new DelayExecutor() }
    };
}

// 避免
private IEnumerator ExecuteGraph()
{
    var executor = new DelayExecutor(); // 每次都创建新实例
}
```

### 3. 避免装箱

使用泛型方法避免装箱：

```csharp
// 好的做法
public T GetNode<T>(int index) where T : TaskRuntimeNode
{
    return nodes[index] as T;
}

// 避免
public object GetNode(int index)
{
    return nodes[index]; // 装箱
}
```

## 调试技巧

### 1. 添加调试日志

在执行器中添加详细的日志：

```csharp
public IEnumerator Execute(TaskRuntimeGraph graph, int nodeIndex)
{
    Debug.Log($"[{Time.time:F2}] Executing {node.nodeType} at index {nodeIndex}");
    // ...
    Debug.Log($"[{Time.time:F2}] Completed {node.nodeType}, next: {node.nextNodeIndex}");
}
```

### 2. 可视化当前执行节点

在TaskExecutor中添加：

```csharp
[SerializeField]
private int m_CurrentNodeIndex = -1;

private IEnumerator ExecuteGraph()
{
    while (currentNodeIndex >= 0)
    {
        m_CurrentNodeIndex = currentNodeIndex; // 在Inspector中显示
        // ...
    }
}
```

### 3. 断点调试

在执行器的Execute方法中设置断点，可以逐步调试图形执行。

## 常见问题

**Q: 为什么需要编辑器和运行时两套节点？**

A: GraphToolkit的Node类依赖编辑器DLL，不能序列化到场景。运行时节点是纯数据类，可以序列化且不依赖编辑器。

**Q: 如何在运行时动态修改图形？**

A: 可以直接修改TaskRuntimeGraph中的节点数据，但要注意在执行过程中修改可能导致不可预期的行为。

**Q: 可以在一个图形中有多个Start节点吗？**

A: 技术上可以，但TaskExecutor只会从第一个找到的Start节点开始执行。如果需要多个入口点，可以添加一个"Entry Point"参数。

**Q: 如何实现节点之间的数据传递？**

A: 当前实现没有数据传递机制。可以通过添加"黑板"（Blackboard）系统来实现，参见教程7（行为树系统）。

**Q: 执行器的协程返回int是什么意思？**

A: 返回int表示下一个要执行的节点索引。TaskExecutor会捕获这个值并跳转到对应节点。

## 下一步

在下一个教程中，我们将学习变量和子图系统，实现更复杂的图形复用和参数传递。

## 总结

本教程展示了执行流图形的完整实现：
- 编辑器节点定义图形结构
- 运行时节点存储可序列化数据
- 执行器实现具体的执行逻辑
- TaskExecutor驱动整个图形执行

这种架构清晰、可扩展，是实现复杂执行流系统（如行为树、对话系统）的基础。
