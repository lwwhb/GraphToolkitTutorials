# 教程9: 渲染图基础

## 概述

本教程介绍如何将GraphToolkit与Unity的URP（Universal Render Pipeline）渲染管线集成，创建基础的图形驱动渲染系统。这是迈向完整图形化渲染管线的第一步。

### 学习目标

- 理解URP渲染管线架构
- 掌握ScriptableRendererFeature的使用
- 学习RenderPass系统的设计
- 实现基础的渲染图节点
- 理解编辑器图形到运行时渲染的转换

### 前置知识

- 完成教程3（执行流图形）
- 了解URP基础概念
- 熟悉Unity渲染管线

---

## URP渲染管线架构

### 核心组件

```
UniversalRenderPipeline
├─ UniversalRenderPipelineAsset    # 管线配置资产
├─ UniversalRenderer               # 渲染器
│  ├─ ScriptableRendererFeature    # 可扩展特性（我们的切入点）
│  │  └─ ScriptableRenderPass      # 具体的渲染Pass
│  └─ RenderPassQueue              # Pass执行队列
└─ RenderingData                   # 渲染数据（相机、光照等）
```

### 渲染流程

```
每帧渲染:
1. Camera.Render() 触发
2. UniversalRenderPipeline.Render()
3. 遍历所有 ScriptableRendererFeature
4. 调用 AddRenderPasses() 添加Pass
5. 按 RenderPassEvent 顺序执行Pass
6. 最终输出到屏幕
```

### RenderPassEvent 执行顺序

```
BeforeRendering
BeforeRenderingShadows
AfterRenderingShadows
BeforeRenderingPrePasses
AfterRenderingPrePasses
BeforeRenderingGbuffer          ← 延迟渲染G-Buffer
AfterRenderingGbuffer
BeforeRenderingDeferredLights   ← 延迟光照
AfterRenderingDeferredLights
BeforeRenderingOpaques          ← 不透明物体
AfterRenderingOpaques
BeforeRenderingSkybox           ← 天空盒
AfterRenderingSkybox
BeforeRenderingTransparents     ← 透明物体
AfterRenderingTransparents
BeforeRenderingPostProcessing   ← 后处理
AfterRenderingPostProcessing
AfterRendering
```

---

## 项目结构

```
Assets/Tutorials/09_RenderGraphBasics/
├─ Editor/
│  ├─ RenderGraph.cs                    # 渲染图形定义
│  ├─ RenderGraphImporter.cs            # 资产导入器（.rendergraph）
│  ├─ Nodes/
│  │  ├─ RenderNode.cs                  # 节点基类
│  │  └─ RenderNodes.cs                 # 具体节点实现
│  └─ Unity.GraphToolkit.Tutorials.RenderGraphBasics.Editor.asmdef
├─ Runtime/
│  ├─ GraphDrivenRendererFeature.cs     # ScriptableRendererFeature
│  ├─ RenderGraphRuntime.cs             # 运行时图形
│  ├─ RenderRuntimeNode.cs              # 运行时节点基类
│  └─ Unity.GraphToolkit.Tutorials.RenderGraphBasics.Runtime.asmdef
└─ Examples/
   └─ BasicRenderGraph.rendergraph      # 基础渲染图示例
```

---

## 核心实现

### 1. 渲染图形定义

```csharp
using Unity.GraphToolkit.Editor;

namespace GraphToolkitTutorials.RenderGraphBasics
{
    /// <summary>
    /// 渲染图形 - 定义渲染管线的执行流程
    /// </summary>
    [Graph("rendergraph", GraphOptions.None)]
    internal class RenderGraph : Graph
    {
        /// <summary>
        /// 创建运行时渲染图
        /// </summary>
        public Runtime.RenderGraphRuntime CreateRuntime()
        {
            var runtime = new Runtime.RenderGraphRuntime();

            // 收集所有渲染节点
            foreach (var node in Nodes)
            {
                if (node is RenderNode renderNode)
                {
                    runtime.AddNode(renderNode.CreateRuntimeNode(this));
                }
            }

            // 找到起始节点
            foreach (var node in Nodes)
            {
                if (node is PipelineStartNode startNode)
                {
                    runtime.StartNodeIndex = startNode.GetNodeIndex(this);
                    break;
                }
            }

            return runtime;
        }
    }
}
```

### 2. 渲染Pass节点

```csharp
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GraphToolkitTutorials.RenderGraphBasics
{
    /// <summary>
    /// 渲染Pass节点 - 代表一个URP渲染Pass
    /// </summary>
    [Node("Render Pass", "Rendering")]
    [UseWithGraph(typeof(RenderGraph))]
    internal class RenderPassNode : RenderNode
    {
        [SerializeField] private RenderPassEvent m_PassEvent = RenderPassEvent.AfterRenderingOpaques;
        [SerializeField] private string m_PassName = "Custom Pass";

        private IPort m_InputPort;
        private IPort m_OutputPort;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_InputPort = context.AddInputPort("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            m_OutputPort = context.AddOutputPort("Out")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Pass Name",
                () => m_PassName,
                v => m_PassName = v)
                .Delayed().Build();
        }

        public override Runtime.RenderRuntimeNode CreateRuntimeNode(RenderGraph graph)
        {
            var runtimeNode = new Runtime.RenderPassRuntimeNode
            {
                passEvent = m_PassEvent,
                passName = m_PassName
            };

            // 获取下一个节点
            var connectedPort = graph.GetConnectedInputPort(m_OutputPort);
            if (connectedPort?.Node is RenderNode nextNode)
                runtimeNode.nextNodeIndex = nextNode.GetNodeIndex(graph);
            else
                runtimeNode.nextNodeIndex = -1;

            return runtimeNode;
        }
    }
}
```

### 3. ScriptableRendererFeature集成

```csharp
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GraphToolkitTutorials.RenderGraphBasics.Runtime
{
    /// <summary>
    /// 图形驱动的渲染特性 - 作为URP和渲染图之间的桥梁
    /// </summary>
    public class GraphDrivenRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private RenderGraph m_GraphAsset;

        private RenderGraphRuntime m_Runtime;
        private List<GraphRenderPass> m_ActivePasses = new List<GraphRenderPass>();

        /// <summary>
        /// 初始化 - 在Feature创建时调用
        /// </summary>
        public override void Create()
        {
            if (m_GraphAsset == null) return;

            m_Runtime = m_GraphAsset.CreateRuntime();
            BuildPasses();
        }

        /// <summary>
        /// 添加渲染Pass - 每帧调用
        /// </summary>
        public override void AddRenderPasses(ScriptableRenderer renderer,
                                             ref RenderingData renderingData)
        {
            foreach (var pass in m_ActivePasses)
            {
                renderer.EnqueuePass(pass);
            }
        }

        /// <summary>
        /// 根据运行时图形构建Pass列表
        /// </summary>
        private void BuildPasses()
        {
            m_ActivePasses.Clear();

            // 遍历运行时节点，为每个RenderPassNode创建对应的ScriptableRenderPass
            int nodeIndex = m_Runtime.StartNodeIndex;
            while (nodeIndex >= 0)
            {
                var node = m_Runtime.GetNode(nodeIndex);
                if (node is RenderPassRuntimeNode passNode)
                {
                    var renderPass = new GraphRenderPass(passNode.passName, passNode.passEvent);
                    m_ActivePasses.Add(renderPass);
                }
                nodeIndex = node.nextNodeIndex;
            }
        }

        protected override void Dispose(bool disposing)
        {
            foreach (var pass in m_ActivePasses)
                pass.Dispose();
            m_ActivePasses.Clear();
        }
    }
}
```

### 4. 自定义RenderPass

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GraphToolkitTutorials.RenderGraphBasics.Runtime
{
    /// <summary>
    /// 图形驱动的渲染Pass
    /// </summary>
    public class GraphRenderPass : ScriptableRenderPass, System.IDisposable
    {
        private string m_PassName;
        private Material m_BlitMaterial;

        public GraphRenderPass(string passName, RenderPassEvent passEvent)
        {
            m_PassName = passName;
            renderPassEvent = passEvent;
            profilingSampler = new ProfilingSampler(passName);
        }

        /// <summary>
        /// 执行渲染Pass
        /// </summary>
        public override void Execute(ScriptableRenderContext context,
                                     ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get(m_PassName);

            using (new ProfilingScope(cmd, profilingSampler))
            {
                // 在这里执行实际的渲染命令
                // 例如：Blit、DrawMesh等
                ExecuteRenderCommands(cmd, ref renderingData);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        protected virtual void ExecuteRenderCommands(CommandBuffer cmd,
                                                      ref RenderingData renderingData)
        {
            // 子类重写此方法实现具体渲染逻辑
        }

        public void Dispose()
        {
            if (m_BlitMaterial != null)
                Object.DestroyImmediate(m_BlitMaterial);
        }
    }
}
```

---

## 渲染图执行流程

```
Unity渲染帧
    └─ GraphDrivenRendererFeature.AddRenderPasses()
        └─ 遍历m_ActivePasses
            └─ renderer.EnqueuePass(pass)
                └─ URP按RenderPassEvent顺序执行
                    └─ GraphRenderPass.Execute()
                        └─ 执行CommandBuffer命令
```

---

## 配置步骤

1. **创建渲染图资产**
   - 在Project窗口右键 → Create → Rendering → Render Graph
   - 在图形编辑器中添加节点

2. **配置RendererFeature**
   - 选择URP Renderer资产
   - 点击 "Add Renderer Feature"
   - 选择 "Graph Driven Renderer Feature"
   - 将渲染图资产赋值给Feature

3. **验证配置**
   - 进入Play模式
   - 在Frame Debugger中查看Pass执行情况

---

## 调试工具

### Frame Debugger

在Unity菜单 Window → Analysis → Frame Debugger 中可以查看每个Pass的执行情况：

```
Frame Debugger
└─ RenderCamera
   ├─ GraphDrivenRendererFeature
   │  ├─ Custom Pass 1 (BeforeRenderingOpaques)
   │  └─ Custom Pass 2 (AfterRenderingTransparents)
   └─ ...
```

### 性能分析

使用ProfilingSampler标记Pass，在Profiler中查看性能：

```csharp
// 在Pass中添加性能标记
profilingSampler = new ProfilingSampler("MyCustomPass");

// 在Execute中使用
using (new ProfilingScope(cmd, profilingSampler))
{
    // 渲染命令
}
```

---

## 练习题

1. **基础**: 创建一个简单的渲染图，包含一个自定义Pass，在屏幕上绘制一个颜色叠加效果
2. **进阶**: 添加多个Pass节点，实现Pass的顺序执行
3. **挑战**: 实现一个条件Pass节点，根据质量设置决定是否执行某个Pass

---

## 常见问题

**Q: 为什么我的Pass没有执行？**
A: 检查以下几点：
- RendererFeature是否已添加到Renderer资产
- 渲染图资产是否已赋值
- RenderPassEvent是否设置正确

**Q: 如何在Pass中访问相机的颜色缓冲？**
A: 使用renderingData.cameraData.renderer.cameraColorTarget：
```csharp
var colorTarget = renderingData.cameraData.renderer.cameraColorTarget;
cmd.Blit(colorTarget, colorTarget, m_BlitMaterial);
```

**Q: 多个Pass之间如何传递数据？**
A: 使用RenderTexture作为中间缓冲，在Pass之间共享。

---

**下一步**: 继续学习 [教程10: 完整图形化渲染管线](10_GraphDrivenURP.md)
                                                    ref RenderingData renderingData)
        {
            foreach (var pass in m_ActivePasses)
            {
                renderer.EnqueuePass(pass);
            }
        }

        /// <summary>
        /// 根据运行时图形构建Pass列表
        /// </summary>
        private void BuildPasses()
        {
            m_ActivePasses.Clear();

            if (m_Runtime.StartNodeIndex < 0) return;

            int currentIndex = m_Runtime.StartNodeIndex;
            while (currentIndex >= 0)
            {
                var node = m_Runtime.GetNode(currentIndex);
                if (node is RenderPassRuntimeNode passNode)
                {
                    var pass = new GraphRenderPass(passNode);
                    m_ActivePasses.Add(pass);
                    currentIndex = passNode.nextNodeIndex;
                }
                else
                {
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 图形渲染Pass - 实际执行渲染的Pass
    /// </summary>
    public class GraphRenderPass : ScriptableRenderPass
    {
        private RenderPassRuntimeNode m_Node;

        public GraphRenderPass(RenderPassRuntimeNode node)
        {
            m_Node = node;
            renderPassEvent = node.passEvent;
        }

        public override void Execute(ScriptingRenderContext context,
                                     ref RenderingData renderingData)
        {
            // 这里执行实际的渲染命令
            var cmd = CommandBufferPool.Get(m_Node.passName);

            // 示例：清除颜色缓冲
            cmd.ClearRenderTarget(true, true, Color.black);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
```

### 4. 运行时图形

```csharp
using System.Collections.Generic;

namespace GraphToolkitTutorials.RenderGraphBasics.Runtime
{
    /// <summary>
    /// 运行时渲染图 - 存储运行时节点数据
    /// </summary>
    [System.Serializable]
    public class RenderGraphRuntime
    {
        public int StartNodeIndex = -1;
        private List<RenderRuntimeNode> m_Nodes = new List<RenderRuntimeNode>();

        public void AddNode(RenderRuntimeNode node)
        {
            m_Nodes.Add(node);
        }

        public RenderRuntimeNode GetNode(int index)
        {
            if (index >= 0 && index < m_Nodes.Count)
                return m_Nodes[index];
            return null;
        }

        public int NodeCount => m_Nodes.Count;
    }

    /// <summary>
    /// 运行时节点基类
    /// </summary>
    [System.Serializable]
    public abstract class RenderRuntimeNode
    {
        public int nextNodeIndex = -1;
    }

    /// <summary>
    /// 渲染Pass运行时节点
    /// </summary>
    [System.Serializable]
    public class RenderPassRuntimeNode : RenderRuntimeNode
    {
        public UnityEngine.Rendering.Universal.RenderPassEvent passEvent;
        public string passName;
    }
}
```

---

## 使用步骤

### 1. 创建渲染图资产

1. 在Project窗口右键 → Create → Render Graph
2. 命名为 `BasicRenderGraph.rendergraph`
3. 双击打开图形编辑器

### 2. 设计渲染流程

```
[Pipeline Start]
    ↓
[Render Pass: "Opaque Objects"]
    ↓
[Render Pass: "Transparent Objects"]
    ↓
[Pipeline End]
```

### 3. 配置URP Renderer

1. 打开URP Renderer资产（如 `UniversalRenderer.asset`）
2. 点击 "Add Renderer Feature"
3. 选择 "Graph Driven Renderer Feature"
4. 将渲染图资产拖入 "Graph Asset" 字段

### 4. 运行测试

进入Play模式，渲染图将自动执行。

---

## 调试技巧

### 1. Pass执行日志

```csharp
public override void Execute(ScriptingRenderContext context,
                             ref RenderingData renderingData)
{
    Debug.Log($"[{Time.frameCount}] Executing: {m_Node.passName}");
    // ... 渲染代码
}
```

### 2. Frame Debugger

使用Unity的Frame Debugger查看Pass执行顺序：
- Window → Analysis → Frame Debugger
- 点击 "Enable" 开始调试
- 查看每个Pass的渲染结果

### 3. 性能分析

```csharp
public override void Execute(ScriptingRenderContext context,
                             ref RenderingData renderingData)
{
    var cmd = CommandBufferPool.Get(m_Node.passName);
    
    // 添加性能标记
    cmd.BeginSample(m_Node.passName);
    
    // ... 渲染代码
    
    cmd.EndSample(m_Node.passName);
    
    context.ExecuteCommandBuffer(cmd);
    CommandBufferPool.Release(cmd);
}
```

---

## 示例：自定义清屏Pass

```csharp
/// <summary>
/// 清屏节点 - 清除颜色和深度缓冲
/// </summary>
[Node("Clear Pass", "Rendering")]
[UseWithGraph(typeof(RenderGraph))]
internal class ClearPassNode : RenderNode
{
    [SerializeField] private Color m_ClearColor = Color.black;
    [SerializeField] private bool m_ClearDepth = true;

    public override Runtime.RenderRuntimeNode CreateRuntimeNode(RenderGraph graph)
    {
        return new Runtime.ClearPassRuntimeNode
        {
            clearColor = m_ClearColor,
            clearDepth = m_ClearDepth,
            nextNodeIndex = GetNextNodeIndex(graph)
        };
    }
}

// 运行时节点
[System.Serializable]
public class ClearPassRuntimeNode : RenderRuntimeNode
{
    public Color clearColor;
    public bool clearDepth;
}

// Pass执行
public class ClearRenderPass : ScriptableRenderPass
{
    private ClearPassRuntimeNode m_Node;

    public override void Execute(ScriptingRenderContext context,
                                 ref RenderingData renderingData)
    {
        var cmd = CommandBufferPool.Get("Clear Pass");
        cmd.ClearRenderTarget(m_Node.clearDepth, true, m_Node.clearColor);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}
```

---

## 关键概念

### 1. 编辑器 vs 运行时分离

**为什么需要分离？**
- 编辑器节点包含Unity Editor API，无法在运行时使用
- 运行时节点需要序列化，必须是简单的数据结构
- 分离后可以独立优化编辑器和运行时性能

**转换流程**:
```
编辑器图形 (RenderGraph)
    ↓ CreateRuntime()
运行时图形 (RenderGraphRuntime)
    ↓ BuildPasses()
URP Pass列表 (List<ScriptableRenderPass>)
```

### 2. RenderPassEvent 的重要性

Pass的执行顺序由 `RenderPassEvent` 决定：
- 不透明物体必须在透明物体之前渲染
- 后处理必须在所有几何体渲染之后
- 阴影必须在使用阴影的物体之前渲染

### 3. CommandBuffer 的使用

所有渲染命令都通过 `CommandBuffer` 提交：
```csharp
var cmd = CommandBufferPool.Get("Pass Name");
cmd.DrawMesh(...);
cmd.Blit(...);
cmd.SetGlobalTexture(...);
context.ExecuteCommandBuffer(cmd);
CommandBufferPool.Release(cmd);
```

---

## 性能考虑

### 1. Pass数量

- 每个Pass都有开销（状态切换、GPU同步）
- 尽量合并相似的Pass
- 避免不必要的Pass

### 2. 资源管理

```csharp
// 使用RTHandle管理RenderTexture
private RTHandle m_TempRT;

public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
{
    var desc = renderingData.cameraData.cameraTargetDescriptor;
    RenderingUtils.ReAllocateIfNeeded(ref m_TempRT, desc, name: "_TempRT");
}

public override void OnCameraCleanup(CommandBuffer cmd)
{
    m_TempRT?.Release();
}
```

### 3. 条件执行

根据质量设置跳过某些Pass：
```csharp
public override void Execute(ScriptingRenderContext context,
                             ref RenderingData renderingData)
{
    if (QualitySettings.GetQualityLevel() < 2)
        return; // 低质量设置下跳过
    
    // ... 渲染代码
}
```

---

## 练习题

1. **基础**: 创建一个简单的渲染图，包含一个清屏Pass
2. **进阶**: 添加一个自定义Pass，在屏幕上绘制一个全屏四边形
3. **挑战**: 实现一个Blit Pass，将相机渲染结果复制到另一个RenderTexture

---

## 常见问题

**Q: 为什么我的Pass没有执行？**
A: 检查以下几点：
- RenderPassEvent 是否设置正确
- Pass是否被正确添加到队列（AddRenderPasses）
- 图形资产是否正确配置到RendererFeature

**Q: 如何访问相机的渲染结果？**
A: 使用 `renderingData.cameraData.renderer.cameraColorTarget`

**Q: 如何在Pass之间传递数据？**
A: 使用临时RenderTexture或全局Shader属性

---

## 下一步

本教程建立了渲染图的基础框架。在教程10中，我们将：
- 实现完整的Pass库（Opaque, Transparent, PostProcess等）
- 添加资源管理节点（RenderTexture, Material）
- 实现条件分支（Quality, Platform）
- 优化性能和添加调试工具

---

**下一步**: 继续学习 [教程10: 完整图形化渲染管线](10_GraphDrivenURP.md)
rPool.Get("Pass Name");
// 添加渲染命令...
context.ExecuteCommandBuffer(cmd);
CommandBufferPool.Release(cmd); // 必须释放！
```

---

## 练习题

1. **基础**: 创建一个简单的渲染图，包含一个清屏Pass和一个不透明物体Pass
2. **进阶**: 添加一个后处理Pass，实现简单的灰度效果
3. **挑战**: 实现一个条件Pass，根据质量设置决定是否执行某个Pass

---

## 常见问题

**Q: 为什么我的Pass没有执行？**
A: 检查以下几点：
- RendererFeature是否已添加到URP Renderer
- 图形资产是否已赋值
- 图形中是否有有效的节点连接

**Q: 如何访问相机的颜色缓冲？**
A: 使用 `renderingData.cameraData.renderer.cameraColorTarget`：
```csharp
var colorTarget = renderingData.cameraData.renderer.cameraColorTarget;
cmd.SetRenderTarget(colorTarget);
```

**Q: 如何在Pass之间传递RenderTexture？**
A: 创建共享的RenderTexture，在Pass的Configure方法中设置：
```csharp
private RenderTargetHandle m_TempTexture;

public override void Configure(CommandBuffer cmd, RenderTextureDescriptor desc)
{
    cmd.GetTemporaryRT(m_TempTexture.id, desc);
    ConfigureTarget(m_TempTexture.Identifier());
}

public override void FrameCleanup(CommandBuffer cmd)
{
    cmd.ReleaseTemporaryRT(m_TempTexture.id);
}
```

---

**下一步**: 继续学习 [教程10: 完整图形化渲染管线](10_GraphDrivenURP.md)
