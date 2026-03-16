using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.DataFlow
{
    /// <summary>
    /// 纹理图形 - 数据流处理示例
    /// 演示如何通过图形生成纹理资产
    /// </summary>
    [Graph("texgraph", GraphOptions.None)]
    internal class TextureGraph : Graph
    {
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

            return Color.white;
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
