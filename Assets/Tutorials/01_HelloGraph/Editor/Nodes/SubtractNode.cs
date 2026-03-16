using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.HelloGraph
{
    /// <summary>
    /// 减法节点 - 计算两个输入值的差
    /// </summary>
    [Node("Subtract", "Calculator")]
    internal class SubtractNode : Node, ICalculatorNode
    {
        private IPort m_InputA;
        private IPort m_InputB;
        private IPort m_Output;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_InputA = context.AddInputPort<float>("A").Build();
            m_InputB = context.AddInputPort<float>("B").Build();
            m_Output = context.AddOutputPort<float>("Result").Build();
        }

        public float Evaluate(IPort port, CalculatorGraph graph)
        {
            if (port != m_Output)
                return 0f;

            float a = EvaluateInputPort(m_InputA, graph);
            float b = EvaluateInputPort(m_InputB, graph);

            return a - b;
        }

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
