using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.VariablesSubgraphs
{
    /// <summary>
    /// 颜色常量节点
    /// </summary>
    [Node("Color", "Material")]
    [UseWithGraph(typeof(MaterialGraph))]
    internal class ColorConstantNode : Node, IConstantNode, IColorNode
    {
        [SerializeField]
        private Color m_Value = Color.white;

        private IPort m_Output;

        public System.Type DataType => typeof(Color);

        public bool TryGetValue<T>(out T value)
        {
            if (typeof(T) == typeof(Color))
            {
                value = (T)(object)m_Value;
                return true;
            }
            value = default;
            return false;
        }

        public bool TrySetValue<T>(T value)
        {
            if (value is Color color)
            {
                m_Value = color;
                return true;
            }
            return false;
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_Output = context.AddOutputPort<Color>("Color").Build();
        }

        public Color EvaluateColor(IPort port, MaterialGraph graph)
        {
            return m_Value;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Color", () => m_Value, v => m_Value = v).Build();
        }
    }
}
