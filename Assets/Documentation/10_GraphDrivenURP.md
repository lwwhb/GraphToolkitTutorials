# 教程10: 完整图形化渲染管线

## 概述

本教程是整个系列的终极目标——实现一个完全由图形编辑器驱动的 URP 渲染管线。通过可视化节点图（`.urpgraph`），开发者可以直观地配置渲染流程、添加自定义后处理效果、实现平台/质量分支，无需修改任何运行时代码。

### 学习目标

- 理解如何将图形执行流与 Unity 6 URP RenderGraph API 深度集成
- 掌握 `RecordRenderGraph` 每节点独立添加 `AddRasterRenderPass` 的架构
- 学习 `DrawRendererList` 渲染不透明/透明队列的正确方式
- 实现 `Blitter.BlitTexture` 双 Pass 模式后处理 Blit
- 掌握条件渲染（质量分支 / 平台分支）在 CPU 阶段的决策机制
- 理解 `[SerializeReference]` 对多态运行时节点的必要性

### 前置知识

- 完成教程9（渲染图基础）
- 了解 URP ScriptableRendererFeature 的生命周期
- 熟悉 Unity 6 RenderGraph 系统概念

---

## 系统架构

```
.urpgraph 文件（编辑器节点图）
    ↓ URPGraphImporter.OnImportAsset()
    ↓ URPGraph.CreateRuntimeGraph()
URPGraphRuntime（ScriptableObject，运行时数据）
    ↓ GraphDrivenURPFeature.AddRenderPasses()
GraphDrivenURPPass.RecordRenderGraph()
    ↓ 遍历节点列表，每节点 AddRasterRenderPass
Unity 6 RenderGraph 执行引擎
    ↓
最终画面
```

核心差异（与旧版 URP）：

| 对比项 | 旧版（Unity 5 / URP 12） | Unity 6 URP |
|--------|--------------------------|-------------|
| 入口方法 | `Execute(ScriptableRenderContext, ref RenderingData)` | `RecordRenderGraph(RenderGraph, ContextContainer)` |
| 绘制调用 | `context.DrawRenderers(cullResults, ...)` | `DrawRendererList(rendererListHandle)` |
| Blit 方式 | `cmd.Blit(src, dst, material)` | `Blitter.BlitTexture(cmd, src, scaleBias, material, 0)` |
| 资源管理 | 手动 `CommandBufferPool` | RenderGraph 自动管理 `TextureHandle` |

---

## 项目结构

```
Assets/Tutorials/10_GraphDrivenURP/
├─ Editor/
│  ├─ URPGraph.cs                   # [Graph("urpgraph")] 图定义 + CreateRuntimeGraph()
│  ├─ URPGraphImporter.cs           # [ScriptedImporter(1,"urpgraph")] 导入器
│  ├─ Nodes/
│  │  ├─ URPNode.cs                 # 所有 URP 节点的抽象基类
│  │  ├─ PipelineNodes.cs           # PipelineStartNode / PipelineEndNode
│  │  ├─ Passes/
│  │  │  ├─ PassNodes.cs            # OpaquePassNode / TransparentPassNode / ShadowPassNode / SkyboxPassNode
│  │  │  └─ PostProcessNodes.cs     # PostProcessPassNode / CustomPassNode
│  │  ├─ Resources/
│  │  │  └─ ResourceNodes.cs        # RenderTextureNode（声明占位符）
│  │  └─ Control/
│  │     └─ ControlNodes.cs         # QualityBranchNode / PlatformBranchNode
│  └─ Unity.GraphToolkit.Tutorials.GraphDrivenURP.Editor.asmdef
├─ Runtime/
│  ├─ GraphDrivenURPFeature.cs      # ScriptableRendererFeature，RecordRenderGraph 入口
│  ├─ URPGraphRuntime.cs            # ScriptableObject，[SerializeReference] 节点列表
│  ├─ URPRuntimeNode.cs             # 所有运行时节点定义
│  ├─ URPGraphTester.cs             # 诊断 MonoBehaviour（OnGUI 显示节点状态）
│  └─ Unity.GraphToolkit.Tutorials.GraphDrivenURP.Runtime.asmdef
└─ Examples/                        # 示例 .urpgraph 资产与测试场景
```

---

## 节点系统

### 节点分类总览

| 分类 | 节点 | 菜单路径 | 运行时行为 |
|------|------|----------|------------|
| 管线控制 | Pipeline Start | `URP` | 图的执行入口，`nextNodeIndex` 指向第一个 Pass |
| 管线控制 | Pipeline End | `URP` | 终止遍历 |
| 渲染 Pass | Opaque Pass | `URP/Passes` | `DrawRendererList`（不透明队列，前→后排序） |
| 渲染 Pass | Transparent Pass | `URP/Passes` | `DrawRendererList`（透明队列，后→前排序） |
| 渲染 Pass | Shadow Pass | `URP/Passes` | 标记节点，URP 内置处理，不发 GPU 命令 |
| 渲染 Pass | Skybox Pass | `URP/Passes` | 标记节点，URP 内置处理，不发 GPU 命令 |
| 后处理 | Post Process | `URP/Passes` | 标记节点，URP Volume 系统处理，不发 GPU 命令 |
| 后处理 | Custom Pass | `URP/Passes` | `Blitter.BlitTexture` 双 Pass（material 后处理） |
| 资源 | Render Texture | `URP/Resources` | 声明占位符，RenderGraph 内部管理纹理 |
| 控制 | Quality Branch | `URP/Control` | CPU 阶段按质量等级分支 |
| 控制 | Platform Branch | `URP/Control` | CPU 阶段按平台分支 |

### 标记节点 vs 真实 GPU 节点

**标记节点**（Shadow / Skybox / Post Process）：只有 In/Out 执行端口，不包含任何选项，在 `RecordRenderGraph` 中跳过（执行 `current = node.nextNodeIndex`）。这些功能已由 URP 内置 Pass 处理，教程节点仅作为图中的可视化注释。

**真实 GPU 节点**（Opaque / Transparent / Custom Pass）：每个节点调用独立的 `AddRasterRenderPass`，在 Frame Debugger 中显示为独立条目。

---

## 关键实现细节

### 1. AddOption API — 唯一有效模式

GraphToolkit 中 `AddOption` **只有一种有效写法**：

```csharp
// ✅ 正确：所有类型一律使用 INodeOption + TryGetValue
private INodeOption m_LayerMaskOption;
private INodeOption m_PassEventOption;

protected override void OnDefineOptions(IOptionDefinitionContext context)
{
    m_LayerMaskOption = context.AddOption<LayerMask>("Layer Mask").Build();
    m_PassEventOption = context.AddOption<RenderPassEvent>("Pass Event").Build();
}

public override Runtime.URPRuntimeNode CreateRuntimeNode(URPGraph graph)
{
    LayerMask layerMask = -1;
    m_LayerMaskOption?.TryGetValue(out layerMask);

    RenderPassEvent passEvent = RenderPassEvent.AfterRenderingOpaques;
    m_PassEventOption?.TryGetValue(out passEvent);
    // ...
}
```

```csharp
// ❌ 错误：getter/setter 三参数形式对所有类型均编译失败（CS1501）
// context.AddOption("Layer Mask", () => m_LayerMask, v => m_LayerMask = v).Build();
// context.AddOption("Pass Event", () => m_PassEvent, v => m_PassEvent = v).Build();
// context.AddOption("Bloom",      () => m_Bloom,     v => m_Bloom = v).Build();
```

受影响的类型包括但不限于：`bool`、`int`、`string`、`LayerMask`、`RenderPassEvent`、`Material`、`RenderTextureFormat`。

### 2. IPort 没有 .Node 属性

```csharp
// ✅ 正确：通过 graph.FindNodeForPort 反查端口所属节点
var connectedPort = graph.GetConnectedInputPort(m_ExecutionOut);
if (connectedPort != null && graph.FindNodeForPort(connectedPort) is URPNode urpNode)
    return urpNode;
```

```csharp
// ❌ 错误：IPort 接口无 .Node 属性，编译失败
var nextNode = connectedPort.Node as URPNode;
```

### 3. GetNodeIndex 必须与 CreateRuntimeGraph 过滤逻辑对齐

`CreateRuntimeGraph()` 只将 `URPNode` 子类加入 `runtimeGraph.nodes`，因此 `GetNodeIndex` 也必须只计数 `URPNode` 实例，否则遇到非 `URPNode` 节点时索引会错位。

```csharp
// ✅ 正确：只计数 URPNode 实例，与 CreateRuntimeGraph 的过滤逻辑保持一致
public int GetNodeIndex(URPGraph graph)
{
    int idx = 0;
    foreach (var node in graph.GetNodes())
    {
        if (node is URPNode)
        {
            if (node == this) return idx;
            idx++;
        }
    }
    return -1;
}
```

```csharp
// ❌ 错误：遍历全部 INode，若存在非 URPNode 节点则索引偏移
// runtimeGraph.nodes 中 Opaque 在 index=2，但 GetNodeIndex 返回 3
var allNodes = new List<INode>(graph.GetNodes());
for (int i = 0; i < allNodes.Count; i++)
    if (allNodes[i] == this) return i;
```

### 4. [SerializeReference] 是多态序列化的关键

```csharp
// URPGraphRuntime.cs
public class URPGraphRuntime : ScriptableObject
{
    // ✅ [SerializeReference] — 支持 OpaquePassNode / QualityBranchNode 等子类的多态序列化
    [SerializeReference]
    public List<URPRuntimeNode> nodes = new List<URPRuntimeNode>();
}
```

```csharp
// ❌ [SerializeField] — 只序列化基类字段，运行时 is 类型检查全部失败
[SerializeField]
public List<URPRuntimeNode> nodes = new List<URPRuntimeNode>();
// node is OpaquePassNode → 始终 false，所有节点被当作未知节点跳过
```

---

## 编辑器图（URPGraph）

```csharp
[Graph("urpgraph", GraphOptions.Default)]
[Serializable]
internal class URPGraph : Graph
{
    [MenuItem("Assets/Create/Graph Toolkit/URP Graph")]
    static void CreateGraphAssetFile()
        => GraphDatabase.PromptInProjectBrowserToCreateNewAsset<URPGraph>();

    // 将编辑器图转为运行时图（在 Importer 中调用）
    public Runtime.URPGraphRuntime CreateRuntimeGraph()
    {
        var runtimeGraph = ScriptableObject.CreateInstance<Runtime.URPGraphRuntime>();
        var allNodes = new List<INode>(GetNodes());

        for (int i = 0; i < allNodes.Count; i++)
        {
            if (allNodes[i] is URPNode urpNode)
            {
                runtimeGraph.nodes.Add(urpNode.CreateRuntimeNode(this));
                if (allNodes[i] is PipelineStartNode)
                    runtimeGraph.startNodeIndex = runtimeGraph.nodes.Count - 1;
            }
        }
        return runtimeGraph;
    }

    // 验证图是否满足运行条件
    public bool Validate(out string errorMessage)
    {
        errorMessage = string.Empty;
        var startNode = FindStartNode();
        if (startNode == null)
        {
            errorMessage = "URP graph must have a Pipeline Start node";
            return false;
        }
        // 检查 Start 节点是否有出线：若无连接，nextNodeIndex=-1，遍历循环立即退出，
        // Frame Debugger 中看不到任何 Pass，且无任何报错提示。
        if (startNode.GetNextNode(this) == null)
        {
            errorMessage = "Pipeline Start node has no outgoing connection. " +
                           "Connect it to the first pass node or the Pipeline End node.";
            return false;
        }
        return true;
    }
}
```

---

## Importer（URPGraphImporter）

```csharp
[ScriptedImporter(1, "urpgraph")]
internal class URPGraphImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        // 注意：Graph 不是 UnityEngine.Object，不能 AddObjectToAsset(graph)
        // 只能将 CreateRuntimeGraph() 产生的 ScriptableObject 作为主资产
        var graph = GraphDatabase.LoadGraphForImporter<URPGraph>(ctx.assetPath);

        Runtime.URPGraphRuntime runtimeGraph;
        if (graph != null)
        {
            if (!graph.Validate(out string errorMessage))
                Debug.LogWarning($"[URPGraph] {ctx.assetPath}: {errorMessage}");
            runtimeGraph = graph.CreateRuntimeGraph();
            Debug.Log($"[URPGraph] Imported '{Path.GetFileNameWithoutExtension(ctx.assetPath)}': " +
                      $"{runtimeGraph.nodes.Count} nodes, start={runtimeGraph.startNodeIndex}");
        }
        else
        {
            runtimeGraph = ScriptableObject.CreateInstance<Runtime.URPGraphRuntime>();
        }

        runtimeGraph.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
        ctx.AddObjectToAsset("main", runtimeGraph);
        ctx.SetMainObject(runtimeGraph);
    }
}
```

---

## 运行时核心：RecordRenderGraph

`GraphDrivenURPPass` 继承 `ScriptableRenderPass`，实现 `RecordRenderGraph`。每帧 URP 调用此方法时，遍历运行时图的节点链表：

```csharp
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
{
    var startNode = m_URPGraph.GetStartNode();
    if (startNode == null) { Debug.LogWarning("..."); return; }

    // ContextContainer 三个数据对象
    var resourceData  = frameData.Get<UniversalResourceData>();   // TextureHandle
    var cameraData    = frameData.Get<UniversalCameraData>();     // Camera
    var renderingData = frameData.Get<UniversalRenderingData>(); // CullingResults

    // 诊断日志：前两帧输出节点数与 activeColorTexture 有效性，便于排查配置问题
    if (Time.frameCount <= 2)
        Debug.Log($"[GraphDrivenURP] Recording: {m_URPGraph.nodes.Count} nodes, " +
                  $"startIndex={m_URPGraph.startNodeIndex}, " +
                  $"activeColorValid={resourceData.activeColorTexture.IsValid()}");

    int current = startNode.nextNodeIndex;
    while (current >= 0)
    {
        var node = m_URPGraph.GetNode(current);
        if (node == null) break;

        if (node is OpaquePassNode opaque)
        {
            RecordDrawPass(renderGraph, resourceData, cameraData, renderingData,
                "Graph: Opaque Pass",
                RenderQueueRange.opaque,
                SortingCriteria.CommonOpaque,
                opaque.layerMask);
            current = opaque.nextNodeIndex;
        }
        else if (node is TransparentPassNode transparent)
        {
            RecordDrawPass(renderGraph, resourceData, cameraData, renderingData,
                "Graph: Transparent Pass",
                RenderQueueRange.transparent,
                SortingCriteria.CommonTransparent,
                transparent.layerMask);
            current = transparent.nextNodeIndex;
        }
        else if (node is CustomPassNode custom)
        {
            // 教学重点：有 material 才执行 Blit（无 material = 跳过）
            if (custom.material != null)
                RecordBlitPass(renderGraph, resourceData, custom);
            current = custom.nextNodeIndex;
        }
        else if (node is QualityBranchNode quality)
        {
            // 分支在 RecordRenderGraph（CPU 阶段）决策，不是 GPU 命令
            current = QualitySettings.GetQualityLevel() >= quality.minimumQualityForHigh
                ? quality.highQualityIndex
                : quality.lowQualityIndex;
        }
        else if (node is PlatformBranchNode platform)
        {
            current = Application.isMobilePlatform ? platform.mobileIndex : platform.pcIndex;
        }
        else if (node is PipelineEndNode)
        {
            break;
        }
        else
        {
            // Shadow / Skybox / PostProcess / RenderTexture = 标记节点，跳过
            current = node.nextNodeIndex;
        }
    }
}
```

### DrawRendererList Pass

```csharp
private void RecordDrawPass(
    RenderGraph rg, UniversalResourceData resourceData,
    UniversalCameraData cameraData, UniversalRenderingData renderingData,
    string passName, RenderQueueRange renderQueueRange,
    SortingCriteria sortingCriteria, LayerMask layerMask)
{
    if (!resourceData.activeColorTexture.IsValid())
    {
        Debug.LogWarning($"[GraphDrivenURP] '{passName}': activeColorTexture is invalid. " +
                         "Ensure a URP Renderer with this feature is active for the camera.");
        return;
    }

    using var builder = rg.AddRasterRenderPass<DrawPassData>(passName, out var passData);

    // RendererListDesc 描述绘制哪些对象
    var desc = new RendererListDesc(k_ShaderTagIds, renderingData.cullResults, cameraData.camera)
    {
        sortingCriteria  = sortingCriteria,
        renderQueueRange = renderQueueRange,
        layerMask        = layerMask,
    };
    passData.rendererListHandle = rg.CreateRendererList(desc);

    builder.UseRendererList(passData.rendererListHandle);  // 防止 Pass 被 RenderGraph 裁剪
    builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
    if (resourceData.activeDepthTexture.IsValid())
        builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite);
    builder.AllowPassCulling(false);

    builder.SetRenderFunc((DrawPassData data, RasterGraphContext ctx) =>
    {
        ctx.cmd.DrawRendererList(data.rendererListHandle);
    });
}
```

**关键要点**：
- `AllowPassCulling(false)` — 防止 RenderGraph 将没有下游读取者的 Pass 裁剪掉
- `SetRenderAttachment` + `SetRenderAttachmentDepth` 使 Pass 在 Frame Debugger 中可见
- `k_ShaderTagIds` 包含 `"SRPDefaultUnlit"`、`"UniversalForward"`、`"UniversalForwardOnly"` 三种 LightMode

### Blitter 双 Pass 模式

同一 `RasterPass` 内不能同时读写同一纹理。CustomPassNode 需要将 `activeColorTexture` 经过 `material` 处理后写回，因此需要两个 Pass：

```csharp
private void RecordBlitPass(RenderGraph rg, UniversalResourceData resourceData, CustomPassNode node)
{
    // 创建与 activeColor 同规格的临时纹理
    var tempDesc         = rg.GetTextureDesc(resourceData.activeColorTexture);
    tempDesc.name        = "URPGraph_Blit_Temp";
    tempDesc.clearBuffer = false;
    var tempTexture      = rg.CreateTexture(tempDesc);

    // Pass A：activeColor → tempTexture（应用 material 效果）
    using (var builder = rg.AddRasterRenderPass<BlitPassData>("Graph: " + node.passName, out var pd))
    {
        pd.sourceTexture = resourceData.activeColorTexture;
        pd.material      = node.material;

        builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
        builder.SetRenderAttachment(tempTexture, 0, AccessFlags.Write);
        builder.AllowPassCulling(false);

        builder.SetRenderFunc((BlitPassData data, RasterGraphContext ctx) =>
        {
            // scaleBias = (1,1,0,0) 表示全屏（UV 从 0 到 1，不偏移）
            Blitter.BlitTexture(ctx.cmd, data.sourceTexture, new Vector4(1, 1, 0, 0), data.material, 0);
        });
    }

    // Pass B：tempTexture → activeColor（无 material，原样复制回颜色缓冲）
    using (var builder = rg.AddRasterRenderPass<BlitPassData>("Graph: " + node.passName + " Copy Back", out var pd))
    {
        pd.sourceTexture = tempTexture;
        pd.material      = null;

        builder.UseTexture(tempTexture, AccessFlags.Read);
        builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
        builder.AllowPassCulling(false);

        builder.SetRenderFunc((BlitPassData data, RasterGraphContext ctx) =>
        {
            // passIndex = 0，mipLevel = false
            Blitter.BlitTexture(ctx.cmd, data.sourceTexture, new Vector4(1, 1, 0, 0), 0, false);
        });
    }
}
```

---

## 条件渲染节点

### QualityBranchNode

```csharp
[Node("Quality Branch", "URP/Control")]
[UseWithGraph(typeof(URPGraph))]
[Serializable]
internal class QualityBranchNode : URPNode
{
    private INodeOption m_MinQualityOption;
    private IPort m_HighQualityPort;
    private IPort m_LowQualityPort;

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        // 分支节点：一个 In + 两个 Out（无执行流 Out，只有命名输出端口）
        m_ExecutionIn = context.AddInputPort("In").WithConnectorUI(PortConnectorUI.Arrowhead).Build();
        m_HighQualityPort = context.AddOutputPort("High Quality").WithConnectorUI(PortConnectorUI.Arrowhead).Build();
        m_LowQualityPort  = context.AddOutputPort("Low Quality") .WithConnectorUI(PortConnectorUI.Arrowhead).Build();
    }

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        m_MinQualityOption = context.AddOption<int>("Min Quality For High").Build();
    }

    public override Runtime.URPRuntimeNode CreateRuntimeNode(URPGraph graph)
    {
        int minQuality = 2;
        m_MinQualityOption?.TryGetValue(out minQuality);
        minQuality = Mathf.Clamp(minQuality, 0, 5);

        var highNode = GetHighQualityNode(graph);
        var lowNode  = GetLowQualityNode(graph);
        return new Runtime.QualityBranchNode
        {
            minimumQualityForHigh = minQuality,
            highQualityIndex = highNode != null ? highNode.GetNodeIndex(graph) : -1,
            lowQualityIndex  = lowNode  != null ? lowNode.GetNodeIndex(graph)  : -1
        };
    }
}
```

**设计要点**：
- 分支节点没有 `m_ExecutionOut`，只有两个命名输出端口
- `GetHighQualityNode` / `GetLowQualityNode` 通过 `graph.GetConnectedInputPort(port)` 获取连接的下游端口，再用 `graph.FindNodeForPort` 反查节点
- 运行时决策在 `RecordRenderGraph`（CPU）完成，两条路径的 Pass 数量可以不同

### PlatformBranchNode

原理与 `QualityBranchNode` 相同，使用 `Application.isMobilePlatform` 在 `RecordRenderGraph` 阶段决策。

---

## 示例图结构

典型的标准渲染管线图（连线顺序）：

```
[Pipeline Start]
    ↓
[Shadow Pass]      (标记，跳过)
    ↓
[Opaque Pass]      (DrawRendererList，不透明队列)
    ↓
[Skybox Pass]      (标记，跳过)
    ↓
[Transparent Pass] (DrawRendererList，透明队列)
    ↓
[Post Process]     (标记，跳过；Volume 系统处理)
    ↓
[Pipeline End]
```

带质量分支的扩展图：

```
[Pipeline Start]
    ↓
[Quality Branch]  ── High ──→ [Custom Pass (material)]
                  └─ Low  ──→ （直接到 End）
    ↓（两路均连接）
[Pipeline End]
```

---

## 运行时诊断工具（URPGraphTester）

`URPGraphTester` 是一个诊断用 MonoBehaviour，在 Play 模式的屏幕左上角显示运行时图的节点列表：

```csharp
public class URPGraphTester : MonoBehaviour
{
    [SerializeField] private URPGraphRuntime m_URPGraph;

    private void OnGUI()
    {
        // 显示节点数量、startIndex，以及每个节点的类型和 nextNodeIndex
        for (int i = 0; i < m_URPGraph.nodes.Count; i++)
        {
            var node = m_URPGraph.nodes[i];
            string label = node != null
                ? $"  [{i}] {node.GetType().Name}  → next: {node.nextNodeIndex}"
                : $"  [{i}] null";
            // ...
        }
    }
}
```

使用方法：
1. 将 `URPGraphTester` 组件挂载到场景中任意 GameObject
2. 将 `.urpgraph` 资产拖入 `URP Graph` 字段
3. Play 模式下查看左上角节点列表，确认 `nextNodeIndex` 链条正确

---

## 配置与使用

### 步骤一：创建 URP Graph 资产

在 Project 窗口右键 → `Create / Graph Toolkit / URP Graph`，创建 `.urpgraph` 文件。

### 步骤二：在图编辑器中搭建节点

1. 双击 `.urpgraph` 文件打开图编辑器
2. 添加 `Pipeline Start` 节点（`URP` 类别）
3. 按需添加 Pass 节点（`URP/Passes`）、控制节点（`URP/Control`）
4. 用箭头执行线将节点按渲染顺序连接
5. 以 `Pipeline End` 结束
6. 保存（Ctrl+S），Importer 自动运行并生成 `URPGraphRuntime`

### 步骤三：添加 Renderer Feature 并赋值

1. 在 Project 窗口找到 URP Renderer 资产（如 `PC_Renderer`）
2. Inspector → `Add Renderer Feature` → `Graph Driven URP Feature`
3. **将生成的 `URPGraphRuntime` 拖入 Feature 的 `URP Graph` 字段**

> ⚠️ **最常见的错误**：Feature 已添加到 Renderer，但 `URP Graph` 字段为空（`None`）。此时 `AddRenderPasses()` 第一行即返回，Pass 从未入队，Frame Debugger 中看不到任何自定义条目，且 Console 不会报任何错误。
>
> `URPGraphTester` 组件（挂在场景 GameObject 上）的 `URP Graph` 字段与此字段相互独立——前者仅用于 OnGUI 诊断显示，后者控制实际渲染。两者必须分别赋值。

### 步骤四：验证

- Play 模式下打开 `Window / Analysis / Frame Debugger`
- 展开 `RenderGraph` → `Camera.Universal.RenderRequest`
- 应看到我们的自定义 Pass 与 URP 内置 Pass 交织出现（见下节完整层级）
- Console 前两帧输出 `[GraphDrivenURP] Recording: 7 nodes, startIndex=0, activeColorValid=True` 说明图已正确加载
- 如未看到自定义 Pass，将 `URPGraphTester` 挂载到场景对象，确认 `nextNodeIndex` 链条完整

---

## Frame Debugger 层级结构

`StandardPipeline.urpgraph` 运行时，Frame Debugger 中的完整渲染层级如下：

```
RenderGraph
└── Camera.Universal.RenderRequest
    ├── Main Shadow Map          ← URP 内置（对应图中 Shadow Pass 标记节点）
    ├── GBuffer Pass             ← URP 内置（Deferred 渲染几何阶段）
    ├── Deferred Lighting        ← URP 内置（延迟光照计算）
    ├── Graph: Opaque Pass       ← ✅ 我们的自定义 Pass（OpaquePassNode）
    ├── Draw Skybox              ← URP 内置（对应图中 Skybox Pass 标记节点）
    ├── Graph: Transparent Pass  ← ✅ 我们的自定义 Pass（TransparentPassNode）
    └── URP Post Processing      ← URP 内置（对应图中 Post Process 标记节点）
```

**只有 `Graph: Opaque Pass` 和 `Graph: Transparent Pass` 是我们的 Feature 注入的。** 其余均为 URP 自动管理的内置阶段。这正是"标记节点"设计的体现：图中的 Shadow / Skybox / Post Process 节点是可视化占位符，告诉读图者管线在这里有这些阶段，但实际 GPU 命令由 URP 负责发出，我们不重复注入。

---

## 常见问题

### Q: Frame Debugger 中完全看不到任何自定义 Pass？

按优先级逐项排查：

1. **Renderer Feature 的 `URP Graph` 字段为空（最常见原因）**
   `AddRenderPasses()` 首行 `if (m_URPGraph == null) return;` 静默退出，不报任何错误。
   修复：打开 `PC_Renderer.asset` Inspector → `Graph Driven URP Feature` → 将 `StandardPipeline.urpgraph` 拖入 `URP Graph` 字段。

2. **Console 中有 `[GraphDrivenURP] Recording: 0 nodes` 日志**
   图导入失败，运行时图为空。右键 `.urpgraph` → `Reimport`，检查 Console 是否有 `[URPGraph]` Warning。

3. **`[SerializeReference]` 缺失**
   `URPGraphRuntime.nodes` 使用 `[SerializeField]` 时，多态节点反序列化失败，`is OpaquePassNode` 始终为 `false`，节点全被当作标记节点跳过。确认使用 `[SerializeReference]`。

4. **Console 中有 `activeColorTexture is invalid` 警告**
   当前摄像机不使用包含此 Feature 的 URP Renderer。检查摄像机的 `Renderer` 设置指向 `PC_Renderer`。

5. **`AllowPassCulling(false)` 缺失**
   RenderGraph 可能将没有下游读者的 Pass 裁剪掉，Frame Debugger 中看不到。

### Q: Frame Debugger 中只有 2 个自定义 Pass，Shadow / Skybox / Post Process 没有出现，正常吗？

**完全正常，这是设计如此。**

Shadow、Skybox、PostProcess 是**标记节点**：在 `RecordRenderGraph` 中执行 `current = node.nextNodeIndex`，不向 RenderGraph 注入任何 GPU 命令。这三个阶段由 URP 内置 Pass 处理：

| 图中的标记节点 | Frame Debugger 中对应的 URP 内置 Pass |
|--------------|---------------------------------------|
| Shadow Pass  | `Main Shadow Map` |
| Skybox Pass  | `Draw Skybox` |
| Post Process | `URP Post Processing` |

标记节点的教学意义：图代表**完整的概念管线**（含 URP 内置阶段），但不是每个阶段都需要注入自定义 GPU 命令。这帮助读者理解整个渲染流程，而无需重复实现 URP 已处理好的功能。

### Q: AddOption 编译报错 CS1501？

GraphToolkit 的 `AddOption` **不支持** getter/setter 三参数形式。对所有类型（包括 `bool`、`int`、`LayerMask`、`RenderPassEvent` 等）一律使用：
```csharp
m_Option = context.AddOption<T>("Name").Build();
// 读取时：T val = default; m_Option?.TryGetValue(out val);
```

### Q: 运行时 is 类型检查失败，所有节点被跳过？

检查 `URPGraphRuntime.nodes` 是否标记了 `[SerializeReference]`（而非 `[SerializeField]`）。

### Q: CustomPassNode 的 Material 不生效？

确认 Material 使用的 Shader 包含从 `_BlitTexture` 采样源纹理的逻辑（Blitter 通过该 uniform 传递源纹理）。

### Q: Quality Branch 的分支在哪里决策？

在 CPU 端的 `RecordRenderGraph` 中，通过 `QualitySettings.GetQualityLevel()` 决策，**不是** GPU 阶段的条件。每帧记录 RenderGraph 时重新计算，改变质量设置后下帧立即生效。

---

## 核心 API 速查

| 需求 | 正确 API |
|------|----------|
| 添加一个 RasterRenderPass | `rg.AddRasterRenderPass<TPassData>(name, out passData)` |
| 设置颜色输出 | `builder.SetRenderAttachment(texHandle, index, AccessFlags.Write)` |
| 设置深度输出 | `builder.SetRenderAttachmentDepth(texHandle, AccessFlags.ReadWrite)` |
| 声明纹理为只读输入 | `builder.UseTexture(texHandle, AccessFlags.Read)` |
| 声明 RendererList | `builder.UseRendererList(rendererListHandle)` |
| 防止 Pass 被裁剪 | `builder.AllowPassCulling(false)` |
| 提交 GPU 命令 | `builder.SetRenderFunc((data, ctx) => { ctx.cmd.DrawRendererList(...); })` |
| 全屏 Blit（带 material） | `Blitter.BlitTexture(ctx.cmd, src, new Vector4(1,1,0,0), material, 0)` |
| 全屏 Blit（无 material） | `Blitter.BlitTexture(ctx.cmd, src, new Vector4(1,1,0,0), 0, false)` |
| 创建临时纹理 | `rg.CreateTexture(rg.GetTextureDesc(srcHandle))` |
| 获取活跃颜色/深度 Handle | `frameData.Get<UniversalResourceData>().activeColorTexture` |
| 获取 Camera 对象 | `frameData.Get<UniversalCameraData>().camera` |
| 获取 CullingResults | `frameData.Get<UniversalRenderingData>().cullResults` |

---

## 与教程9的对比

| 对比维度 | 教程9（渲染图基础） | 教程10（完整图形化渲染管线） |
|----------|--------------------|-----------------------------|
| 节点类型 | 单一：`RenderNode`（DrawRenderers） | 多种：Pass / PostProcess / Resource / Control |
| 分支支持 | 无 | Quality Branch + Platform Branch |
| 后处理 | 无 | Custom Pass（Blitter 双 Pass） |
| 标记节点 | 无 | Shadow / Skybox / PostProcess |
| 诊断工具 | `RenderGraphTester` | `URPGraphTester` |
| 适用场景 | 学习 RenderGraph 基础 | 生产级图形化渲染管线原型 |

---

## 下一步学习

- 阅读 Unity 官方文档：`ScriptableRendererFeature`、`RenderGraph`、`Blitter`
- 探索向 `OpaquePassNode` 和 `TransparentPassNode` 添加 Per-Object Material Override 能力
- 尝试在 `QualityBranchNode` 的两条分支路径中使用不同的 Custom Pass 效果
- 将此框架扩展为完整的多摄像机/多视口支持
