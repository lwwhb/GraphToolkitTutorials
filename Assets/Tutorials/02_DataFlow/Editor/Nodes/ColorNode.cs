using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.DataFlow
{
    /// <summary>
    /// 颜色常量节点
    /// 输出一个固定的颜色值
    /// </summary>
    [Node("Color", "Texture")]
    internal class ColorNode : Node, IColorNode
    {
        [SerializeField]
        private Color m_Color = Color.white;

        private IPort m_Output;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_Output = context.AddOutputPort<Color>("Color").Build();
        }

        public Color EvaluateColor(IPort port, TextureGraph graph)
        {
            return m_Color;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Color", () => m_Color, v => m_Color = v).Build();
        }
    }
}
