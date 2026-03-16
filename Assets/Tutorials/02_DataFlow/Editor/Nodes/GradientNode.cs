using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.DataFlow
{
    /// <summary>
    /// 渐变纹理节点
    /// 生成从一种颜色到另一种颜色的渐变纹理
    /// </summary>
    [Node("Gradient", "Texture")]
    internal class GradientNode : Node, ITextureNode
    {
        public enum GradientDirection
        {
            Horizontal,
            Vertical,
            Diagonal
        }

        [SerializeField]
        private Color m_ColorA = Color.black;

        [SerializeField]
        private Color m_ColorB = Color.white;

        [SerializeField]
        private GradientDirection m_Direction = GradientDirection.Horizontal;

        [SerializeField]
        private int m_Width = 256;

        [SerializeField]
        private int m_Height = 256;

        private IPort m_ColorAInput;
        private IPort m_ColorBInput;
        private IPort m_TextureOutput;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_ColorAInput = context.AddInputPort<Color>("Color A").Build();
            m_ColorBInput = context.AddInputPort<Color>("Color B").Build();
            m_TextureOutput = context.AddOutputPort<Texture2D>("Texture").Build();
        }

        public Texture2D EvaluateTexture(IPort port, TextureGraph graph)
        {
            if (port != m_TextureOutput)
                return null;

            // 获取颜色
            Color colorA = m_ColorA;
            Color colorB = m_ColorB;

            var connectedPortA = graph.GetConnectedOutputPort(m_ColorAInput);
            if (connectedPortA != null)
            {
                colorA = graph.EvaluateColorPort(connectedPortA);
            }

            var connectedPortB = graph.GetConnectedOutputPort(m_ColorBInput);
            if (connectedPortB != null)
            {
                colorB = graph.EvaluateColorPort(connectedPortB);
            }

            // 创建纹理
            Texture2D texture = new Texture2D(m_Width, m_Height, TextureFormat.RGBA32, false);

            for (int y = 0; y < m_Height; y++)
            {
                for (int x = 0; x < m_Width; x++)
                {
                    float t = CalculateGradientValue(x, y);
                    Color color = Color.Lerp(colorA, colorB, t);
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return texture;
        }

        private float CalculateGradientValue(int x, int y)
        {
            switch (m_Direction)
            {
                case GradientDirection.Horizontal:
                    return (float)x / (m_Width - 1);

                case GradientDirection.Vertical:
                    return (float)y / (m_Height - 1);

                case GradientDirection.Diagonal:
                    return ((float)x / (m_Width - 1) + (float)y / (m_Height - 1)) * 0.5f;

                default:
                    return 0f;
            }
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Color A", () => m_ColorA, v => m_ColorA = v).Build();
            context.AddOption("Color B", () => m_ColorB, v => m_ColorB = v).Build();
            context.AddOption("Direction", () => m_Direction, v => m_Direction = v).Build();
            context.AddOption("Width", () => m_Width, v => m_Width = Mathf.Max(1, v)).Build();
            context.AddOption("Height", () => m_Height, v => m_Height = Mathf.Max(1, v)).Build();
        }
    }
}
