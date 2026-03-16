using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.CustomUI
{
    /// <summary>
    /// 自定义图形 - 演示自定义编辑器UI
    /// 这个图形将展示如何扩展GraphToolkit的编辑器界面
    /// </summary>
    [Graph("customgraph", GraphOptions.None)]
    internal class CustomGraph : Graph
    {
        /// <summary>
        /// 评估浮点端口
        /// </summary>
        public float EvaluateFloatPort(IPort port)
        {
            if (port == null || port.Direction != PortDirection.Output)
                return 0f;

            var node = port.Node;
            if (node is IFloatNode floatNode)
            {
                return floatNode.EvaluateFloat(port, this);
            }

            return 0f;
        }

        /// <summary>
        /// 评估颜色端口
        /// </summary>
        public Color EvaluateColorPort(IPort port)
        {
            if (port == null || port.Direction != PortDirection.Output)
                return Color.white;

            var node = port.Node;
            if (node is IColorNode colorNode)
            {
                return colorNode.EvaluateColor(port, this);
            }

            return Color.white;
        }

        /// <summary>
        /// 获取连接到输入端口的输出端口
        /// </summary>
        public IPort GetConnectedOutputPort(IPort inputPort)
        {
            if (inputPort == null || inputPort.Direction != PortDirection.Input)
                return null;

            foreach (var connection in Connections)
            {
                if (connection.InputPort == inputPort)
                    return connection.OutputPort;
            }

            return null;
        }
    }
}
