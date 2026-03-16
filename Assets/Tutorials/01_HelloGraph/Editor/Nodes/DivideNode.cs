using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.HelloGraph
{
    /// <summary>
    /// 除法节点 - 计算两个输入值的商
    /// 包含除零保护
    /// </summary>
    [Node("Divide", "Calculator")]
    internal class DivideNode : Node, ICalculatorNode
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

            // 除零保护
            if (Mathf.Approximately(b, 0f))
            {
                Debug.LogWarning($"Division by zero in node {Name}");
                return 0f;
            }

            return a / b;
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
