# GraphToolkit 教程系列

Unity 6 GraphToolkit 完整教程，从基础到高级应用。

## 教程概览

### 阶段一：基础入门

#### [教程1: Hello Graph - 简单计算器图形](01_HelloGraph.md)
**难度**: ⭐
**时间**: 1-2小时

学习GraphToolkit的核心概念：
- Graph、Node、Port基础
- 数据流评估（Pull 模式）
- ScriptedImporter使用
- 创建简单的数学运算图形

**关键收获**: 理解图形系统的基本架构

---

#### [教程2: 数据流图形 - 纹理生成器](02_DataFlow.md)
**难度**: ⭐⭐
**时间**: 2-3小时

深入数据流处理：
- 递归端口评估
- 多类型接口设计（ITextureNode / IColorNode / IFloatNode）
- 程序化纹理生成
- 复杂数据流管理

**关键收获**: 掌握多类型数据流图形的设计模式

---

#### [教程3: 执行流图形 - 任务系统](03_ExecutionFlow.md)
**难度**: ⭐⭐
**时间**: 2-3小时

理解执行流控制：
- 编辑器节点 → 运行时节点转换（Push 模式）
- 执行器模式（Executor Pattern）
- 协程驱动的顺序执行与分支
- Editor / Runtime 分离架构

**关键收获**: 区分数据流和执行流图形，掌握 Editor/Runtime 分离

---

### 阶段二：进阶应用

#### [教程4: 变量与子图 - URP 材质生成器](04_VariablesSubgraphs.md)
**难度**: ⭐⭐⭐
**时间**: 3-4小时

掌握高级特性：
- `IVariable` 接口与 `VariableKind`（Local / Input / Output）
- `[Subgraph]` 与框架自动生成的 `ISubgraphNode`
- 常量子图（只有 Output 变量）与变量子图（Input + Output 变量）
- 在 ScriptedImporter 中生成真实 URP Material 资产

**关键收获**: 实现可复用的参数化图形模块

---

#### [教程5: ContextNode 与 BlockNode - 着色器函数图](05_ContextBlocks.md)
**难度**: ⭐⭐⭐
**时间**: 3-4小时

理解上下文系统：
- `ContextNode` 作为函数容器，`BlockNode` 作为可组合操作单元
- `[UseWithContext]` 将 BlockNode 与 ContextNode 绑定
- `TryGetValue` 的关键限制与正确的连线求值方式
- 类似 Shader Graph 自定义函数节点的实现思路

**关键收获**: 掌握 ContextNode / BlockNode 的组织结构与求值模式

---

#### [教程6: 技能系统 - 事件驱动与并行执行](06_AbilitySystem.md)
**难度**: ⭐⭐⭐⭐
**时间**: 4-5小时

综合执行流实践：
- 事件驱动入口（`OnEventNode` + `FireEvent`）
- 并行执行分支（`ParallelNode` + 协程 `WaitUntil`）
- `[SerializeReference]` 多态运行时节点列表
- `FindNextIndex` 将连线转换为整数索引
- `AbilityRunner` MonoBehaviour 协程执行引擎

**关键收获**: 完整的事件驱动执行流图，深化 Editor/Runtime 分离实践

---

### 阶段三：实战项目

#### [教程7: 行为树系统](07_BehaviorTree.md)
**难度**: ⭐⭐⭐⭐
**时间**: 4-5小时

实现完整的 AI 行为树：
- Composite（Sequence / Selector）、Decorator（Inverter / Repeater）、Leaf 三层节点
- 运行时黑板（Blackboard）共享 AI 状态数据
- 递归 Tick 机制与 Success / Failure / Running 三态返回值
- `BehaviorTreeRunner` 每帧驱动根节点 Tick

**关键收获**: 将经典 AI 架构映射为 GraphToolkit 执行流图，掌握树形遍历运行时

---

#### [教程8: 对话系统](08_DialogueSystem.md)
**难度**: ⭐⭐⭐⭐
**时间**: 4-5小时

构建完整的游戏对话系统：
- 线性对话、玩家选择分支（`ChoiceNode`）、条件分支（`BranchNode`）
- 运行时变量管理（`DialogueVariables`，字符串键值对）
- `UnityEvent` 解耦 `DialogueRunner` 与 UI / 业务逻辑
- `graph.FindNodeForPort(port)` 的必要性（`IPort` 无 `.Node` 属性）
- New Input System（`Keyboard.current`）与 asmdef 引用配置

**关键收获**: 综合运用执行流全套技术，掌握 UnityEvent 解耦模式

---

#### [教程9: 渲染图基础](09_RenderGraphBasics.md)
**难度**: ⭐⭐⭐⭐⭐
**时间**: 5-6小时

将图形系统集成到 Unity 6 URP RenderGraph API：
- `RecordRenderGraph` 替代旧版 `Execute`（Unity 6 URP 唯一入口）
- 每节点一个 `AddRasterRenderPass`，Frame Debugger 中各自独立可见
- `ContextContainer` 三个数据对象：`UniversalResourceData` / `UniversalCameraData` / `UniversalRenderingData`
- `DrawRendererList`（新版）代替 `DrawRenderers`（旧版）
- 双 Pass Blit 模式（同一纹理不能在同一 Pass 内同时读写）

**关键收获**: 掌握 Unity 6 URP RenderGraph API，理解 Frame Debugger 可见性原理

---

#### [教程10: 完整图形化渲染管线](10_GraphDrivenURP.md)
**难度**: ⭐⭐⭐⭐⭐
**时间**: 5-6小时

实现完全由节点图驱动的 URP 渲染管线：
- 多类型 Pass 节点：Opaque / Transparent / Shadow / Skybox / Post Process / Custom Pass
- 条件渲染：`QualityBranchNode`（质量等级分支）和 `PlatformBranchNode`（平台分支）在 CPU 阶段决策
- `Blitter.BlitTexture` 双 Pass 模式（CustomPassNode 全屏后处理效果）
- `[SerializeReference]` 多态序列化与运行时 `is` 类型检查
- `AddOption<T>().Build()` + `TryGetValue` — GraphToolkit 唯一有效的选项 API 模式
- `ScriptableRendererFeature` 集成：`AddRenderPasses` → `EnqueuePass` → `RecordRenderGraph`

**关键收获**: 综合运用全系列技术，实现生产级图形化渲染管线原型

---

## 学习路径建议

### 快速入门路径（适合时间有限的开发者）
1. 教程1: Hello Graph（必学）
2. 教程3: 执行流图形（必学）
3. 根据需求选择教程4（数据流方向）或教程6（执行流方向）

### 完整学习路径（适合深入掌握）
按顺序完成所有教程1-10

### 专项学习路径

**数据处理方向**:
- 教程1 → 教程2 → 教程4 → 教程5

**游戏逻辑方向**:
- 教程1 → 教程3 → 教程6 → 教程7 → 教程8

---

## 两大图形范式对比

| 特性 | 数据流（Pull） | 执行流（Push） |
|------|--------------|--------------|
| 典型教程 | 1, 2, 4, 5 | 3, 6, 7, 8, 9, 10 |
| 求值时机 | 导入时（ScriptedImporter） | 运行时（MonoBehaviour） |
| 连线方向 | 数据从源节点流向目标 | 执行顺序从上游流向下游 |
| 等待/异步 | 不支持 | 支持（协程 yield） |
| Editor/Runtime 分离 | 不需要 | 必须 |
| 适用场景 | 材质、纹理、着色器 | AI、对话、任务、渲染管线 |

---

## 前置知识

### 必需
- C#基础编程
- Unity编辑器基本操作
- 面向对象编程概念

### 推荐
- Unity编辑器扩展基础（ScriptedImporter、Editor 脚本）
- 数据结构和算法
- 设计模式（接口、策略、访问者）

---

## 项目设置

### Unity版本
- **要求**: Unity 6000.0.5a8 或更高
- **原因**: GraphToolkit 是 Unity 6 的内置模块（`UnityEditor.GraphToolkitModule.dll`）

### 项目配置
1. 使用 Unity 6000.0.5a8+ 打开本项目
2. Unity 会自动编译所有教程代码
3. 在 Console 窗口检查是否有编译错误

### 验证安装
```csharp
// 在任意 Editor 脚本中，能使用以下命名空间即表示 GraphToolkit 可用
using Unity.GraphToolkit.Editor;
```

---

## 教程约定

### 代码风格
- Editor 节点类用 `internal`；Runtime 类用 `public`
- `[SerializeField] private` 用于节点字段
- 字符串/数值选项加 `.Delayed().Build()` 避免每次按键重建节点
- 中文注释解释设计意图

### 文件组织
```
Assets/Tutorials/
├─ 01_HelloGraph/
│  └─ Editor/           # 编辑器代码（数据流图无 Runtime）
├─ 03_ExecutionFlow/
│  ├─ Editor/           # 编辑器节点
│  └─ Runtime/          # 运行时节点、执行器、MonoBehaviour
├─ 08_DialogueSystem/
│  ├─ Editor/
│  ├─ Runtime/
│  └─ Examples/         # 示例资产与测试场景
└─ ...
```

### 命名规范
- **Graph 类**: `[功能]Graph`（如 `CalculatorGraph`、`MaterialGraph`）
- **Node 类**: `[功能]Node`（如 `AddNode`、`OnEventNode`）
- **文件扩展名**: 简短且描述性（`.calc`、`.texgraph`、`.matgraph`、`.ability`、`.bt`、`.dialogue`）

---

## 常见问题

### Q: 为什么使用 Unity 6？
A: GraphToolkit 是 Unity 6 的内置模块，旧版本无法使用。

### Q: 数据流图和执行流图的区别？
A: 数据流（Pull 模式）在导入时求值，结果存入资产；执行流（Push 模式）在运行时由 MonoBehaviour 驱动，支持时序、等待、并发。

### Q: IPort 没有 .Node 属性怎么办？
A: 使用 `graph.FindNodeForPort(port)` 遍历所有节点的输入/输出端口来查找。这是执行流图中连线查找的标准模式（见教程3、7、8）。

### Q: 项目使用 Input System package 后 Input.GetKey 报错？
A: 在 runtime asmdef 的 `references` 中添加 `"Unity.InputSystem"`，并改用 `Keyboard.current` / `Mouse.current` API。

### Q: 教程代码的许可证？
A: MIT 许可证，可自由使用和修改。

---

## 版本历史

### v4.0.0 (2026-03-25)
- 完成教程10（完整图形化渲染管线）
- 教程10：多类型 Pass 节点（Opaque / Transparent / Shadow / Skybox / Post Process / Custom Pass）
- 教程10：Quality Branch + Platform Branch 条件渲染（CPU 阶段决策）
- 教程10：`Blitter.BlitTexture` 双 Pass 全屏后处理模式
- 教程10：`AddOption<T>().Build()` + `TryGetValue` — 所有类型唯一有效的选项 API
- 教程10：`[SerializeReference]` 多态序列化（`[SerializeField]` 会导致 is 检查失败）
- 教程10新增：`URPGraphTester` 诊断 MonoBehaviour（OnGUI 节点链条可视化）
- 阶段三更新为包含教程9+10，移除"阶段四规划中"

### v3.1.0 (2026-03-25)
- 完成教程9（渲染图基础）
- 教程9修复：`RecordRenderGraph` 每节点独立 `AddRasterRenderPass`，Frame Debugger 可见
- 教程9修复：`DrawRendererList` 替代 `BeginSample`/`EndSample`（CPU 标记不是 GPU 命令）
- 教程9修复：`[SerializeReference]` 多态序列化 + `Blitter.BlitTexture` 双 Pass 模式
- 阶段三新增教程9，阶段四为"完整渲染管线（规划中）"

### v3.0.0 (2026-03-25)
- 完成教程7（行为树系统）和教程8（对话系统）
- 教程8修复：`IPort.Node` → `graph.FindNodeForPort(port)`
- 教程8修复：Input System asmdef 引用 + `Keyboard.current` API
- 教程8新增：`DialogueTester` 头像渲染（`GUI.DrawTextureWithTexCoords`）
- 阶段三更新为"实战项目"，阶段四为"渲染图（规划中）"

### v2.0.0 (2026-03-24)
- 完成教程1-6（全部可运行）
- 教程6从 Custom UI 改为技能系统（事件驱动 + 并行执行）
- 修复 Tutorial 06 所有节点的 `[Node]` 属性错误

### v1.0.0 (2026-03-16)
- 初始版本，完成教程1-2，规划教程3-10

---

## 下一步

开始学习 [教程1: Hello Graph](01_HelloGraph.md)！
