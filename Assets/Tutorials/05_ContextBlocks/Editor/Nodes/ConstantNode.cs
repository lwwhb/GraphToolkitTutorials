using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.ContextBlocks
{
    /// <summary>
    /// 向量常量节点
    /// 这是一个普通节点（不是BlockNode），可以在任何地方使用
    /// </summary>
    [Node("Vector3", "Shader")]
    [UseWithGraph(typeof(ShaderFunctionGraph))]
    internal class Vector3Node : Node, IVectorNode
    {
        [SerializeField]
        private Vector3 m_Value = Vector3.zero;

        private IPort m_Output;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_Output = context.AddOutputPort<Vector3>("Value").Build();
        }

        public Vector3 EvaluateVector(IPort port, ShaderFunctionGraph graph)
        {
            return m_Value;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Value", () => m_Value, v => m_Value = v).Build();
        }
    }

    /// <summary>
    /// 浮点常量节点
    /// </summary>
    [Node("Float", "Shader")]
    [UseWithGraph(typeof(ShaderFunctionGraph))]
    internal class FloatNode : Node, IFloatNode
    {
        [SerializeField]
        private float m_Value = 0f;

        private IPort m_Output;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_Output = context.AddOutputPort<float>("Value").Build();
        }

        public float EvaluateFloat(IPort port, ShaderFunctionGraph graph)
        {
            return m_Value;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Value", () => m_Value, v => m_Value = v).Build();
        }
    }
}
