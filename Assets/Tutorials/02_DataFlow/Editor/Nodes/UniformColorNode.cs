using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.DataFlow
{
    /// <summary>
    /// 统一颜色纹理节点
    /// 生成一个纯色纹理
    /// </summary>
    [Node("Uniform Color", "Texture")]
    internal class UniformColorNode : Node, ITextureNode, IColorNode
    {
        [SerializeField]
        private Color m_Color = Color.white;

        [SerializeField]
        private int m_Width = 256;

        [SerializeField]
        private int m_Height = 256;

        private IPort m_ColorInput;
        private IPort m_TextureOutput;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_ColorInput = context.AddInputPort<Color>("Color").Build();
            m_TextureOutput = context.AddOutputPort<Texture2D>("Texture").Build();
        }

        public Texture2D EvaluateTexture(IPort port, TextureGraph graph)
        {
            if (port != m_TextureOutput)
                return null;

            // 获取颜色（从输入端口或使用默认值）
            Color color = m_Color;
            var connectedPort = graph.GetConnectedOutputPort(m_ColorInput);
            if (connectedPort != null)
            {
                color = graph.EvaluateColorPort(connectedPort);
            }

            // 创建纹理
            Texture2D texture = new Texture2D(m_Width, m_Height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[m_Width * m_Height];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }

        public Color EvaluateColor(IPort port, TextureGraph graph)
        {
            return m_Color;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Color", () => m_Color, v => m_Color = v).Build();
            context.AddOption("Width", () => m_Width, v => m_Width = Mathf.Max(1, v)).Build();
            context.AddOption("Height", () => m_Height, v => m_Height = Mathf.Max(1, v)).Build();
        }
    }
}
