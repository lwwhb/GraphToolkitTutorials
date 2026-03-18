using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.DataFlow
{
    /// <summary>
    /// 输出节点
    /// 标记纹理图形的最终输出
    /// </summary>
    [Node("Texture", "")]
    [Serializable]
    internal class OutputNode : Node, ITextureNode
    {
        private IPort m_TextureInput;

        [SerializeField]
        private Texture2D m_CachedTexture;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_TextureInput = context.AddInputPort<Texture2D>("Texture").Build();
        }

        public Texture2D EvaluateTexture(IPort port, TextureGraph graph)
        {
            var connectedPort = graph.GetConnectedOutputPort(m_TextureInput);
            if (connectedPort != null)
            {
                m_CachedTexture = graph.EvaluateTexturePort(connectedPort);
            }
            else
            {
                m_CachedTexture = null;
            }

            return m_CachedTexture;
        }
    }
}
