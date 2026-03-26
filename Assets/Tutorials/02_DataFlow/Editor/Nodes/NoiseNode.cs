using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.DataFlow
{
    /// <summary>
    /// 噪声纹理节点
    /// 生成程序化噪声纹理（Perlin噪声）
    /// </summary>
    [Node("Texture", "")]
    [Serializable]
    internal class NoiseNode : Node, ITextureNode
    {
        private IPort m_ResolutionInput;
        private IPort m_OffsetInput;
        private IPort m_ScaleInput;
        private IPort m_TextureOutput;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_ResolutionInput = context.AddInputPort<Vector2>("Resolution").WithDefaultValue(new Vector2(512, 512)).Build();
            m_OffsetInput = context.AddInputPort<Vector2>("Offset").WithDefaultValue(new Vector2(0.2f, 0.2f)).Build();
            m_ScaleInput = context.AddInputPort<int>("Scale").WithDefaultValue(100).Build();
            m_TextureOutput = context.AddOutputPort<Texture2D>("Texture").Build();
        }

        public Texture2D EvaluateTexture(IPort port, TextureGraph graph)
        {
            if (port != m_TextureOutput)
                return null;
            
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

            // 获取位移
            Vector2 offset = Vector2.one;
            var connectedOffsetPort = graph.GetConnectedOutputPort(m_OffsetInput);
            if (connectedOffsetPort != null)
            {
                offset = graph.EvaluateVector2Port(connectedOffsetPort);
            }
            else
            {
                m_OffsetInput.TryGetValue(out Vector2 offsetValue);
                offset = offsetValue;
            }
            
            // 获取缩放
            float scale = 1.0f;
            var connectedScalePort = graph.GetConnectedOutputPort(m_ScaleInput);
            if (connectedScalePort != null)
            {
                scale = graph.EvaluateFloatPort(connectedScalePort);
            }
            else
            {
                m_ScaleInput.TryGetValue(out int scaleValue);
                scale = scaleValue;
            }
            
            // 创建纹理
            Texture2D texture = new Texture2D((int)resolution.x, (int)resolution.y, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[(int)resolution.x * (int)resolution.y];
            for (int y = 0; y < (int)resolution.y; y++)
            {
                for (int x = 0; x < (int)resolution.x; x++)
                {
                    float nx = (float)x / (int)resolution.x * scale + offset.x;
                    float ny = (float)y / (int)resolution.y * scale + offset.y;
                    float value = Mathf.PerlinNoise(nx, ny);
                    texture.SetPixel(x, y, new Color(value, value, value, 1f));
                }
            }
            texture.Apply(); 
            return texture;
        }
    }
}
