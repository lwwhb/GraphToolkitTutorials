using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.VariablesSubgraphs
{
    /// <summary>
    /// 材质输出节点
    /// 定义材质的最终属性
    /// </summary>
    [Node("Material Output", "Material")]
    [UseWithGraph(typeof(MaterialGraph))]
    internal class MaterialOutputNode : Node, IColorNode
    {
        [SerializeField]
        private float m_Metallic = 0f;

        [SerializeField]
        private float m_Smoothness = 0.5f;

        private IPort m_BaseColorInput;
        private IPort m_MetallicInput;
        private IPort m_SmoothnessInput;

        private Color m_CachedBaseColor = Color.white;
        private float m_CachedMetallic = 0f;
        private float m_CachedSmoothness = 0.5f;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_BaseColorInput = context.AddInputPort<Color>("Base Color").Build();
            m_MetallicInput = context.AddInputPort<float>("Metallic").Build();
            m_SmoothnessInput = context.AddInputPort<float>("Smoothness").Build();
        }

        public Color EvaluateColor(IPort port, MaterialGraph graph)
        {
            // 评估基础颜色
            var connectedPort = graph.GetConnectedOutputPort(m_BaseColorInput);
            if (connectedPort != null)
            {
                m_CachedBaseColor = graph.EvaluateColorPort(connectedPort);
            }
            else
            {
                m_CachedBaseColor = Color.white;
            }

            // 评估金属度
            connectedPort = graph.GetConnectedOutputPort(m_MetallicInput);
            if (connectedPort != null)
            {
                m_CachedMetallic = graph.EvaluateFloatPort(connectedPort);
            }
            else
            {
                m_CachedMetallic = m_Metallic;
            }

            // 评估光滑度
            connectedPort = graph.GetConnectedOutputPort(m_SmoothnessInput);
            if (connectedPort != null)
            {
                m_CachedSmoothness = graph.EvaluateFloatPort(connectedPort);
            }
            else
            {
                m_CachedSmoothness = m_Smoothness;
            }

            return m_CachedBaseColor;
        }

        public float GetMetallic()
        {
            return Mathf.Clamp01(m_CachedMetallic);
        }

        public float GetSmoothness()
        {
            return Mathf.Clamp01(m_CachedSmoothness);
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Metallic", () => m_Metallic, v => m_Metallic = Mathf.Clamp01(v)).Build();
            context.AddOption("Smoothness", () => m_Smoothness, v => m_Smoothness = Mathf.Clamp01(v)).Build();
        }
    }
}
