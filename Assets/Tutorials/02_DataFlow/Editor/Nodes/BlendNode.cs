using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.DataFlow
{
    /// <summary>
    /// 混合纹理节点
    /// 将两个纹理按指定比例混合
    /// </summary>
    [Node("Blend", "Texture")]
    internal class BlendNode : Node, ITextureNode
    {
        public enum BlendMode
        {
            Mix,
            Add,
            Multiply,
            Screen
        }

        [SerializeField]
        private BlendMode m_BlendMode = BlendMode.Mix;

        [SerializeField]
        private float m_BlendFactor = 0.5f;

        private IPort m_TextureAInput;
        private IPort m_TextureBInput;
        private IPort m_BlendFactorInput;
        private IPort m_TextureOutput;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_TextureAInput = context.AddInputPort<Texture2D>("Texture A").Build();
            m_TextureBInput = context.AddInputPort<Texture2D>("Texture B").Build();
            m_BlendFactorInput = context.AddInputPort<float>("Blend").Build();
            m_TextureOutput = context.AddOutputPort<Texture2D>("Result").Build();
        }

        public Texture2D EvaluateTexture(IPort port, TextureGraph graph)
        {
            if (port != m_TextureOutput)
                return null;

            // 获取输入纹理
            Texture2D textureA = null;
            Texture2D textureB = null;

            var connectedPortA = graph.GetConnectedOutputPort(m_TextureAInput);
            if (connectedPortA != null)
            {
                textureA = graph.EvaluateTexturePort(connectedPortA);
            }

            var connectedPortB = graph.GetConnectedOutputPort(m_TextureBInput);
            if (connectedPortB != null)
            {
                textureB = graph.EvaluateTexturePort(connectedPortB);
            }

            if (textureA == null || textureB == null)
            {
                Debug.LogWarning($"BlendNode: Missing input texture(s)");
                return textureA ?? textureB;
            }

            // 获取混合因子
            float blendFactor = m_BlendFactor;
            var connectedBlendPort = graph.GetConnectedOutputPort(m_BlendFactorInput);
            if (connectedBlendPort != null)
            {
                blendFactor = graph.EvaluateFloatPort(connectedBlendPort);
            }
            blendFactor = Mathf.Clamp01(blendFactor);

            // 确保纹理尺寸一致
            int width = Mathf.Min(textureA.width, textureB.width);
            int height = Mathf.Min(textureA.height, textureB.height);

            Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color colorA = textureA.GetPixel(x, y);
                    Color colorB = textureB.GetPixel(x, y);
                    Color blended = BlendColors(colorA, colorB, blendFactor);
                    result.SetPixel(x, y, blended);
                }
            }

            result.Apply();
            return result;
        }

        private Color BlendColors(Color a, Color b, float t)
        {
            switch (m_BlendMode)
            {
                case BlendMode.Mix:
                    return Color.Lerp(a, b, t);

                case BlendMode.Add:
                    return new Color(
                        Mathf.Clamp01(a.r + b.r * t),
                        Mathf.Clamp01(a.g + b.g * t),
                        Mathf.Clamp01(a.b + b.b * t),
                        Mathf.Clamp01(a.a + b.a * t)
                    );

                case BlendMode.Multiply:
                    return new Color(
                        Mathf.Lerp(a.r, a.r * b.r, t),
                        Mathf.Lerp(a.g, a.g * b.g, t),
                        Mathf.Lerp(a.b, a.b * b.b, t),
                        Mathf.Lerp(a.a, a.a * b.a, t)
                    );

                case BlendMode.Screen:
                    return new Color(
                        Mathf.Lerp(a.r, 1f - (1f - a.r) * (1f - b.r), t),
                        Mathf.Lerp(a.g, 1f - (1f - a.g) * (1f - b.g), t),
                        Mathf.Lerp(a.b, 1f - (1f - a.b) * (1f - b.b), t),
                        Mathf.Lerp(a.a, 1f - (1f - a.a) * (1f - b.a), t)
                    );

                default:
                    return a;
            }
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Blend Mode", () => m_BlendMode, v => m_BlendMode = v).Build();
            context.AddOption("Blend Factor", () => m_BlendFactor, v => m_BlendFactor = Mathf.Clamp01(v)).Build();
        }
    }
}
