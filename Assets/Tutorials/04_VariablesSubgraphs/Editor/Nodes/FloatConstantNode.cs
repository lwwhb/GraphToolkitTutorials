using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.VariablesSubgraphs
{
    /// <summary>
    /// 浮点常量节点
    /// </summary>
    [Node("Float", "Material")]
    [UseWithGraph(typeof(MaterialGraph))]
    internal class FloatConstantNode : Node, IConstantNode, IFloatNode
    {
        [SerializeField]
        private float m_Value = 0f;

        private IPort m_Output;

        public System.Type DataType => typeof(float);

        public bool TryGetValue<T>(out T value)
        {
            if (typeof(T) == typeof(float))
            {
                value = (T)(object)m_Value;
                return true;
            }
            value = default;
            return false;
        }

        public bool TrySetValue<T>(T value)
        {
            if (value is float floatValue)
            {
                m_Value = floatValue;
                return true;
            }
            return false;
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_Output = context.AddOutputPort<float>("Value").Build();
        }

        public float EvaluateFloat(IPort port, MaterialGraph graph)
        {
            return m_Value;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Value", () => m_Value, v => m_Value = v).Build();
        }
    }
}
