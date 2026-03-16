using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.ContextBlocks
{
    /// <summary>
    /// 着色器函数图形 - 演示ContextNode和BlockNode
    /// 类似于Shader Graph中的自定义函数节点
    /// </summary>
    [Graph("shaderfunc", GraphOptions.None)]
    internal class ShaderFunctionGraph : Graph
    {
        /// <summary>
        /// 评估浮点端口
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
        /// 评估向量端口
        /// </summary>
        public Vector3 EvaluateVectorPort(IPort port)
        {
            if (port == null || port.Direction != PortDirection.Output)
                return Vector3.zero;

            var node = FindNodeForPort(port);
            if (node is IVectorNode vectorNode)
            {
                return vectorNode.EvaluateVector(port, this);
            }

            return Vector3.zero;
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

        /// <summary>
        /// 查找函数上下文节点
        /// </summary>
        public FunctionContextNode FindFunctionContext()
        {
            foreach (var node in GetNodes())
            {
                if (node is FunctionContextNode contextNode)
                {
                    return contextNode;
                }
            }
            return null;
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
