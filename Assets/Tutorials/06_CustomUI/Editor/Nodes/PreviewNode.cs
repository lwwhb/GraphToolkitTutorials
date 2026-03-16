using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.CustomUI
{
    /// <summary>
    /// 预览节点 - 演示在节点上显示预览
    /// 展示如何在节点内嵌入自定义UI元素
    /// </summary>
    [Node("Preview Node", "Custom", Color = "#4ECDC4")]
    [UseWithGraph(typeof(CustomGraph))]
    internal class PreviewNode : Node, IColorNode
    {
        [SerializeField]
        private Color m_Color = Color.white;

        [SerializeField]
        private bool m_ShowPreview = true;

        private IPort m_ColorInput;
        private IPort m_Output;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_ColorInput = context.AddInputPort<Color>("Color").Build();
            m_Output = context.AddOutputPort<Color>("Output").Build();
        }

        public Color EvaluateColor(IPort port, CustomGraph graph)
        {
            if (port != m_Output)
                return Color.white;

            // 评估输入颜色
            var connectedPort = graph.GetConnectedOutputPort(m_ColorInput);
            if (connectedPort != null)
            {
                m_Color = graph.EvaluateColorPort(connectedPort);
            }

            return m_Color;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Color", () => m_Color, v => m_Color = v).Build();
            context.AddOption("Show Preview", () => m_ShowPreview, v => m_ShowPreview = v).Build();
        }

        /// <summary>
        /// 获取预览颜色（用于自定义UI）
        /// </summary>
        public Color GetPreviewColor()
        {
            return m_Color;
        }

        /// <summary>
        /// 是否显示预览
        /// </summary>
        public bool ShouldShowPreview()
        {
            return m_ShowPreview;
        }
    }
}
