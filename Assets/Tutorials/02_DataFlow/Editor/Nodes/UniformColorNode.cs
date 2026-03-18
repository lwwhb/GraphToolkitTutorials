using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.DataFlow
{
    /// <summary>
    /// 统一颜色纹理节点
    /// 生成一个纯色纹理
    /// </summary>
    [Node("Texture", "")]
    [Serializable]
    internal class UniformColorNode : Node, ITextureNode
    {
        private IPort m_ColorInput;
        private IPort m_ResolutionInput;
        private IPort m_TextureOutput;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_ColorInput = context.AddInputPort<Color>("Color").Build();
            m_ResolutionInput = context.AddInputPort<Vector2>("Resolution").WithDefaultValue(new Vector2(512, 512)).Build();
            m_TextureOutput = context.AddOutputPort<Texture2D>("Texture").Build();
        }

        public Texture2D EvaluateTexture(IPort port, TextureGraph graph)
        {
            if (port != m_TextureOutput)
                return null;

            // 获取颜色（从输入端口或使用默认值）
            Color color = Color.white;
            var connectedColorPort = graph.GetConnectedOutputPort(m_ColorInput);
            if (connectedColorPort != null)
            {
                color = graph.EvaluateColorPort(connectedColorPort);
            }

            // 获取尺寸
            Vector2 resolution = Vector2.one;
            var connectedResolutionPort = graph.GetConnectedOutputPort(m_ResolutionInput);
            if (connectedResolutionPort != null)
            {
                resolution = graph.EvaluateVector2Port(connectedResolutionPort);
            }
            else
            {
                m_ResolutionInput.TryGetValue(out Vector2 resolutionValue);
                resolution = resolutionValue;
            }
            
            // 创建纹理
            Texture2D texture = new Texture2D((int)resolution.x, (int)resolution.y, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[(int)resolution.x * (int)resolution.y];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
