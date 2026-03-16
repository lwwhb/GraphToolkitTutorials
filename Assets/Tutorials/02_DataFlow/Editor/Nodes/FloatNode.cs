using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.DataFlow
{
    /// <summary>
    /// 浮点常量节点
    /// 输出一个固定的浮点值
    /// </summary>
    [Node("Float", "Texture")]
    internal class FloatNode : Node, IFloatNode
    {
        [SerializeField]
        private float m_Value = 0.5f;

        private IPort m_Output;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_Output = context.AddOutputPort<float>("Value").Build();
        }

        public float EvaluateFloat(IPort port, TextureGraph graph)
        {
            return m_Value;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Value", () => m_Value, v => m_Value = v).Build();
        }
    }
}
