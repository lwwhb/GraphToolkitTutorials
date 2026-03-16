# 教程7: 行为树系统

## 概述

本教程将实现一个完整的AI行为树系统，这是游戏开发中最常用的AI决策系统之一。通过本教程，你将学习如何使用GraphToolkit构建复杂的执行流图形，并实现生产级的行为树运行时系统。

### 学习目标

- 理解行为树的核心概念和设计模式
- 掌握Composite、Decorator、Leaf节点的实现
- 学习黑板系统（Blackboard）的设计
- 实现完整的运行时执行系统
- 掌握行为树的调试和可视化技巧

### 前置知识

- 完成教程1-3（必须）
- 了解基本的AI概念
- 熟悉协程和状态机

---

## 行为树基础

### 什么是行为树？

行为树（Behavior Tree）是一种用于AI决策的树形结构，它通过组合简单的行为节点来实现复杂的AI逻辑。

**核心特点**:
- 模块化：每个节点职责单一
- 可复用：节点可以在不同树中复用
- 易于调试：树形结构清晰可见
- 动态执行：运行时可以动态修改

### 节点类型

#### 1. Composite节点（组合节点）
控制子节点的执行顺序和逻辑。

**Sequence（序列）**:
- 按顺序执行子节点
- 任一子节点失败则整体失败
- 所有子节点成功则整体成功
- 用途：执行一系列必须完成的任务

**Selector（选择器）**:
- 按顺序执行子节点
- 任一子节点成功则整体成功
- 所有子节点失败则整体失败
- 用途：尝试多个备选方案

**Parallel（并行）**:
- 同时执行所有子节点
- 根据策略决定成功/失败
- 用途：同时执行多个任务

#### 2. Decorator节点（修饰节点）
修改子节点的行为或结果。

**Inverter（反转器）**:
- 反转子节点的结果
- 成功→失败，失败→成功

**Repeater（重复器）**:
- 重复执行子节点N次
- 可设置无限循环

**UntilFail（直到失败）**:
- 重复执行直到子节点失败

#### 3. Leaf节点（叶子节点）
实际执行具体任务的节点。

**Action（动作）**:
- 执行具体的行为
- 例如：移动、攻击、播放动画

**Condition（条件）**:
- 检查某个条件
- 例如：检测敌人、检查血量

---

## 项目结构

```
Assets/Tutorials/07_BehaviorTree/
├─ Editor/
│  ├─ BehaviorTreeGraph.cs              # 行为树图形定义
│  ├─ BehaviorTreeImporter.cs           # 资产导入器
│  ├─ Nodes/
│  │  ├─ BTNode.cs                      # 节点基类
│  │  ├─ RootNode.cs                    # 根节点
│  │  ├─ Composites/
│  │  │  └─ CompositeNodes.cs           # Sequence, Selector, Parallel
│  │  ├─ Decorators/
│  │  │  └─ DecoratorNodes.cs           # Inverter, Repeater
│  │  └─ Leaves/
│  │     └─ LeafNodes.cs                # Action, Condition
│  └─ Unity.GraphToolkit.Tutorials.BehaviorTree.Editor.asmdef
├─ Runtime/
│  ├─ BehaviorTreeRuntime.cs            # 运行时图形
│  ├─ BehaviorTreeRunner.cs             # MonoBehaviour执行器
│  ├─ Blackboard.cs                     # 黑板系统
│  ├─ BTRuntimeNode.cs                  # 运行时节点基类
│  ├─ IBTExecutor.cs                    # 执行器接口
│  ├─ Nodes/
│  │  └─ RuntimeNodes.cs                # 运行时节点定义
│  ├─ Executors/
│  │  ├─ CompositeExecutors.cs          # 组合节点执行器
│  │  ├─ DecoratorExecutors.cs          # 修饰节点执行器
│  │  └─ LeafExecutors.cs               # 叶子节点执行器
│  └─ Unity.GraphToolkit.Tutorials.BehaviorTree.Runtime.asmdef
└─ Examples/
   ├─ PatrolBehavior.bt                 # 巡逻行为示例
   └─ CombatBehavior.bt                 # 战斗行为示例
```

---

## 核心实现

### 1. 编辑器节点基类

```csharp
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.BehaviorTree
{
    /// <summary>
    /// 行为树节点基类
    /// </summary>
    internal abstract class BTNode : Node
    {
        /// <summary>
        /// 节点状态
        /// </summary>
        public enum NodeStatus
        {
            Success,    // 成功
            Failure,    // 失败
            Running     // 运行中
        }

        protected IPort m_InputPort;
        protected IPort m_OutputPort;

        /// <summary>
        /// 添加执行流端口
        /// </summary>
        protected void AddExecutionPorts(IPortDefinitionContext context)
        {
            m_InputPort = context.AddInputPort("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            m_OutputPort = context.AddOutputPort("Out")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        /// <summary>
        /// 创建运行时节点
        /// </summary>
        public abstract Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph);

        /// <summary>
        /// 获取节点索引
        /// </summary>
        public int GetNodeIndex(BehaviorTreeGraph graph)
        {
            return graph.Nodes.IndexOf(this);
        }

        /// <summary>
        /// 获取连接的子节点
        /// </summary>
        protected BTNode GetChildNode(BehaviorTreeGraph graph)
        {
            if (m_OutputPort == null) return null;

            var connectedPort = graph.GetConnectedInputPort(m_OutputPort);
            if (connectedPort != null && connectedPort.Node is BTNode btNode)
            {
                return btNode;
            }
            return null;
        }
    }
}
```

### 2. Composite节点实现

```csharp
/// <summary>
/// Sequence节点 - 顺序执行所有子节点
/// </summary>
[Node("Sequence", "Behavior Tree/Composite")]
[UseWithGraph(typeof(BehaviorTreeGraph))]
internal class SequenceNode : BTNode
{
    private List<IPort> m_ChildPorts = new List<IPort>();

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        m_InputPort = context.AddInputPort("In")
            .WithConnectorUI(PortConnectorUI.Arrowhead)
            .Build();

        // 添加多个子节点端口
        for (int i = 0; i < 3; i++)
        {
            var port = context.AddOutputPort($"Child {i + 1}")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
            m_ChildPorts.Add(port);
        }
    }

    public List<BTNode> GetChildren(BehaviorTreeGraph graph)
    {
        var children = new List<BTNode>();
        foreach (var port in m_ChildPorts)
        {
            var connectedPort = graph.GetConnectedInputPort(port);
            if (connectedPort != null && connectedPort.Node is BTNode btNode)
            {
                children.Add(btNode);
            }
        }
        return children;
    }

    public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
    {
        var runtimeNode = new Runtime.SequenceNode();
        
        var children = GetChildren(graph);
        runtimeNode.childIndices = new int[children.Count];
        for (int i = 0; i < children.Count; i++)
        {
            runtimeNode.childIndices[i] = children[i].GetNodeIndex(graph);
        }

        return runtimeNode;
    }
}
```

### 3. 黑板系统

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace GraphToolkitTutorials.BehaviorTree.Runtime
{
    /// <summary>
    /// 黑板系统 - 存储行为树的共享数据
    /// </summary>
    public class Blackboard
    {
        private Dictionary<string, object> m_Data = new Dictionary<string, object>();

        /// <summary>
        /// 设置值
        /// </summary>
        public void SetValue<T>(string key, T value)
        {
            m_Data[key] = value;
        }

        /// <summary>
        /// 获取值
        /// </summary>
        public T GetValue<T>(string key, T defaultValue = default)
        {
            if (m_Data.TryGetValue(out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// 检查是否存在
        /// </summary>
        public bool HasValue(string key)
        {
            return m_Data.ContainsKey(key);
        }

        /// <summary>
        /// 移除值
        /// </summary>
        public void RemoveValue(string key)
        {
            m_Data.Remove(key);
        }

        /// <summary>
        /// 清空所有数据
        /// </summary>
        public void Clear()
        {
            m_Data.Clear();
        }
    }
}
```

### 4. 运行时执行器

```csharp
using System.Collections;
using UnityEngine;

namespace GraphToolkitTutorials.BehaviorTree.Runtime
{
    /// <summary>
    /// 行为树运行器
    /// </summary>
    public class BehaviorTreeRunner : MonoBehaviour
    {
        [SerializeField]
        private BehaviorTreeGraph m_GraphAsset;

        [SerializeField]
        private float m_TickInterval = 0.1f; // 更新间隔

        private BehaviorTreeRuntime m_RuntimeGraph;
        private Blackboard m_Blackboard;
        private Dictionary<System.Type, IBTExecutor> m_Executors;
        private bool m_IsRunning;

        void Start()
        {
            if (m_GraphAsset != null)
            {
                InitializeTree();
                StartTree();
            }
        }

        void InitializeTree()
        {
            // 创建运行时图形
            m_RuntimeGraph = m_GraphAsset.CreateRuntimeGraph();
            
            // 创建黑板
            m_Blackboard = new Blackboard();
            
            // 注册执行器
            m_Executors = new Dictionary<System.Type, IBTExecutor>
            {
                { typeof(SequenceNode), new SequenceExecutor() },
                { typeof(SelectorNode), new SelectorExecutor() },
                { typeof(ParallelNode), new ParallelExecutor() },
                { typeof(InverterNode), new InverterExecutor() },
                { typeof(RepeaterNode), new RepeaterExecutor() },
                { typeof(ActionNode), new ActionExecutor() },
                { typeof(ConditionNode), new ConditionExecutor() }
            };
        }

        public void StartTree()
        {
            if (!m_IsRunning)
            {
                m_IsRunning = true;
                StartCoroutine(TreeUpdateLoop());
            }
        }

        public void StopTree()
        {
            m_IsRunning = false;
        }

        IEnumerator TreeUpdateLoop()
        {
            while (m_IsRunning)
            {
                // 从根节点开始执行
                var rootIndex = m_RuntimeGraph.GetRootNodeIndex();
                if (rootIndex >= 0)
                {
                    yield return ExecuteNode(rootIndex);
                }

                yield return new WaitForSeconds(m_TickInterval);
            }
        }

        IEnumerator ExecuteNode(int nodeIndex)
        {
            var node = m_RuntimeGraph.GetNode(nodeIndex);
            var nodeType = node.GetType();

            if (m_Executors.TryGetValue(nodeType, out var executor))
            {
                yield return executor.Execute(m_RuntimeGraph, m_Blackboard, nodeIndex);
            }
            else
            {
                Debug.LogWarning($"No executor found for node type: {nodeType}");
            }
        }

        /// <summary>
        /// 获取黑板
        /// </summary>
        public Blackboard GetBlackboard()
        {
            return m_Blackboard;
        }
    }
}
```

### 5. Sequence执行器

```csharp
/// <summary>
/// Sequence节点执行器
/// </summary>
public class SequenceExecutor : IBTExecutor
{
    public IEnumerator Execute(BehaviorTreeRuntime graph, Blackboard blackboard, int nodeIndex)
    {
        var node = graph.GetNode<SequenceNode>(nodeIndex);
        
        // 按顺序执行所有子节点
        foreach (var childIndex in node.childIndices)
        {
            if (childIndex < 0) continue;

            // 执行子节点
            var childNode = graph.GetNode(childIndex);
            var executor = GetExecutorForNode(childNode);
            
            NodeStatus status = NodeStatus.Running;
            yield return executor.Execute(graph, blackboard, childIndex, out status);

            // 如果子节点失败，整个Sequence失败
            if (status == NodeStatus.Failure)
            {
                yield return NodeStatus.Failure;
                yield break;
            }

            // 如果子节点运行中，等待
            while (status == NodeStatus.Running)
            {
                yield return null;
                yield return executor.Execute(graph, blackboard, childIndex, out status);
            }
        }

        // 所有子节点成功，Sequence成功
        yield return NodeStatus.Success;
    }
}
```

---

## 实战示例

### 示例1: 巡逻AI

创建一个简单的巡逻AI行为树：

```
[Root]
└─ [Sequence]
   ├─ [Condition: 有巡逻点]
   ├─ [Action: 移动到下一个巡逻点]
   ├─ [Action: 等待2秒]
   └─ [Action: 更新巡逻点索引]
```

**实现步骤**:

1. 创建行为树资产
```csharp
// 在Unity编辑器中
// Assets -> Create -> Behavior Tree -> New Behavior Tree
```

2. 添加节点并连接

3. 配置黑板变量
```csharp
blackboard.SetValue("patrolPoints", patrolPoints);
blackboard.SetValue("currentPatrolIndex", 0);
```

4. 运行行为树
```csharp
var runner = gameObject.AddComponent<BehaviorTreeRunner>();
runner.StartTree();
```

### 示例2: 战斗AI

创建一个更复杂的战斗AI：

```
[Root]
└─ [Selector]
   ├─ [Sequence] (逃跑)
   │  ├─ [Condition: 血量 < 20%]
   │  └─ [Action: 逃跑]
   ├─ [Sequence] (攻击)
   │  ├─ [Condition: 发现敌人]
   │  ├─ [Condition: 距离 < 攻击范围]
   │  └─ [Action: 攻击]
   └─ [Sequence] (巡逻)
      ├─ [Action: 移动到巡逻点]
      └─ [Action: 等待]
```

**关键代码**:

```csharp
// 条件节点：检查血量
public class CheckHealthCondition : ConditionNode
{
    public float threshold = 0.2f;

    public override NodeStatus Evaluate(Blackboard blackboard)
    {
        var health = blackboard.GetValue<float>("health", 1.0f);
        var maxHealth = blackboard.GetValue<float>("maxHealth", 100f);
        
        return (health / maxHealth) < threshold ? NodeStatus.Success : NodeStatus.Failure;
    }
}

// 动作节点：攻击
public class AttackAction : ActionNode
{
    public float damage = 10f;

    public override IEnumerator Execute(Blackboard blackboard)
    {
        var target = blackboard.GetValue<GameObject>("target");
        if (target != null)
        {
            // 执行攻击逻辑
            var enemy = target.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }

            // 播放攻击动画
            yield return new WaitForSeconds(0.5f);
            
            yield return NodeStatus.Success;
        }
        else
        {
            yield return NodeStatus.Failure;
        }
    }
}
```

---

## 高级特性

### 1. 动态优先级

使用Selector实现动态优先级系统：

```csharp
[Selector]
├─ [高优先级任务]
├─ [中优先级任务]
└─ [低优先级任务]
```

### 2. 中断机制

实现可中断的行为：

```csharp
public class InterruptibleAction : ActionNode
{
    public override IEnumerator Execute(Blackboard blackboard)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // 检查中断条件
            if (blackboard.GetValue<bool>("shouldInterrupt", false))
            {
                yield return NodeStatus.Failure;
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return NodeStatus.Success;
    }
}
```

### 3. 并行任务

使用Parallel节点同时执行多个任务：

```csharp
[Parallel] (策略: 所有成功)
├─ [Action: 移动]
├─ [Action: 播放动画]
└─ [Action: 播放音效]
```

---

## 调试技巧

### 1. 可视化当前执行节点

```csharp
public class BehaviorTreeDebugger : MonoBehaviour
{
    private BehaviorTreeRunner m_Runner;
    private int m_CurrentNodeIndex = -1;

    void OnDrawGizmos()
    {
        if (m_Runner != null && m_CurrentNodeIndex >= 0)
        {
            // 在Scene视图中显示当前执行的节点
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2, 0.5f);
        }
    }
}
```

### 2. 日志记录

```csharp
public class LoggingExecutor : IBTExecutor
{
    private IBTExecutor m_InnerExecutor;

    public IEnumerator Execute(BehaviorTreeRuntime graph, Blackboard blackboard, int nodeIndex)
    {
        var node = graph.GetNode(nodeIndex);
        Debug.Log($"[BT] Executing: {node.GetType().Name}");

        yield return m_InnerExecutor.Execute(graph, blackboard, nodeIndex);

        Debug.Log($"[BT] Completed: {node.GetType().Name}");
    }
}
```

### 3. 性能分析

```csharp
public class PerformanceMonitor
{
    private Dictionary<string, float> m_NodeExecutionTimes = new Dictionary<string, float>();

    public void RecordExecution(string nodeName, float time)
    {
        if (!m_NodeExecutionTimes.ContainsKey(nodeName))
        {
            m_NodeExecutionTimes[nodeName] = 0f;
        }
        m_NodeExecutionTimes[nodeName] += time;
    }

    public void PrintStats()
    {
        foreach (var kvp in m_NodeExecutionTimes)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value:F3}ms");
        }
    }
}
```

---

## 性能优化

### 1. 对象池

```csharp
public class NodeExecutorPool
{
    private Dictionary<System.Type, Queue<IBTExecutor>> m_Pools = new Dictionary<System.Type, Queue<IBTExecutor>>();

    public IBTExecutor Get(System.Type type)
    {
        if (!m_Pools.ContainsKey(type))
        {
            m_Pools[type] = new Queue<IBTExecutor>();
        }

        var pool = m_Pools[type];
        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }

        return (IBTExecutor)System.Activator.CreateInstance(type);
    }

    public void Return(System.Type type, IBTExecutor executor)
    {
        m_Pools[type].Enqueue(executor);
    }
}
```

### 2. 缓存优化

```csharp
public class CachedBlackboard : Blackboard
{
    private Dictionary<string, object> m_Cache = new Dictionary<string, object>();

    public override T GetValue<T>(string key, T defaultValue = default)
    {
        // 使用缓存减少字典查找
        if (m_Cache.TryGetValue(key, out var cached))
        {
            return (T)cached;
        }

        var value = base.GetValue(key, defaultValue);
        m_Cache[key] = value;
        return value;
    }
}
```

---

## 常见问题

### Q1: 如何实现条件检查？

A: 使用Condition节点：

```csharp
[Node("Check Distance", "Behavior Tree/Condition")]
internal class CheckDistanceNode : BTNode
{
    [SerializeField]
    private float m_MaxDistance = 5f;

    public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
    {
        return new Runtime.CheckDistanceCondition
        {
            maxDistance = m_MaxDistance
        };
    }
}
```

### Q2: 如何处理异步操作？

A: 使用协程和Running状态：

```csharp
public override IEnumerator Execute(Blackboard blackboard)
{
    // 开始异步操作
    var operation = StartAsyncOperation();

    // 等待完成
    while (!operation.isDone)
    {
        yield return NodeStatus.Running;
    }

    yield return NodeStatus.Success;
}
```

### Q3: 如何实现行为树的保存和加载？

A: 使用ScriptedImporter：

```csharp
[ScriptedImporter(1, "bt")]
public class BehaviorTreeImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var graph = GraphDatabase.LoadGraphForImporter<BehaviorTreeGraph>(assetPath);
        var runtimeGraph = graph.CreateRuntimeGraph();
        
        ctx.AddObjectToAsset("main", runtimeGraph);
        ctx.SetMainObject(runtimeGraph);
    }
}
```

---

## 练习题

### 练习1: 实现一个简单的巡逻AI
创建一个在多个点之间巡逻的AI。

**提示**:
- 使用Sequence节点
- 使用黑板存储巡逻点列表
- 实现移动和等待动作

### 练习2: 实现战斗AI
创建一个能够攻击、逃跑和巡逻的AI。

**提示**:
- 使用Selector实现优先级
- 使用Condition检查血量和距离
- 实现攻击和逃跑动作

### 练习3: 实现可中断的动作
创建一个可以被高优先级任务中断的动作。

**提示**:
- 在Execute中检查中断标志
- 使用黑板传递中断信号
- 返回适当的状态

---

## 总结

本教程实现了一个完整的行为树系统，包括：

✅ **完整的节点类型**: Composite、Decorator、Leaf
✅ **黑板系统**: 共享数据管理
✅ **运行时执行**: 协程驱动的执行系统
✅ **调试工具**: 可视化和日志记录
✅ **性能优化**: 对象池和缓存

**关键收获**:
- 行为树是模块化的AI决策系统
- 编辑器/运行时分离保证序列化
- 黑板系统实现数据共享
- 执行器模式解耦节点定义和执行逻辑

**下一步**: 教程8将实现对话系统，进一步探索执行流图形的应用。

---

**相关资源**:
- [教程3: 执行流图形](03_ExecutionFlow.md)
- [教程8: 对话系统](08_DialogueSystem.md)
- [API参考](API_Reference.md)
- [最佳实践](Best_Practices.md)
new Dictionary<string, object>();

        /// <summary>
        /// 设置值
        /// </summary>
        public void Set<T>(string key, T value)
        {
            m_Data[key] = value;
        }

        /// <summary>
        /// 获取值
        /// </summary>
        public T Get<T>(string key, T defaultValue = default)
        {
            if (m_Data.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return defaultValue;
        }

        /// <summary>
        /// 检查键是否存在
        /// </summary>
        public bool Has(string key) => m_Data.ContainsKey(key);

        /// <summary>
        /// 删除键
        /// </summary>
        public void Remove(string key) => m_Data.Remove(key);

        /// <summary>
        /// 清空所有数据
        /// </summary>
        public void Clear() => m_Data.Clear();
    }
}
```

### 4. 运行时执行器

```csharp
using System.Collections;
using UnityEngine;

namespace GraphToolkitTutorials.BehaviorTree.Runtime
{
    /// <summary>
    /// 行为树运行器 - MonoBehaviour组件
    /// </summary>
    public class BehaviorTreeRunner : MonoBehaviour
    {
        [SerializeField] private BehaviorTreeGraph m_GraphAsset;
        [SerializeField] private float m_TickInterval = 0.1f;

        private BehaviorTreeRuntime m_Runtime;
        private Blackboard m_Blackboard;

        void Start()
        {
            if (m_GraphAsset == null) return;

            m_Blackboard = new Blackboard();
            m_Runtime = m_GraphAsset.CreateRuntime();
            StartCoroutine(TickLoop());
        }

        private IEnumerator TickLoop()
        {
            while (true)
            {
                m_Runtime.Tick(m_Blackboard);
                yield return new WaitForSeconds(m_TickInterval);
            }
        }

        /// <summary>
        /// 获取黑板（供外部设置数据）
        /// </summary>
        public Blackboard Blackboard => m_Blackboard;
    }
}
```

---

## 执行流程

```
BehaviorTreeRunner.Tick()
    └─ BehaviorTreeRuntime.Tick(blackboard)
        └─ 从Root节点开始执行
            └─ SequenceNode.Execute()
                ├─ Child1.Execute() → Success
                ├─ Child2.Execute() → Success
                └─ Child3.Execute() → Running (等待下一帧)
```

---

## 示例：巡逻AI

```
[Root]
└─ [Selector]
   ├─ [Sequence] (发现敌人时攻击)
   │  ├─ [Condition: HasEnemy]
   │  └─ [Action: AttackEnemy]
   └─ [Sequence] (默认巡逻)
      ├─ [Action: MoveToWaypoint]
      └─ [Action: WaitAtWaypoint]
```

**使用步骤**:
1. 在Unity中创建 `.bt` 资产文件
2. 在图形编辑器中连接节点
3. 将 `BehaviorTreeRunner` 组件添加到GameObject
4. 将 `.bt` 资产赋值给组件
5. 通过 `Blackboard` 传入运行时数据

---

## 练习题

1. **基础**: 创建一个简单的巡逻行为树，让AI在两个路点之间来回移动
2. **进阶**: 添加敌人检测条件，当发现敌人时切换到追击状态
3. **挑战**: 实现 `Parallel` 节点，让AI同时执行移动和播放动画

---

## 常见问题

**Q: 行为树每帧都会重新从根节点开始执行吗？**
A: 是的，每次Tick都从根节点开始，但Running状态的节点会记住当前执行位置。

**Q: 如何在节点间共享数据？**
A: 使用Blackboard系统，所有节点都可以读写同一个Blackboard实例。

**Q: Sequence和Selector的区别是什么？**
A: Sequence是"与"逻辑（所有子节点都要成功），Selector是"或"逻辑（任一子节点成功即可）。

---

**下一步**: 继续学习 [教程8: 对话系统](08_DialogueSystem.md)
