using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.DataFlow
{
    /// <summary>
    /// 纹理节点接口
    /// 所有生成纹理的节点都需要实现此接口
    /// </summary>
    internal interface ITextureNode
    {
        /// <summary>
        /// 评估指定端口的纹理
        /// </summary>
        Texture2D EvaluateTexture(IPort port, TextureGraph graph);
    }

    /// <summary>
    /// 颜色节点接口
    /// 所有生成颜色的节点都需要实现此接口
    /// </summary>
    internal interface IColorNode
    {
        /// <summary>
        /// 评估指定端口的颜色
        /// </summary>
        Color EvaluateColor(IPort port, TextureGraph graph);
    }
    
    /// <summary>
    /// 颜色节点接口
    /// 所有生成颜色的节点都需要实现此接口
    /// </summary>
    internal interface IVector2Node
    {
        /// <summary>
        /// 评估指定端口的颜色
        /// </summary>
        Vector2 EvaluateVector2(IPort port, TextureGraph graph);
    }

    /// <summary>
    /// 浮点节点接口
    /// 所有生成浮点值的节点都需要实现此接口
    /// </summary>
    internal interface IFloatNode
    {
        /// <summary>
        /// 评估指定端口的浮点值
        /// </summary>
        float EvaluateFloat(IPort port, TextureGraph graph);
    }
}
