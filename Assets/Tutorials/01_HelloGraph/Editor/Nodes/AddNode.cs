using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.HelloGraph
{
    /// <summary>
    /// 加法节点 - 计算两个输入值的和
    /// 演示如何从输入端口获取值并进行计算
    /// </summary>
    [Node("Add", "Calculator")]
    internal class AddNode : Node, ICalculatorNode
    {
        private IPort m_InputA;
        private IPort m_InputB;
        private IPort m_Output;

        /// <summary>
        /// 定义节点的端口
        /// 加法节点有两个输入端口和一个输出端口
        /// </summary>
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_InputA = context.AddInputPort<float>("A").Build();
            m_InputB = context.AddInputPort<float>("B").Build();
            m_Output = context.AddOutputPort<float>("Result").Build();
        }

        /// <summary>
        /// 评估端口值
        /// 递归评估输入端口的值，然后返回它们的和
        /// </summary>
        public float Evaluate(IPort port, CalculatorGraph graph)
        {
            if (port != m_Output)
                return 0f;

            float a = EvaluateInputPort(m_InputA, graph);
            float b = EvaluateInputPort(m_InputB, graph);

            return a + b;
        }

        /// <summary>
        /// 评估输入端口的值
        /// 如果端口有连接，则递归评估连接的输出端口
        /// 如果没有连接，返回默认值0
        /// </summary>
        private float EvaluateInputPort(IPort inputPort, CalculatorGraph graph)
        {
            var connectedPort = graph.GetConnectedOutputPort(inputPort);
            if (connectedPort != null)
            {
                return graph.EvaluatePort(connectedPort);
            }
            return 0f;
        }
    }
}
