using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.HelloGraph
{
    /// <summary>
    /// 计算器图形 - GraphToolkit入门示例
    /// 演示如何创建自定义图形类型
    /// </summary>
    [Graph("calc", GraphOptions.None)]
    internal class CalculatorGraph : Graph
    {
        /// <summary>
        /// 评估指定输出端口的值
        /// 这是数据流图形的核心方法，通过递归评估连接的节点来计算结果
        /// </summary>
        public float EvaluatePort(IPort port)
        {
            if (port == null || port.Direction != PortDirection.Output)
                return 0f;

            var node = port.Node;
            if (node is ICalculatorNode calcNode)
            {
                return calcNode.Evaluate(port, this);
            }

            return 0f;
        }

        /// <summary>
        /// 查找指定输出端口连接的输入端口
        /// </summary>
        public IPort GetConnectedInputPort(IPort outputPort)
        {
            if (outputPort == null || outputPort.Direction != PortDirection.Output)
                return null;

            foreach (var connection in Connections)
            {
                if (connection.OutputPort == outputPort)
                    return connection.InputPort;
            }

            return null;
        }

        /// <summary>
        /// 查找指定输入端口连接的输出端口
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
