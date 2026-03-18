using System;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace GraphToolkitTutorials.DataFlow
{
    /// <summary>
    /// 纹理图形 - 数据流处理示例
    /// 演示如何通过图形生成纹理资产
    /// </summary>
    [Graph("texgraph", GraphOptions.Default)]
    [Serializable]
    public class TextureGraph : Graph
    {
        [MenuItem("Assets/Create/Graph Toolkit/TextureGraph", false)]
        /// <summary>
        /// 创建纹理图资源
        /// </summary>
        static void CreateGraphAssetFile()
        {
            GraphDatabase.PromptInProjectBrowserToCreateNewAsset<TextureGraph>();
        }
        /// <summary>
        /// 评估指定端口的纹理
        /// 这是纹理生成图形的核心方法
        /// </summary>
        public Texture2D EvaluateTexturePort(IPort port)
        {
            if (port == null || port.Direction != PortDirection.Output)
                return null;

            var node = FindNodeForPort(port);
            if (node is ITextureNode textureNode)
            {
                return textureNode.EvaluateTexture(port, this);
            }
            else if (node is IConstantNode constantNode)
            {
                constantNode.TryGetValue(out Texture2D texture);
                return texture;
            }
            else if (node is IVariableNode variableNode)
            {
                variableNode.Variable.TryGetDefaultValue(out Texture2D texture);
                return texture;
            }
            else
                return null;
        }

        /// <summary>
        /// 评估指定端口的颜色值
        /// </summary>
        public Color EvaluateColorPort(IPort port)
        {
            if (port == null || port.Direction != PortDirection.Output)
                return Color.white;

            var node = FindNodeForPort(port);
            if (node is IColorNode colorNode)
            {
                return colorNode.EvaluateColor(port, this);
            }
            else if (node is IConstantNode constantNode)
            {
                constantNode.TryGetValue(out Color color);
                return color;
            }
            else if (node is IVariableNode variableNode)
            {
                variableNode.Variable.TryGetDefaultValue(out Color color);
                return color;
            }
            else
                return Color.white;
        }
        
        /// <summary>
        /// 评估指定端口的Vector2
        /// </summary>
        public Vector2 EvaluateVector2Port(IPort port)
        {
            if (port == null || port.Direction != PortDirection.Output)
                return Vector2.one;

            var node = FindNodeForPort(port);
            if (node is IVector2Node vectorNode)
            {
                return vectorNode.EvaluateVector2(port, this);
            }
            else if (node is IConstantNode constantNode)
            {
                constantNode.TryGetValue(out Vector2 value);
                return value;
            }
            else if (node is IVariableNode variableNode)
            {
                variableNode.Variable.TryGetDefaultValue(out Vector2 value);
                return value;
            }
            else
                return Vector2.one;
        }

        /// <summary>
        /// 评估指定端口的浮点值
        /// </summary>
        public float EvaluateFloatPort(IPort port)
        {
            if (port == null || port.Direction != PortDirection.Output)
                return 0f;

            var node = FindNodeForPort(port);
            if (node is IFloatNode floatNode)
            {
                return floatNode.EvaluateFloat(port, this);
            }
            else if (node is IConstantNode constantNode)
            {
                constantNode.TryGetValue(out float value);
                return value;
            }
            else if (node is IVariableNode variableNode)
            {
                variableNode.Variable.TryGetDefaultValue(out float value);
                return value;
            }
            else
                return 0f;
        }

        /// <summary>
        /// 获取连接到输入端口的输出端口
        /// </summary>
        public IPort GetConnectedOutputPort(IPort inputPort)
        {
            if (inputPort == null || inputPort.Direction != PortDirection.Input)
                return null;

            return inputPort.FirstConnectedPort;
        }

        private INode FindNodeForPort(IPort port)
        {
            if (port == null) return null;
            foreach (var node in GetNodes())
            {
                foreach (var p in node.GetInputPorts())
                    if (p == port) return node;
                foreach (var p in node.GetOutputPorts())
                    if (p == port) return node;
            }
            return null;
        }
    }
}
