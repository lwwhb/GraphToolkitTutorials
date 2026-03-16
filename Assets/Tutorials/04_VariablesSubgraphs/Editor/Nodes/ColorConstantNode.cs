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

        public object Value
        {
            get => m_Value;
            set
            {
                if (value is Color color)
                    m_Value = color;
            }
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
