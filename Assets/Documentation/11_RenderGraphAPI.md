# 教程11：ContextNode 与 BlockNode —— 以新 RenderGraph 为例

## 概述

本教程引入 GraphToolkit 的两个重要概念：**ContextNode** 和 **BlockNode**，并以一个简化的 URP 渲染图（`.newrendergraph`）作为实践载体。

- **ContextNode** 是一种可以容纳子节点的节点，充当"容器"角色。
- **BlockNode** 是只能存在于特定 ContextNode 内的子节点，每个 BlockNode 描述一个原子操作。

通过将多种 GPU 操作（清屏、绘制不透明物体、后处理 Blit）组合进一个 `RenderPassNode`（ContextNode），本教程展示了如何用灵活的节点组合替代固定的节点类型枚举。

### 学习目标

- 理解 `ContextNode` 与普通 `Node` 的区别
- 掌握 `BlockNode` 的定义和 `[UseWithContext]` 限制机制
- 理解 `Graph.GetNodes()` 不含 `BlockNode`，必须通过 `ContextNode.BlockNodes` 访问
- 学习将多种操作合并到一个 `RasterRenderPass` 的策略
- 理解 `BlitOperation` 为何仍需独立双 Pass（读写约束）
- 与 T10 对比：T10 每种操作对应独立节点，T11 同一节点内嵌多种操作

### 前置知识

- 完成教程 05（ContextBlocks）— ContextNode/BlockNode 的入门教程
- 完成教程 09 或 10（URP RenderGraph）— `RecordRenderGraph` 基础

---

## 项目结构

```
Assets/Tutorials/11_RenderGraphAPI/
├─ Editor/
│  ├─ NewRenderGraph.cs                 # [Graph("newrendergraph")] 图定义 + CreateRuntime()
│  ├─ NewRenderGraphImporter.cs         # ScriptedImporter，.newrendergraph → NewRenderGraphRuntime
│  ├─ Nodes/
│  │  ├─ RenderGraphNode.cs             # 抽象基类（继承 ContextNode）+ PipelineStartNode / PipelineEndNode
│  │  ├─ PassNodes.cs                   # RenderPassNode（ContextNode，容纳 BlockNode 子节点）
│  │  └─ BlockNodes.cs                  # ClearBlockNode / DrawOpaqueBlockNode / DrawTransparentBlockNode / BlitBlockNode
│  └─ Unity.GraphToolkit.Tutorials.RenderGraphAPI.Editor.asmdef
├─ Runtime/
│  ├─ NewRenderGraphRuntime.cs          # ScriptableObject，[SerializeReference] 节点列表 + PassOperation 层次
│  ├─ NewRenderGraphFeature.cs          # ScriptableRendererFeature + NewRenderGraphPass
│  ├─ NewRenderGraphTester.cs           # 诊断 MonoBehaviour（OnGUI 显示运行时图状态）
│  └─ Unity.GraphToolkit.Tutorials.RenderGraphAPI.Runtime.asmdef
└─ Examples/
   ├─ Graphs/StandardPipeline.newrendergraph   # 示例图：Start → RenderPass[Clear+DrawOpaque+DrawTransparent] → End
   ├─ Scenes/RenderGraphTestScene.unity
   └─ Materials/CubeLit.mat                    # URP Lit 材质（橙棕色 Cube）
```

---

## 核心概念：ContextNode 与 BlockNode

### ContextNode — 可容纳子节点的容器

`ContextNode` 继承自 `Node`，但可以包含 `BlockNode` 子节点。

```csharp
// ContextNode 容器
[Node("RenderGraphPass", "")]
[UseWithGraph(typeof(NewRenderGraph))]
[Serializable]
internal class RenderPassNode : RenderGraphNode   // RenderGraphNode 继承 ContextNode
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        AddExecutionPorts(context);    // In + Out 执行端口
    }

    public override Runtime.RenderGraphRuntimeNode CreateRuntimeNode(NewRenderGraph graph)
    {
        var node = new Runtime.RenderPassDataRuntimeNode
        {
            nextNodeIndex = GetNextNodeIndex(graph)
        };

        // 关键：BlockNodes 属性访问子节点（GetNodes() 不含 BlockNode！）
        foreach (var block in BlockNodes)
        {
            if (block is ClearBlockNode clear)
                node.operations.Add(clear.CreateOperation());
            else if (block is DrawOpaqueBlockNode opaque)
                node.operations.Add(opaque.CreateOperation());
            else if (block is DrawTransparentBlockNode transparent)
                node.operations.Add(transparent.CreateOperation());
            else if (block is BlitBlockNode blit)
                node.operations.Add(blit.CreateOperation());
        }
        return node;
    }
}
```

### BlockNode — 只能存在于特定容器内的操作块

`BlockNode` 通过 `[UseWithContext]` 属性限制其只能拖入指定的 ContextNode。

```csharp
// BlockNode — 清屏操作
[Node("Operations", "")]
[UseWithContext(typeof(RenderPassNode))]   // ← 只能放进 RenderPassNode
[Serializable]
internal class ClearBlockNode : BlockNode
{
    private IPort m_ClearColor;
    private IPort m_ClearDepth;

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        m_ClearColor = context.AddInputPort<Color>("Clear Color").Build();
        m_ClearDepth = context.AddInputPort<bool>("Clear Depth").Build();
    }

    public Runtime.ClearOperation CreateOperation()
    {
        Color color = Color.black;
        m_ClearColor?.TryGetValue<Color>(out color);
        bool clearDepth = true;
        m_ClearDepth?.TryGetValue<bool>(out clearDepth);
        return new Runtime.ClearOperation { clearColor = color, clearDepth = clearDepth };
    }
}
```

### GetNodes() vs BlockNodes — 关键区别

```csharp
// ✅ 正确：通过图遍历普通节点（不含 BlockNode）
foreach (var node in graph.GetNodes())
{
    if (node is RenderGraphNode rn) { ... }
}

// ✅ 正确：通过 ContextNode 访问其子 BlockNode
foreach (var block in renderPassNode.BlockNodes)
{
    if (block is ClearBlockNode clear) { ... }
}

// ❌ 错误：GetNodes() 中永远找不到 BlockNode
foreach (var node in graph.GetNodes())
{
    if (node is ClearBlockNode clear) { ... }   // 永远不会进入此分支
}
```

---

## 节点层次

### 编辑器节点

```
RenderGraphNode（抽象基类，继承 ContextNode）
│   ├─ AddExecutionPorts()       — 添加 In/Out 执行端口
│   ├─ GetNodeIndex(graph)       — 只计数 RenderGraphNode 实例（与 CreateRuntime 过滤对齐）
│   └─ GetNextNodeIndex(graph)   — 用 graph.FindNodeForPort() 反查端口所属节点
│
├─ PipelineStartNode    — 只有 Out，图的入口
├─ PipelineEndNode      — 只有 In，图的终点
└─ RenderPassNode       — In + Out，可容纳 BlockNode 子节点
```

### BlockNode（只能放入 RenderPassNode）

```
BlockNode（抽象基类）
├─ ClearBlockNode         — 清屏，端口：Color (Color)、Clear Depth (bool)
├─ DrawOpaqueBlockNode    — 绘制不透明物体，端口：Layer Mask (LayerMask)
├─ DrawTransparentBlockNode — 绘制透明物体，端口：Layer Mask (LayerMask)
└─ BlitBlockNode          — 全屏后处理，选项：Material (INodeOption)
```

### 运行时节点

```csharp
RenderGraphRuntimeNode（抽象基类）
│   public int nextNodeIndex = -1;
│
├─ PipelineStartRuntimeNode
├─ PipelineEndRuntimeNode
└─ RenderPassDataRuntimeNode
       [SerializeReference]
       public List<PassOperation> operations   // 有序操作列表

PassOperation（抽象基类）
├─ ClearOperation          { Color clearColor; bool clearDepth; }
├─ DrawOpaqueOperation     { LayerMask layerMask; }
├─ DrawTransparentOperation { LayerMask layerMask; }
└─ BlitOperation           { Material material; }
```

---

## `INodeOption` — 引用类型数据的正确处理

`Material` 是 `UnityEngine.Object` 引用类型，无法用端口内联常量存储，必须使用 `INodeOption`。

```csharp
[Node("Operations", "")]
[UseWithContext(typeof(RenderPassNode))]
[Serializable]
internal class BlitBlockNode : BlockNode
{
    private INodeOption m_Material;

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        m_Material = context.AddOption<Material>("Material").Build();
    }

    public Runtime.BlitOperation CreateOperation()
    {
        Material mat = null;
        m_Material?.TryGetValue<Material>(out mat);
        return new Runtime.BlitOperation { material = mat };
    }
}
```

**规则**：
- 值类型（`Color`、`bool`、`LayerMask`、`float` 等）→ 用端口 `TryGetValue<T>`
- 引用类型（`Material`、`Texture` 等）→ 用 `INodeOption` `TryGetValue<T>`

---

## 运行时渲染策略

### 操作分组（合并 vs 独立）

```
RenderPassNode
├─ ClearBlockNode          ┐
├─ DrawOpaqueBlockNode     ├─ 合并为 1 个 AddRasterRenderPass（"Graph: Render Pass"）
├─ DrawTransparentBlockNode┘
└─ BlitBlockNode             → 独立双 Pass（"Graph: Blit" + "Graph: Blit Copy Back"）
```

**为何合并 Clear + Draw**：减少 `SetRenderTarget` 切换，提升 GPU 效率。

**为何 Blit 必须独立**：同一 `RasterPass` 内不能对同一纹理同时 `UseTexture`（读）和 `SetRenderAttachment`（写）——URP RenderGraph 的约束，防止读写竞争。

### 双 Pass Blit 流程

```
Pass A：activeColor ──[UseTexture/Read]──→ Blitter.BlitTexture(material) ──→ tempTexture
Pass B：tempTexture ──[UseTexture/Read]──→ 原样复制                       ──→ activeColor
```

```csharp
// Pass A
using (var builder = rg.AddRasterRenderPass<BlitPassData>("Graph: Blit", out var pd))
{
    pd.sourceTexture = resourceData.activeColorTexture;
    pd.material      = blitOp.material;
    builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
    builder.SetRenderAttachment(tempTexture, 0, AccessFlags.Write);
    builder.AllowPassCulling(false);
    builder.SetRenderFunc((BlitPassData data, RasterGraphContext ctx) =>
        Blitter.BlitTexture(ctx.cmd, data.sourceTexture, new Vector4(1, 1, 0, 0), data.material, 0));
}

// Pass B
using (var builder = rg.AddRasterRenderPass<BlitPassData>("Graph: Blit Copy Back", out var pd))
{
    pd.sourceTexture = tempTexture;
    builder.UseTexture(tempTexture, AccessFlags.Read);
    builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
    builder.AllowPassCulling(false);
    builder.SetRenderFunc((BlitPassData data, RasterGraphContext ctx) =>
        Blitter.BlitTexture(ctx.cmd, data.sourceTexture, new Vector4(1, 1, 0, 0), 0, false));
}
```

---

## 关键实现细节

### GetNodeIndex 与 CreateRuntime 过滤对齐

`CreateRuntime()` 只将 `RenderGraphNode` 实例加入 `runtime.nodes`，因此 `GetNodeIndex` 也必须只计数 `RenderGraphNode`，否则索引错位：

```csharp
// ✅ 正确：与 CreateRuntime 过滤逻辑一致
public int GetNodeIndex(NewRenderGraph graph)
{
    int idx = 0;
    foreach (var node in graph.GetNodes())
    {
        if (node is RenderGraphNode)
        {
            if (node == this) return idx;
            idx++;
        }
    }
    return -1;
}
```

### FindNodeForPort — IPort 无 .Node 属性

```csharp
// ✅ 正确：通过 graph.FindNodeForPort 反查端口所属节点
public int GetNextNodeIndex(NewRenderGraph graph)
{
    if (m_OutputPort == null) return -1;
    var connectedPort = graph.GetConnectedInputPort(m_OutputPort);
    if (connectedPort != null && graph.FindNodeForPort(connectedPort) is RenderGraphNode nextNode)
        return nextNode.GetNodeIndex(graph);
    return -1;
}

// ❌ 错误：IPort 接口无 .Node 属性，编译失败
var nextNode = connectedPort.Node as RenderGraphNode;
```

### `[SerializeReference]` — 多态列表必须

```csharp
// NewRenderGraphRuntime.cs
[SerializeReference]
public List<RenderGraphRuntimeNode> nodes = new List<RenderGraphRuntimeNode>();

// RenderPassDataRuntimeNode
[SerializeReference]
public List<PassOperation> operations = new List<PassOperation>();
```

`[SerializeField]` 只序列化基类字段，运行时 `is ClearOperation` 等类型检查全部失败。

---

## 与 Tutorial 10 的设计对比

| 维度 | Tutorial 10（URPGraph） | Tutorial 11（NewRenderGraph） |
|------|------------------------|-------------------------------|
| 节点粒度 | 每种操作一个独立节点（OpaquePassNode、TransparentPassNode…） | 一个 RenderPassNode（ContextNode）内嵌多个 BlockNode |
| 添加操作 | 在图中添加新节点并连线 | 在 RenderPassNode 内拖入新的 BlockNode |
| 节点基类 | `Node` | `ContextNode` |
| 子节点访问 | N/A | `BlockNodes` 属性（`GetNodes()` 不含 BlockNode） |
| GPU Pass 数 | 每节点一个 Pass | 非 Blit 操作合并为 1 Pass；每个 Blit 独立双 Pass |
| 条件分支 | QualityBranchNode / PlatformBranchNode | 无（本教程聚焦 ContextNode 模式） |

---

## 配置步骤

### 1. 创建 NewRenderGraph 资产

右键 Project 窗口 → **Create → Graph Toolkit → New Render Graph**，创建 `.newrendergraph` 文件。

### 2. 在图编辑器中搭建节点

```
[Pipeline Start] → [RenderPass] → [Pipeline End]
                      ↑
               （ContextNode 内）
               [ClearBlockNode]
               [DrawOpaqueBlockNode]
               [DrawTransparentBlockNode]
```

1. 双击 `.newrendergraph` 打开图编辑器
2. 添加 `Pipeline Start` 节点（菜单 **RenderGraph**）
3. 添加 `RenderGraphPass` 节点（菜单 **RenderGraph**）
4. 将 BlockNode 拖入 `RenderGraphPass` 内部（菜单 **Operations**）：
   - `ClearBlockNode`：配置 **Clear Color**（深灰色）和 **Clear Depth**（`true`）
   - `DrawOpaqueBlockNode`：默认 Layer Mask（所有层）
   - `DrawTransparentBlockNode`：默认 Layer Mask
5. 添加 `Pipeline End` 节点，按顺序连接执行端口
6. Ctrl+S 保存，Importer 自动生成 `NewRenderGraphRuntime`

### 3. 添加 Renderer Feature

1. 打开 `PC_Renderer.asset` Inspector
2. **Add Renderer Feature → New RenderGraph Feature**
3. 将生成的 `NewRenderGraphRuntime` 拖入 **Graph Asset** 字段

> ⚠️ **最常见错误**：Feature 已添加但 **Graph Asset** 字段为空（`None`）。`AddRenderPasses()` 首行即返回，Frame Debugger 中看不到任何自定义 Pass，且不报任何错误。

### 4. 验证

1. **Window → Analysis → Frame Debugger → Enable**
2. 进入 Play 模式
3. 在 Frame Debugger 树中展开 `RenderGraph`，应看到 `Graph: Render Pass`

---

## Frame Debugger 中的结构

`StandardPipeline.newrendergraph` 运行后，Frame Debugger 中的条目：

```
RenderGraph
└── Camera.Universal.RenderRequest
    ├── ... （URP 内置 Pass）
    ├── Graph: Render Pass     ← ✅ ClearBlockNode + DrawOpaqueBlockNode + DrawTransparentBlockNode 合并
    └── ... （URP 内置 Pass）
```

若 `RenderPassNode` 内还有 `BlitBlockNode`（且已赋值 Material）：

```
    ├── Graph: Render Pass     ← 非 Blit 操作合并
    ├── Graph: Blit            ← Blit Pass A（activeColor → temp）
    └── Graph: Blit Copy Back  ← Blit Pass B（temp → activeColor）
```

---

## 常见问题

### Q：往 RenderPassNode 外拖 BlockNode，提示无法放置？

`[UseWithContext(typeof(RenderPassNode))]` 限制了 BlockNode 只能拖入 `RenderPassNode`。在其他地方右键添加节点时，Operations 类别中的节点不会出现。

### Q：Frame Debugger 看不到 `Graph: Render Pass`？

按优先级排查：
1. **Graph Asset 字段为空** — 最常见，重新赋值
2. **activeColorTexture 无效** — Console 会出现 `[NewRenderGraph] activeColorTexture is invalid` 警告，检查相机使用的 Renderer 是否包含本 Feature
3. **RenderPassNode 内无 BlockNode** — 空的 `operations` 列表 → `drawOps.Count == 0` → 不录制任何 Pass

### Q：`GetNodes()` 里找不到 ClearBlockNode？

正确行为。`graph.GetNodes()` 只返回顶级 `INode`（即 `RenderPassNode`、`PipelineStartNode` 等），`BlockNode` 必须通过其父 ContextNode 的 `BlockNodes` 属性访问。

### Q：`is` 类型检查失败，operations 里的 `ClearOperation` 始终是 null？

检查 `RenderPassDataRuntimeNode.operations` 是否标记了 `[SerializeReference]`（而非 `[SerializeField]`）。缺失 `[SerializeReference]` 时多态列表反序列化失败，运行时 `is` 检查对所有 `PassOperation` 子类都返回 false。

### Q：BlitBlockNode 的 Material 不生效？

Material 使用的 Shader 必须能从 `_BlitTexture` 采样源纹理（`Blitter.BlitTexture` 自动绑定此 uniform）。可使用 URP 内置 Blit shader 作为模板。

---

## 诊断工具（NewRenderGraphTester）

```csharp
// 挂载到场景任意 GameObject
// m_GraphAsset 字段赋值 StandardPipeline.newrendergraph
public class NewRenderGraphTester : MonoBehaviour
{
    [SerializeField] private NewRenderGraphRuntime m_GraphAsset;

    private void OnGUI()
    {
        // 显示 startNodeIndex + 每个节点的类型和 operations 数量
    }
}
```

**操作**：

1. 将 `NewRenderGraphTester` 挂载到场景 GameObject
2. 将 `.newrendergraph` 资产拖入 `Graph Asset` 字段
3. Play 模式下查看左上角，确认节点链和 operations 列表正确

---

## 关键 API 速查

```csharp
// ── ContextNode / BlockNode ────────────────────────────────────────────────
ContextNode.BlockNodes                        // 访问子 BlockNode 列表
[UseWithContext(typeof(MyContextNode))]       // 限制 BlockNode 只能放入指定容器

// ── 端口值读取 ─────────────────────────────────────────────────────────────
port.TryGetValue<Color>(out color)            // 读取端口的内联常量或上游连接值
option.TryGetValue<Material>(out material)   // 读取 INodeOption 的值

// ── 查找节点 ───────────────────────────────────────────────────────────────
graph.GetConnectedInputPort(outputPort)       // 输出端口 → 下游输入端口
graph.FindNodeForPort(inputPort)              // 端口 → 所属节点（IPort 无 .Node 属性）

// ── 运行时序列化 ───────────────────────────────────────────────────────────
[SerializeReference]                          // 多态列表必须用此标记
List<RenderGraphRuntimeNode> nodes            // 支持 is 类型检查
List<PassOperation> operations                // 同上

// ── RenderGraph Pass ────────────────────────────────────────────────────────
rg.AddRasterRenderPass<TData>("Name", out pd)  // 添加一个 RasterRenderPass
builder.SetRenderAttachment(tex, 0, AccessFlags.Write)
builder.SetRenderAttachmentDepth(depth, AccessFlags.ReadWrite)
builder.UseTexture(tex, AccessFlags.Read)      // 只读纹理（不能与 SetRenderAttachment 同纹理共用）
builder.AllowPassCulling(false)                // 防止 Pass 被 RenderGraph 裁剪
```

---

## 练习题

1. **基础**：在 `StandardPipeline.newrendergraph` 的 `RenderPassNode` 中修改 `ClearBlockNode` 的颜色，观察场景清屏颜色的变化。

2. **进阶**：新建一个 `.newrendergraph`，在 `RenderPassNode` 中只放 `ClearBlockNode` 和 `DrawOpaqueBlockNode`（不加 DrawTransparent），观察 Frame Debugger 中 `Graph: Render Pass` 内的命令数量。

3. **挑战**：为 `BlitBlockNode` 创建一个简单的灰度后处理 Shader 和 Material，赋值后观察 Frame Debugger 中 `Graph: Blit` 和 `Graph: Blit Copy Back` 两个 Pass 的出现，以及画面效果。

4. **扩展**：参照 `DrawOpaqueBlockNode` 的模式，实现一个 `SkyboxBlockNode`，在其 `CreateOperation()` 中返回一个新的 `SkyboxOperation` 类型，并在 `NewRenderGraphFeature` 中处理它（提示：需要修改 Runtime 层）。

---

**上一教程**：[教程10：完整图形化 URP 渲染管线](10_GraphDrivenURP.md)
