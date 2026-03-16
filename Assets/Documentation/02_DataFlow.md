# 教程2: 数据流图形 - 纹理生成器

## 概述

本教程将带你创建一个纹理生成系统，通过图形化的方式生成程序化纹理。这是一个典型的数据流图形应用，展示了如何处理复杂的数据类型和递归评估。

## 学习目标

- 掌握数据流图形的评估模式
- 理解递归端口评估机制
- 学会处理多种数据类型（Texture2D、Color、float）
- 实现纹理处理节点
- 生成实际的Unity资产

## 核心概念

### 数据流图形 vs 执行流图形

**数据流图形**:
- 数据从输入端口流向输出端口
- 节点通过递归评估获取输入数据
- 评估是按需进行的（pull模式）
- 适合：数据处理、材质生成、数学计算

**执行流图形**（下一个教程）:
- 有明确的执行顺序
- 节点按顺序执行
- 执行是主动推进的（push模式）
- 适合：行为树、对话系统、流程控制

### 多类型接口设计

本教程使用多个接口来支持不同的数据类型：

```csharp
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
```

这种设计允许：
- 节点可以实现多个接口（如UniformColorNode同时实现ITextureNode和IColorNode）
- 图形可以根据端口类型调用相应的评估方法
- 类型安全的端口连接

### 递归评估模式

纹理生成使用递归评估：

```csharp
public Texture2D EvaluateTexture(IPort port, TextureGraph graph)
{
    // 1. 获取输入端口连接的输出端口
    var connectedPort = graph.GetConnectedOutputPort(m_TextureInput);

    // 2. 递归评估连接的端口
    if (connectedPort != null)
    {
        return graph.EvaluateTexturePort(connectedPort);
    }

    // 3. 没有连接则使用默认值
    return null;
}
```

## 项目结构

```
02_DataFlow/
├─ Editor/
│  ├─ TextureGraph.cs                 # 纹理图形定义
│  ├─ TextureGraphImporter.cs         # 资产导入器
│  ├─ Nodes/
│  │  ├─ ITextureNode.cs              # 节点接口
│  │  ├─ UniformColorNode.cs          # 纯色纹理
│  │  ├─ GradientNode.cs              # 渐变纹理
│  │  ├─ NoiseNode.cs                 # 噪声纹理
│  │  ├─ BlendNode.cs                 # 混合节点
│  │  ├─ ColorNode.cs                 # 颜色常量
│  │  ├─ FloatNode.cs                 # 浮点常量
│  │  └─ OutputNode.cs                # 输出节点
│  └─ Unity.GraphToolkit.Tutorials.DataFlow.Editor.asmdef
└─ Examples/
   └─ (将在Unity编辑器中创建.texgraph文件)
```

## 节点详解

### UniformColorNode（纯色纹理节点）

生成一个纯色纹理，同时实现ITextureNode和IColorNode：

```csharp
[Node("Uniform Color", "Texture")]
internal class UniformColorNode : Node, ITextureNode, IColorNode
{
    [SerializeField]
    private Color m_Color = Color.white;

    [SerializeField]
    private int m_Width = 256;

    [SerializeField]
    private int m_Height = 256;

    public Texture2D EvaluateTexture(IPort port, TextureGraph graph)
    {
        // 获取颜色（可以从输入端口或使用默认值）
        Color color = m_Color;
        var connectedPort = graph.GetConnectedOutputPort(m_ColorInput);
        if (connectedPort != null)
        {
            color = graph.EvaluateColorPort(connectedPort);
        }

        // 创建纹理
        Texture2D texture = new Texture2D(m_Width, m_Height);
        // 填充颜色...
        return texture;
    }
}
```

**特点**:
- 可以直接设置颜色，也可以从Color端口接收
- 可配置纹理尺寸
- 同时作为纹理源和颜色源

### GradientNode（渐变纹理节点）

生成从一种颜色到另一种颜色的渐变：

```csharp
public enum GradientDirection
{
    Horizontal,
    Vertical,
    Diagonal
}

public Texture2D EvaluateTexture(IPort port, TextureGraph graph)
{
    // 获取两个颜色
    Color colorA = EvaluateColorInput(m_ColorAInput, graph);
    Color colorB = EvaluateColorInput(m_ColorBInput, graph);

    // 生成渐变
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            float t = CalculateGradientValue(x, y);
            Color color = Color.Lerp(colorA, colorB, t);
            texture.SetPixel(x, y, color);
        }
    }
}
```

**特点**:
- 支持水平、垂直、对角线渐变
- 颜色可以从输入端口获取
- 逐像素计算渐变值

### BlendNode（混合节点）

将两个纹理按指定方式混合：

```csharp
public enum BlendMode
{
    Mix,        // 线性插值
    Add,        // 相加
    Multiply,   // 相乘
    Screen      // 滤色
}

public Texture2D EvaluateTexture(IPort port, TextureGraph graph)
{
    // 评估两个输入纹理
    Texture2D textureA = graph.EvaluateTexturePort(connectedPortA);
    Texture2D textureB = graph.EvaluateTexturePort(connectedPortB);

    // 获取混合因子
    float blendFactor = EvaluateFloatInput(m_BlendFactorInput, graph);

    // 逐像素混合
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            Color colorA = textureA.GetPixel(x, y);
            Color colorB = textureB.GetPixel(x, y);
            Color blended = BlendColors(colorA, colorB, blendFactor);
            result.SetPixel(x, y, blended);
        }
    }
}
```

**特点**:
- 支持多种混合模式
- 混合因子可以从输入端口获取
- 自动处理尺寸不一致的纹理

### NoiseNode（噪声纹理节点）

生成Perlin噪声纹理：

```csharp
public Texture2D EvaluateTexture(IPort port, TextureGraph graph)
{
    for (int y = 0; y < m_Height; y++)
    {
        for (int x = 0; x < m_Width; x++)
        {
            float nx = (float)x / m_Width * m_Scale + m_OffsetX;
            float ny = (float)y / m_Height * m_Scale + m_OffsetY;
            float value = Mathf.PerlinNoise(nx, ny);
            texture.SetPixel(x, y, new Color(value, value, value, 1f));
        }
    }
}
```

**特点**:
- 使用Unity的Mathf.PerlinNoise
- 可调节缩放和偏移
- 生成灰度噪声

## 实践步骤

### 示例1: 简单渐变纹理

创建一个从红色到蓝色的水平渐变：

1. 创建.texgraph文件
2. 添加节点：
   - 2个Color节点（红色和蓝色）
   - 1个Gradient节点
   - 1个Output节点
3. 连接：
   - Color(红) → Gradient的Color A
   - Color(蓝) → Gradient的Color B
   - Gradient → Output
4. 配置Gradient节点：Direction = Horizontal
5. 保存并查看生成的纹理

### 示例2: 混合纹理

创建渐变和噪声的混合：

```
[Gradient] ─┐
            ├─> [Blend] ─> [Output]
[Noise] ────┘
```

**步骤**:
1. 添加Gradient节点和Noise节点
2. 添加Blend节点
3. 连接两个纹理到Blend的输入
4. 添加Float节点，设置为0.5，连接到Blend Factor
5. 尝试不同的混合模式

### 示例3: 复杂纹理

创建一个更复杂的纹理：

```
[Color: Red] ─┐
              ├─> [Gradient] ─┐
[Color: Yellow]─┘              │
                               ├─> [Blend: Multiply] ─> [Output]
[Noise] ───────────────────────┘
```

这会创建一个带有噪声的红黄渐变纹理。

## 性能优化

### 1. 缓存机制

当前实现每次导入都重新评估整个图形。可以添加缓存：

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
```

### 2. 延迟评估

只评估连接到Output节点的部分：

```csharp
// 从Output节点开始，只评估需要的节点
public void EvaluateFromOutput()
{
    foreach (var node in Nodes)
    {
        if (node is OutputNode output)
        {
            output.EvaluateTexture(null, this);
            break;
        }
    }
}
```

### 3. 并行处理

对于大纹理，可以使用Job System并行处理像素：

```csharp
// 使用Unity的Job System
var job = new GenerateTextureJob
{
    width = m_Width,
    height = m_Height,
    pixels = new NativeArray<Color32>(m_Width * m_Height, Allocator.TempJob)
};
job.Schedule(m_Width * m_Height, 64).Complete();
```

## 练习题

### 练习1: 添加新的混合模式

在BlendNode中添加"Overlay"混合模式：

```csharp
case BlendMode.Overlay:
    // Overlay公式: a < 0.5 ? 2*a*b : 1 - 2*(1-a)*(1-b)
    float r = a.r < 0.5f ? 2f * a.r * b.r : 1f - 2f * (1f - a.r) * (1f - b.r);
    // 对g和b通道做同样处理
```

### 练习2: 实现CheckerboardNode

创建一个棋盘格纹理节点：

```csharp
[Node("Checkerboard", "Texture")]
internal class CheckerboardNode : Node, ITextureNode
{
    [SerializeField]
    private int m_TileSize = 32;

    [SerializeField]
    private Color m_ColorA = Color.white;

    [SerializeField]
    private Color m_ColorB = Color.black;

    // 实现评估逻辑
}
```

### 练习3: 实现InvertNode

创建一个反转纹理颜色的节点：

```csharp
[Node("Invert", "Texture")]
internal class InvertNode : Node, ITextureNode
{
    public Texture2D EvaluateTexture(IPort port, TextureGraph graph)
    {
        // 获取输入纹理
        // 反转每个像素的颜色: newColor = Color.white - oldColor
        // 返回新纹理
    }
}
```

## 扩展方向

### 1. 添加更多纹理操作

- **模糊节点**: 实现高斯模糊
- **锐化节点**: 增强边缘
- **色调调整**: HSV调整
- **通道分离/合并**: 操作RGBA通道

### 2. 支持纹理导入

允许从外部导入纹理作为输入：

```csharp
[Node("Texture Input", "Texture")]
internal class TextureInputNode : Node, ITextureNode
{
    [SerializeField]
    private Texture2D m_InputTexture;

    public Texture2D EvaluateTexture(IPort port, TextureGraph graph)
    {
        return m_InputTexture;
    }
}
```

### 3. 实时预览

在图形编辑器中显示节点的纹理预览：

```csharp
// 在自定义NodeView中
public override void OnInspectorGUI()
{
    base.OnInspectorGUI();

    if (node is ITextureNode textureNode)
    {
        var texture = textureNode.EvaluateTexture(/* ... */);
        if (texture != null)
        {
            GUILayout.Label(texture, GUILayout.Width(128), GUILayout.Height(128));
        }
    }
}
```

## 关键要点

1. **数据流评估**: 通过递归评估端口获取数据
2. **多类型支持**: 使用接口支持不同的数据类型
3. **按需计算**: 只评估需要的节点（从Output开始）
4. **类型安全**: 端口类型确保正确的连接
5. **资产生成**: 通过ScriptedImporter生成实际的Unity资产

## 下一步

在下一个教程中，我们将学习执行流图形，创建一个任务执行系统，理解与数据流图形的区别。

## 常见问题

**Q: 为什么需要多个接口（ITextureNode、IColorNode等）？**

A: 因为节点可能输出不同类型的数据。例如UniformColorNode既可以作为纹理源，也可以作为颜色源。多接口设计提供了灵活性。

**Q: 如何避免重复评估同一个节点？**

A: 实现缓存机制。在图形中维护一个Dictionary，存储已评估的端口结果。

**Q: 生成的纹理可以在运行时使用吗？**

A: 可以。导入器生成的纹理是标准的Unity Texture2D资产，可以在运行时使用。

**Q: 如何处理循环依赖？**

A: 添加访问标记，在评估时检测循环：

```csharp
private HashSet<IPort> m_EvaluatingPorts = new HashSet<IPort>();

public Texture2D EvaluateTexturePort(IPort port)
{
    if (m_EvaluatingPorts.Contains(port))
    {
        Debug.LogError("Circular dependency detected!");
        return null;
    }

    m_EvaluatingPorts.Add(port);
    var result = /* 评估 */;
    m_EvaluatingPorts.Remove(port);
    return result;
}
```

**Q: 大纹理生成很慢怎么办？**

A: 考虑：
1. 使用Job System并行处理
2. 降低预览分辨率
3. 添加进度条显示
4. 实现增量更新
