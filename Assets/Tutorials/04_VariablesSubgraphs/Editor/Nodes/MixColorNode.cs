using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.VariablesSubgraphs
{
    /// <summary>
    /// 颜色混合节点
    /// 混合两个颜色
    /// </summary>
    [Node("Mix Color", "Material")]
    [UseWithGraph(typeof(MaterialGraph))]
    internal class MixColorNode : Node, IColorNode
    {
        [SerializeField]
        private float m_Factor = 0.5f;

        private IPort m_ColorA;
        private IPort m_ColorB;
        private IPort m_Factor;
        private IPort m_Output;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_ColorA = context.AddInputPort<Color>("Color A").Build();
            m_ColorB = context.AddInputPort<Color>("Color B").Build();
            m_Factor = context.AddInputPort<float>("Factor").Build();
            m_Output = context.AddOutputPort<Color>("Result").Build();
        }

        public Color EvaluateColor(IPort port, MaterialGraph graph)
        {
            if (port != m_Output)
                return Color.white;

            // 评估输入
            Color colorA = EvaluateColorInput(m_ColorA, graph);
            Color colorB = EvaluateColorInput(m_ColorB, graph);
            float factor = EvaluateFloatInput(m_Factor, graph);

            factor = Mathf.Clamp01(factor);
            return Color.Lerp(colorA, colorB, factor);
        }

        private Color EvaluateColorInput(IPort inputPort, MaterialGraph graph)
        {
            var connectedPort = graph.GetConnectedOutputPort(inputPort);
            if (connectedPort != null)
            {
                return graph.EvaluateColorPort(connectedPort);
            }
            return Color.white;
        }

        private float EvaluateFloatInput(IPort inputPort, MaterialGraph graph)
        {
            var connectedPort = graph.GetConnectedOutputPort(inputPort);
            if (connectedPort != null)
            {
                return graph.EvaluateFloatPort(connectedPort);
            }
            return m_Factor;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Factor", () => m_Factor, v => m_Factor = Mathf.Clamp01(v)).Build();
        }
    }
}
