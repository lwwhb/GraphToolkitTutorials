using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.ContextBlocks
{
    /// <summary>
    /// 操作块节点
    /// 在函数内部执行各种操作
    /// </summary>
    [Node("Add", "Shader/Block")]
    [UseWithGraph(typeof(ShaderFunctionGraph))]
    internal class AddBlockNode : BlockNode, IVectorNode
    {
        private IPort m_InputA;
        private IPort m_InputB;
        private IPort m_Output;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_InputA = context.AddInputPort<Vector3>("A").Build();
            m_InputB = context.AddInputPort<Vector3>("B").Build();
            m_Output = context.AddOutputPort<Vector3>("Result").Build();
        }

        public Vector3 EvaluateVector(IPort port, ShaderFunctionGraph graph)
        {
            if (port != m_Output)
                return Vector3.zero;

            Vector3 a = EvaluateInput(m_InputA, graph);
            Vector3 b = EvaluateInput(m_InputB, graph);

            return a + b;
        }

        private Vector3 EvaluateInput(IPort inputPort, ShaderFunctionGraph graph)
        {
            var connectedPort = graph.GetConnectedOutputPort(inputPort);
            if (connectedPort != null)
            {
                return graph.EvaluateVectorPort(connectedPort);
            }
            return Vector3.zero;
        }
    }

    /// <summary>
    /// 乘法块节点
    /// </summary>
    [Node("Multiply", "Shader/Block")]
    [UseWithGraph(typeof(ShaderFunctionGraph))]
    internal class MultiplyBlockNode : BlockNode, IVectorNode
    {
        private IPort m_InputA;
        private IPort m_InputB;
        private IPort m_Output;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_InputA = context.AddInputPort<Vector3>("A").Build();
            m_InputB = context.AddInputPort<Vector3>("B").Build();
            m_Output = context.AddOutputPort<Vector3>("Result").Build();
        }

        public Vector3 EvaluateVector(IPort port, ShaderFunctionGraph graph)
        {
            if (port != m_Output)
                return Vector3.zero;

            Vector3 a = EvaluateInput(m_InputA, graph);
            Vector3 b = EvaluateInput(m_InputB, graph);

            return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        }

        private Vector3 EvaluateInput(IPort inputPort, ShaderFunctionGraph graph)
        {
            var connectedPort = graph.GetConnectedOutputPort(inputPort);
            if (connectedPort != null)
            {
                return graph.EvaluateVectorPort(connectedPort);
            }
            return Vector3.one;
        }
    }

    /// <summary>
    /// 归一化块节点
    /// </summary>
    [Node("Normalize", "Shader/Block")]
    [UseWithGraph(typeof(ShaderFunctionGraph))]
    internal class NormalizeBlockNode : BlockNode, IVectorNode
    {
        private IPort m_Input;
        private IPort m_Output;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_Input = context.AddInputPort<Vector3>("Vector").Build();
            m_Output = context.AddOutputPort<Vector3>("Result").Build();
        }

        public Vector3 EvaluateVector(IPort port, ShaderFunctionGraph graph)
        {
            if (port != m_Output)
                return Vector3.zero;

            var connectedPort = graph.GetConnectedOutputPort(m_Input);
            if (connectedPort != null)
            {
                Vector3 input = graph.EvaluateVectorPort(connectedPort);
                return input.normalized;
            }

            return Vector3.zero;
        }
    }
}
