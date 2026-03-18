using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.DataFlow
{
    /// <summary>
    /// 颜色常量节点
    /// 输出一个固定的颜色值
    /// </summary>
    [Node("Texture", "")]
    [Serializable]
    internal class ColorNode : Node, IColorNode
    {
        private INodeOption m_Color;
        private IPort m_Output;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_Output = context.AddOutputPort<Color>("Color").Build();
        }

        public Color EvaluateColor(IPort port, TextureGraph graph)
        {
            if (m_Color != null && m_Color.TryGetValue(out Color value))
            {
                return value;
            }
            return Color.white;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_Color = context.AddOption<Color>("Color").Build();
        }
    }
}
