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

        public object Value
        {
            get => m_Value;
            set
            {
                if (value is float floatValue)
                    m_Value = floatValue;
            }
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
