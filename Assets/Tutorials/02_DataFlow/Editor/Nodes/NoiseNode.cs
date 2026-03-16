using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.DataFlow
{
    /// <summary>
    /// 噪声纹理节点
    /// 生成程序化噪声纹理（Perlin噪声）
    /// </summary>
    [Node("Noise", "Texture")]
    internal class NoiseNode : Node, ITextureNode
    {
        [SerializeField]
        private float m_Scale = 10f;

        [SerializeField]
        private float m_OffsetX = 0f;

        [SerializeField]
        private float m_OffsetY = 0f;

        [SerializeField]
        private int m_Width = 256;

        [SerializeField]
        private int m_Height = 256;

        private IPort m_TextureOutput;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_TextureOutput = context.AddOutputPort<Texture2D>("Texture").Build();
        }

        public Texture2D EvaluateTexture(IPort port, TextureGraph graph)
        {
            if (port != m_TextureOutput)
                return null;

            Texture2D texture = new Texture2D(m_Width, m_Height, TextureFormat.RGBA32, false);

            for (int y = 0; y < m_Height; y++)
            {
                for (int x = 0; x < m_Width; x++)
                {
                    float nx = (float)x / m_Width * m_Scale + m_OffsetX;
                    float ny = (float)y / m_Height * m_Scale + m_OffsetY;
                    float value = Mathf.PerlinNoise(nx, ny);
                    texture.SetPixel(x, y, new Color(value, value, value, 1f));
                }
            }

            texture.Apply();
            return texture;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Scale", () => m_Scale, v => m_Scale = Mathf.Max(0.01f, v)).Build();
            context.AddOption("Offset X", () => m_OffsetX, v => m_OffsetX = v).Build();
            context.AddOption("Offset Y", () => m_OffsetY, v => m_OffsetY = v).Build();
            context.AddOption("Width", () => m_Width, v => m_Width = Mathf.Max(1, v)).Build();
            context.AddOption("Height", () => m_Height, v => m_Height = Mathf.Max(1, v)).Build();
        }
    }
}
