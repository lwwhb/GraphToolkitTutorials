using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.CustomUI
{
    /// <summary>
    /// 样式化节点 - 演示自定义节点外观
    /// 这个节点展示如何通过NodeAttribute自定义节点的显示
    /// </summary>
    [Node("Styled Node", "Custom")]
    [UseWithGraph(typeof(CustomGraph))]
    internal class StyledNode : Node, IFloatNode
    {
        [SerializeField]
        private float m_Value = 1f;

        [SerializeField]
        private string m_Label = "Styled";

        private IPort m_Input;
        private IPort m_Output;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_Input = context.AddInputPort<float>("Input")
                .WithCapacity(PortCapacity.Multiple) // 允许多个连接
                .Build();

            m_Output = context.AddOutputPort<float>("Output")
                .Build();
        }

        public float EvaluateFloat(IPort port, CustomGraph graph)
        {
            if (port != m_Output)
                return 0f;

            // 评估输入
            float result = m_Value;
            var connectedPort = graph.GetConnectedOutputPort(m_Input);
            if (connectedPort != null)
            {
                result += graph.EvaluateFloatPort(connectedPort);
            }

            return result;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Value", () => m_Value, v => m_Value = v).Build();

            context.AddOption("Label", () => m_Label, v => m_Label = v)
                .Delayed() // 延迟更新，适合文本输入
                .Build();
        }
    }
}
