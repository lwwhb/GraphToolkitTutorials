# 教程 09：渲染图基础（RenderGraph Basics）

## 概述

本教程演示如何将 GraphToolkit **执行流图**（Push 模型）与 Unity 6 URP 的 **RenderGraph API** 集成，实现一套由节点图驱动的渲染管线。

### 学习目标

- 理解 Unity 6 URP RenderGraph API 的核心方法（`RecordRenderGraph` 替代旧版 `Execute`）
- 掌握 `AddRasterRenderPass + SetRenderAttachment` 模式及其与 Frame Debugger 的关系
- 学会用 `DrawRendererList`（新版）代替 `ScriptableRenderContext.DrawRenderers`（旧版）
- 理解 `ContextContainer` 三个数据对象的职责分工
- 掌握 `Blitter.BlitTexture` 的双 Pass 模式

### 前置知识

- 完成教程 03（执行流图形）
- 了解 URP `ScriptableRendererFeature` 基础概念

---

## 项目结构

```
Assets/Tutorials/09_RenderGraphBasics/
├─ Editor/
│  ├─ RenderGraph.cs                 # 编辑器图定义（.rendergraph 文件类型）
│  ├─ RenderGraphImporter.cs         # ScriptedImporter，将 .rendergraph → RenderGraphRuntime
│  ├─ Nodes/
│  │  ├─ RenderNode.cs               # 编辑器节点基类（GetNextNode / GetNodeIndex）
│  │  └─ RenderNodes.cs              # CameraNode / RenderPassNode / BlitNode / OutputNode
│  └─ Unity.GraphToolkit.Tutorials.RenderGraphBasics.Editor.asmdef
├─ Runtime/
│  ├─ GraphDrivenRendererFeature.cs  # ScriptableRendererFeature + ScriptableRenderPass
│  ├─ RenderGraphRuntime.cs          # 运行时 ScriptableObject（节点列表 + 访问 API）
│  ├─ RenderRuntimeNode.cs           # 运行时节点数据类（CameraNode/RenderPassNode/BlitNode/OutputNode）
│  ├─ RenderGraphTester.cs           # 诊断 MonoBehaviour（OnGUI 显示运行时图状态）
│  └─ Unity.GraphToolkit.Tutorials.RenderGraphBasics.Runtime.asmdef
└─ Examples/
   ├─ Graphs/BasicRenderGraph.rendergraph   # 示例图：Camera → RenderPass("OpaquePass") → Output
   └─ Scenes/RenderGraphTestScene.unity     # 测试场景
```

---

## 核心概念

### Unity 6 URP RenderGraph API 与旧版对比

| 概念 | 旧版 URP（兼容模式）| Unity 6 RenderGraph API |
|---|---|---|
| GPU 命令入口 | `Execute(ScriptableRenderContext, ref RenderingData)` | `RecordRenderGraph(RenderGraph, ContextContainer)` |
| 颜色缓冲访问 | `renderingData.cameraData.renderer.cameraColorTargetHandle` | `frameData.Get<UniversalResourceData>().activeColorTexture` |
| 裁剪结果 | `renderingData.cullResults` | `frameData.Get<UniversalRenderingData>().cullResults` |
| 相机对象 | `renderingData.cameraData.camera` | `frameData.Get<UniversalCameraData>().camera` |
| 绘制可见对象 | `context.DrawRenderers(cullResults, ref drawSettings, ref filterSettings)` | `rg.CreateRendererList(desc)` + `cmd.DrawRendererList(handle)` |
| CommandBuffer | `CommandBufferPool.Get(...)` + `context.ExecuteCommandBuffer(cmd)` | `ctx.cmd`（`RasterCommandBuffer`，自动管理） |
| Blit | `cmd.Blit(src, dst, mat)` | `Blitter.BlitTexture(cmd, src, scaleBias, mat, pass)` |

### ContextContainer 三个数据对象

```csharp
// 在 RecordRenderGraph 中通过 ContextContainer 获取当前帧数据
var resourceData  = frameData.Get<UniversalResourceData>();   // 纹理句柄
var cameraData    = frameData.Get<UniversalCameraData>();     // 相机信息
var renderingData = frameData.Get<UniversalRenderingData>();  // 裁剪/渲染数据

// 各自职责：
// resourceData.activeColorTexture  → 当前帧颜色缓冲 TextureHandle
// resourceData.activeDepthTexture  → 当前帧深度缓冲 TextureHandle
// cameraData.camera                → Unity Camera 对象（排序、视角矩阵）
// renderingData.cullResults        → 相机已裁剪的可见对象列表
```

---

## 节点设计

### 编辑器节点（`RenderNodes.cs`）

```
CameraNode      只有 Out 执行端口，配置 Camera Tag。图的起点。
RenderPassNode  In → Out，配置 Pass Name / Pass Event / Layer Mask。
BlitNode        In → Out，配置 Material（后处理材质）。
OutputNode      只有 In 执行端口。图的终点。
```

### 运行时节点（`RenderRuntimeNode.cs`）

```csharp
// 基类：所有运行时节点共有 nextNodeIndex（-1 表示无后继）
public abstract class RenderRuntimeNode
{
    public int nextNodeIndex = -1;
}

// 相机节点：记录相机 Tag，用于查找正确的相机
public class CameraNode : RenderRuntimeNode { public string cameraTag; }

// 渲染 Pass 节点：配置要绘制什么层、哪个事件时机
public class RenderPassNode : RenderRuntimeNode
{
    public string         passName;
    public RenderPassEvent passEvent;
    public LayerMask       layerMask;
}

// Blit 节点：使用 material 对当前颜色缓冲做后处理
public class BlitNode : RenderRuntimeNode { public Material material; }

// 输出节点：终止遍历
public class OutputNode : RenderRuntimeNode { }
```

---

## 编辑器到运行时的转换（Importer）

```
.rendergraph 文件
    ↓ ScriptedImporter（RenderGraphImporter）
    ↓ editorGraph.CreateRuntimeGraph()
RenderGraphRuntime（ScriptableObject，主资产）
    • nodes: List<RenderRuntimeNode>   ← [SerializeReference] 支持多态序列化
    • startNodeIndex                   ← CameraNode 在 nodes 中的位置
```

**关键：`[SerializeReference]` 多态序列化**

`nodes` 列表使用 `[SerializeReference]` 而非 `[SerializeField]`，这样 Unity 能正确序列化并反序列化 `CameraNode`、`RenderPassNode` 等不同子类型，在运行时 `is` 类型检查才能正确匹配。

---

## ScriptableRendererFeature 实现

### Feature 的职责

```csharp
public class GraphDrivenRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private RenderGraphRuntime m_RenderGraph;  // 在 Renderer Inspector 赋值
    private GraphDrivenRenderPass m_RenderPass;

    public override void Create()
    {
        if (m_RenderGraph != null)
            m_RenderPass = new GraphDrivenRenderPass(m_RenderGraph);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_RenderGraph == null) return;
        // 延迟初始化：Create() 可能在 m_RenderGraph 尚未反序列化时被调用
        if (m_RenderPass == null)
            m_RenderPass = new GraphDrivenRenderPass(m_RenderGraph);
        renderer.EnqueuePass(m_RenderPass);
    }
}
```

---

## RecordRenderGraph：每节点一个 Pass

### 架构设计决策

| 方式 | Frame Debugger 可见性 | 资源依赖声明 |
|---|---|---|
| 单一大 Pass（旧尝试）| 只有 1 个条目 | 所有节点共享同一声明 |
| **每节点一个 Pass（当前）**| **每节点独立一个条目** | 每节点按需声明自己的输入/输出 |

### 遍历逻辑

```csharp
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
{
    var cameraNode = m_RenderGraph.GetCameraNode();
    var resourceData  = frameData.Get<UniversalResourceData>();
    var cameraData    = frameData.Get<UniversalCameraData>();
    var renderingData = frameData.Get<UniversalRenderingData>();

    int current = cameraNode.nextNodeIndex;
    while (current >= 0)
    {
        var node = m_RenderGraph.GetNode(current);
        if (node is RenderPassNode rp)      { RecordRenderPassNode(..., rp);   current = rp.nextNodeIndex; }
        else if (node is BlitNode blit)     { RecordBlitNode(..., blit);       current = blit.nextNodeIndex; }
        else if (node is OutputNode)        { break; }
        else                                { current = node.nextNodeIndex; }
    }
}
```

---

## RenderPassNode：DrawRendererList 模式

### 完整实现

```csharp
private class RenderPassData { public RendererListHandle rendererListHandle; }

private void RecordRenderPassNode(RenderGraph rg, UniversalResourceData resourceData,
    UniversalCameraData cameraData, UniversalRenderingData renderingData, RenderPassNode rp)
{
    if (!resourceData.activeColorTexture.IsValid()) return;

    using var builder = rg.AddRasterRenderPass<RenderPassData>("Graph: " + rp.passName, out var passData);

    // 1. 创建 RendererListHandle（Record 阶段注册，Execute 阶段才实例化）
    var desc = new RendererListDesc(k_ShaderTagIds, renderingData.cullResults, cameraData.camera)
    {
        sortingCriteria  = SortingCriteria.CommonOpaque,
        renderQueueRange = RenderQueueRange.all,
        layerMask        = rp.layerMask,
    };
    passData.rendererListHandle = rg.CreateRendererList(desc);

    // 2. 声明资源依赖
    builder.UseRendererList(passData.rendererListHandle);   // 防止 Pass 被 RenderGraph 裁剪
    builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);  // ← Frame Debugger 可见的关键
    if (resourceData.activeDepthTexture.IsValid())
        builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite);
    builder.AllowPassCulling(false);

    // 3. 注册 GPU 命令（Execute 阶段执行）
    builder.SetRenderFunc((RenderPassData data, RasterGraphContext ctx) =>
    {
        ctx.cmd.DrawRendererList(data.rendererListHandle);  // 真正的 DrawCall
    });
}
```

### 关键 API 解释

**`RendererListDesc`**（命名空间 `UnityEngine.Rendering.RendererUtils`）

```csharp
new RendererListDesc(shaderTagIds, cullingResults, camera)
{
    sortingCriteria  = SortingCriteria.CommonOpaque,  // 前到后排序（不透明），后到前（透明）
    renderQueueRange = RenderQueueRange.all,           // opaque(2000-2500) + transparent(2501-5000)
    layerMask        = rp.layerMask,                   // 节点配置的层遮罩，-1 = 所有层
}
```

**`builder.SetRenderAttachment`** — Frame Debugger 可见性的核心

```
SetRenderAttachment(activeColorTexture, 0, AccessFlags.Write)
    → RenderGraph 在 Execute 时自动插入 SetRenderTarget GPU 命令
    → 该 SetRenderTarget 命令使此 Pass 在 Frame Debugger 中出现独立条目
    → 仅有 BeginSample/EndSample 是 CPU Profiler 标记，不足以让 Pass 在 Frame Debugger 中可见
```

**`RendererListHandle` → `RendererList` 的隐式转换**

`RendererListHandle` 有隐式转换操作符到 `RendererList`（通过 `RenderGraphResourceRegistry.current.GetRendererList`）。此转换仅在 `SetRenderFunc` 的 Execute 阶段有效（因为 `RenderGraphResourceRegistry.current` 仅在此阶段设置）。

---

## BlitNode：双 Pass 模式

### 为什么需要双 Pass

在同一 `RasterPass` 内，不能对同一纹理同时声明 `UseTexture`（读）和 `SetRenderAttachment`（写）——这是 URP RenderGraph 的约束，防止读写竞争。

解决方案：引入临时纹理作为中间缓冲。

```
Pass A：activeColor --[UseTexture/Read]--> material 处理 --[SetRenderAttachment/Write]--> tempTexture
Pass B：tempTexture --[UseTexture/Read]--> 原样复制    --[SetRenderAttachment/Write]--> activeColor
```

### 实现

```csharp
private void RecordBlitNode(RenderGraph rg, UniversalResourceData resourceData, BlitNode blit)
{
    if (blit.material == null || !resourceData.activeColorTexture.IsValid()) return;

    // 创建与颜色缓冲同规格的临时纹理
    var tempDesc = rg.GetTextureDesc(resourceData.activeColorTexture);
    tempDesc.name = "Blit_Temp";
    var tempTexture = rg.CreateTexture(tempDesc);

    // Pass A：activeColor → tempTexture，用 material 处理
    using (var builder = rg.AddRasterRenderPass<BlitPassData>("Graph: Blit " + blit.material.name, out var pd))
    {
        pd.sourceTexture = resourceData.activeColorTexture;
        pd.material = blit.material;
        builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
        builder.SetRenderAttachment(tempTexture, 0, AccessFlags.Write);
        builder.AllowPassCulling(false);
        builder.SetRenderFunc((BlitPassData data, RasterGraphContext ctx) =>
        {
            // 绑定 _BlitTexture = source，用 material 绘制全屏三角形到当前渲染目标
            Blitter.BlitTexture(ctx.cmd, data.sourceTexture, new Vector4(1, 1, 0, 0), data.material, 0);
        });
    }

    // Pass B：tempTexture → activeColor，无 material 原样复制
    using (var builder = rg.AddRasterRenderPass<BlitPassData>("Graph: Blit Copy Back", out var pd))
    {
        pd.sourceTexture = tempTexture;
        builder.UseTexture(tempTexture, AccessFlags.Read);
        builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
        builder.AllowPassCulling(false);
        builder.SetRenderFunc((BlitPassData data, RasterGraphContext ctx) =>
        {
            // 内置 Blit shader，像素精确复制（mipLevel=0, bilinear=false）
            Blitter.BlitTexture(ctx.cmd, data.sourceTexture, new Vector4(1, 1, 0, 0), 0, false);
        });
    }
}
```

### Blitter.BlitTexture API

```csharp
// RasterCommandBuffer 重载（在 RasterPass 的 SetRenderFunc 中使用）：
Blitter.BlitTexture(RasterCommandBuffer cmd, RTHandle source, Vector4 scaleBias, Material material, int pass)
Blitter.BlitTexture(RasterCommandBuffer cmd, RTHandle source, Vector4 scaleBias, float mipLevel, bool bilinear)

// scaleBias = Vector4(scaleX, scaleY, offsetX, offsetY)
//   (1, 1, 0, 0) → 全屏，无缩放无偏移
//   (0.5, 0.5, 0, 0) → 左下角 1/4 屏幕

// TextureHandle 隐式转换为 RTHandle，可直接传入
```

---

## Pass 被 RenderGraph 裁剪的预防

RenderGraph 有 Pass 裁剪（Pass Culling）机制：如果一个 Pass 的输出未被后续任何 Pass 读取，则该 Pass 会被自动跳过。

防止裁剪的三种方式：

```csharp
// 1. 声明 RendererList 依赖（DrawRendererList Pass 必须）
builder.UseRendererList(passData.rendererListHandle);

// 2. 声明颜色写入（SetRenderAttachment 已隐含）
builder.SetRenderAttachment(colorTexture, 0, AccessFlags.Write);

// 3. 强制禁止裁剪（副作用 Pass，如写入全局状态）
builder.AllowPassCulling(false);
```

---

## 配置步骤

### 1. 创建渲染图资产

右键 Project 窗口 → **Create → Graph Toolkit → Render Graph**

在图编辑器中连接节点：

```
[CameraNode]  →  [RenderPassNode]  →  [OutputNode]
  (Out)           (In → Out)            (In)
```

`RenderPassNode` 配置：
- **Pass Name**：`OpaquePass`（Frame Debugger 中显示为 `Graph: OpaquePass`）
- **Pass Event**：`AfterRenderingOpaques`（300）
- **Layer Mask**：默认（全层）

### 2. 配置 URP Renderer

打开 `Assets/Settings/PC_Renderer.asset`（或你的 Renderer 资产）：

1. 点击 **Add Renderer Feature**
2. 选择 **Graph Driven Renderer Feature**
3. 将 `.rendergraph` 文件（即生成的 `RenderGraphRuntime` ScriptableObject）拖入 **Render Graph** 字段

> ⚠️ 确认 Renderer 是否已被 Pipeline Asset 和当前相机使用：
> - Graphics Settings → Custom Render Pipeline → PC_RPAsset
> - PC_RPAsset → Renderer → PC_Renderer（默认 index 0）
> - 相机 → Renderer → 默认（index -1 = 使用 PC_Renderer）

### 3. 在场景中添加 RenderGraphTester

将 `RenderGraphTester.cs` 挂载到任意 GameObject，并赋值 `BasicRenderGraph` 资产。运行时 OnGUI 显示节点状态。

### 4. 在 Frame Debugger 中验证

1. **Window → Analysis → Frame Debugger → Enable**
2. 进入 Play 模式
3. 在 Frame Debugger 树中展开：`Main Camera → RenderGraph`
4. 找到名为 `Graph: OpaquePass` 的条目

> **注意（Metal/Vulkan）**：在 Mac（Metal）或 Vulkan 上，RenderGraph 可能将多个共用相同渲染目标的 Pass 合并为一个 NativeRenderPass 的子通道（Subpass）。此时 `Graph: OpaquePass` 会出现在某个 NativeRenderPass 条目的**内部**，而不是顶层。展开 NativeRenderPass 可以找到它。

---

## 常见问题与解决方法

### Q：Frame Debugger 中完全看不到 Pass

按以下顺序诊断：

**步骤 1**：添加临时日志确认 `AddRenderPasses` 被调用
```csharp
public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
{
    Debug.Log("[T09] AddRenderPasses called"); // 若无此日志，说明 Feature 未激活
    ...
}
```

**步骤 2**：确认 `RecordRenderGraph` 被调用
```csharp
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
{
    Debug.Log($"[T09] RecordRenderGraph, activeColor={frameData.Get<UniversalResourceData>().activeColorTexture.IsValid()}");
    ...
}
```

**步骤 3**：检查 `activeColorTexture.IsValid()` 是否为 `True`。若为 `False`，考虑更换注入时机（如 `BeforeRenderingTransparents`）。

常见根因 | 解决方式
---|---
Feature 未激活（`m_Active: 0`）| Renderer Inspector 勾选 Feature
Feature 的 Render Graph 字段为空 | 重新拖拽赋值
`Create()` 在资产反序列化前调用导致 `m_RenderPass == null` | 已在 `AddRenderPasses` 中加入延迟初始化
仅有 `BeginSample`/`EndSample`，无真正 GPU 命令 | 必须使用 `DrawRendererList` 等真正 GPU 命令

### Q：Pass 出现但场景没有变化

`RenderPassNode` 在 `AfterRenderingOpaques` 时机绘制不透明物体，而 URP 已在 GBuffer Pass（Deferred）或 DrawOpaques Pass（Forward）中绘制过这些物体并写入了深度值。我们的 Pass 再次绘制时，对象因深度测试不通过而不产生像素输出。但 **DrawCall 仍然存在**，Pass **在 Frame Debugger 中仍然可见**。

这是预期行为——Tutorial 09 的目标是演示 RenderGraph API 结构，而非实现完整的自定义渲染器。

### Q：Deferred 模式下 `activeDepthTexture.IsValid()` 为 false

Deferred 渲染中，深度缓冲可能在某些时机以不同形式存在。代码中已加入有效性检查：

```csharp
if (resourceData.activeDepthTexture.IsValid())
    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite);
```

若深度无效但仍需深度测试，可考虑改用 `RenderPassEvent.BeforeRenderingTransparents` 时机。

### Q：`BlitNode` 的 Material 如何编写

Material 使用的 Shader 需要：
- 通过 `_BlitTexture` 采样源纹理（`Blitter` 自动绑定）
- 使用 `Blit.hlsl` 中的全屏三角形顶点着色器（或自行实现全屏 pass）

---

## 调试技巧

### Frame Debugger 树形结构

```
Main Camera
└─ RenderGraph
   ├─ ... （URP 内置 Pass）
   ├─ [NativeRenderPass]           ← Metal/Vulkan 合并的 Pass（可展开）
   │  └─ Graph: OpaquePass         ← 我们的 RenderPassNode（可能是子通道）
   └─ Graph: OpaquePass            ← 或作为独立顶层条目（DirectX/OpenGL）
```

展开 Pass 可看到：
- **SetRenderTarget**：由 `SetRenderAttachment` 自动插入，验证渲染目标正确绑定
- **DrawRendererList**：实际的 DrawCall，可看到批次数和顶点数

### 性能标记（CPU Profiler）

```csharp
// RasterCommandBuffer 支持 BeginSample/EndSample（CPU Profiler 可见）
builder.SetRenderFunc((RenderPassData data, RasterGraphContext ctx) =>
{
    ctx.cmd.BeginSample("My Custom Work");
    ctx.cmd.DrawRendererList(data.rendererListHandle);
    ctx.cmd.EndSample("My Custom Work");
});
```

---

## 关键 API 速查

```csharp
// ── 注册 Pass ─────────────────────────────────────────────────────────────
using var builder = renderGraph.AddRasterRenderPass<TPassData>("Pass Name", out var passData);

// ── 资源声明 ──────────────────────────────────────────────────────────────
builder.SetRenderAttachment(textureHandle, index, AccessFlags.Write);  // 颜色写入
builder.SetRenderAttachmentDepth(depthHandle, AccessFlags.ReadWrite);  // 深度读写
builder.UseTexture(textureHandle, AccessFlags.Read);                   // 纹理采样（不能与 SetRenderAttachment 对同一纹理同用）
builder.UseRendererList(rendererListHandle);                           // RendererList 依赖
builder.AllowPassCulling(false);                                       // 禁止 Pass 裁剪

// ── RendererList 创建 ─────────────────────────────────────────────────────
var desc = new RendererListDesc(shaderTagIds, cullResults, camera) { ... };
var handle = renderGraph.CreateRendererList(desc);                     // 返回 RendererListHandle

// ── 临时纹理创建 ──────────────────────────────────────────────────────────
var tempDesc = renderGraph.GetTextureDesc(existingHandle);             // 从已有 Handle 获取描述符
var tempHandle = renderGraph.CreateTexture(tempDesc);                  // 创建临时纹理

// ── SetRenderFunc（GPU 命令）──────────────────────────────────────────────
builder.SetRenderFunc((TPassData data, RasterGraphContext ctx) =>
{
    ctx.cmd.DrawRendererList(data.rendererListHandle);                 // 绘制场景对象
    ctx.cmd.ClearRenderTarget(clearDepth, clearColor, color);         // 清空缓冲
    Blitter.BlitTexture(ctx.cmd, source, scaleBias, material, pass);  // 全屏 Blit（带 material）
    Blitter.BlitTexture(ctx.cmd, source, scaleBias, mipLevel, bilinear); // 全屏 Blit（无 material）
});
```

---

## 练习题

1. **基础**：在 `BasicRenderGraph.rendergraph` 中新增一个 `RenderPassNode`，配置不同的 Pass Name，观察 Frame Debugger 中出现两个独立条目。

2. **进阶**：创建一个简单的灰度后处理 Shader 和 Material，添加 `BlitNode` 到图中，观察 Frame Debugger 中 `Graph: Blit Copy Back` 和 `Graph: Blit <MaterialName>` 两个 Pass。

3. **挑战**：为 `RenderPassNode` 的 `Pass Event` 选项设置不同的值（如 `BeforeRenderingTransparents`），观察 Pass 在 Frame Debugger 中出现的位置变化。

---

**下一步**：继续学习 [教程 10：完整图形化 URP 渲染管线](10_GraphDrivenURP.md)
