using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.VariablesSubgraphs
{
    /// <summary>
    /// 材质输出节点 — 收集所有 PBR 参数并生成 Material
    /// 由 MaterialGraph.OnEnable / OnGraphChanged 确保始终存在，不可被永久删除
    /// </summary>
    [Node("Material Output", "")]
    [UseWithGraph(typeof(MaterialGraph))]
    [Serializable]
    internal class MaterialOutputNode : Node
    {
        private IPort m_BaseColorInput;
        private IPort m_MetallicInput;
        private IPort m_SmoothnessInput;
        private IPort m_EmissionColorInput;
        private IPort m_EmissionIntensityInput;

        // 缓存求值结果
        private Color m_CachedBaseColor = Color.white;
        private float m_CachedMetallic = 0f;
        private float m_CachedSmoothness = 0.5f;
        private Color m_CachedEmissionColor = Color.black;
        private float m_CachedEmissionIntensity = 0f;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_BaseColorInput        = context.AddInputPort<Color>("Base Color").Build();
            m_MetallicInput         = context.AddInputPort<float>("Metallic").Build();
            m_SmoothnessInput       = context.AddInputPort<float>("Smoothness").Build();
            m_EmissionColorInput    = context.AddInputPort<Color>("Emission Color").Build();
            m_EmissionIntensityInput = context.AddInputPort<float>("Emission Intensity").WithDefaultValue(0f).Build();
        }

        /// <summary>
        /// 触发整图求值，将所有 PBR 参数缓存起来
        /// </summary>
        public void EvaluateAll(MaterialGraph graph)
        {
            // Base Color
            var conn = graph.GetConnectedOutputPort(m_BaseColorInput);
            m_CachedBaseColor = conn != null ? graph.EvaluateColorPort(conn) : Color.white;

            // Metallic
            conn = graph.GetConnectedOutputPort(m_MetallicInput);
            m_CachedMetallic = conn != null ? graph.EvaluateFloatPort(conn) : 0f;

            // Smoothness
            conn = graph.GetConnectedOutputPort(m_SmoothnessInput);
            m_CachedSmoothness = conn != null ? graph.EvaluateFloatPort(conn) : 0.5f;


            // Emission Color
            conn = graph.GetConnectedOutputPort(m_EmissionColorInput);
            m_CachedEmissionColor = conn != null ? graph.EvaluateColorPort(conn) : Color.black;

            // Emission Intensity
            conn = graph.GetConnectedOutputPort(m_EmissionIntensityInput);
            if (conn != null)
            {
                m_CachedEmissionIntensity = graph.EvaluateFloatPort(conn);
            }
            else
            {
                float intensity = 0f;
                m_EmissionIntensityInput?.TryGetValue(out intensity);
                m_CachedEmissionIntensity = intensity;
            }
        }

        public Color GetBaseColor()            => m_CachedBaseColor;
        public float GetMetallic()             => Mathf.Clamp01(m_CachedMetallic);
        public float GetSmoothness()           => Mathf.Clamp01(m_CachedSmoothness);
        public Color GetEmission()             => m_CachedEmissionColor;
        public float GetEmissionIntensity()    => Mathf.Max(0f, m_CachedEmissionIntensity);
    }
}
