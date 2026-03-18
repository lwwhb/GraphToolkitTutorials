using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.DataFlow
{
    /// <summary>
    /// 渐变纹理节点
    /// 生成从一种颜色到另一种颜色的渐变纹理
    /// </summary>
    [Node("Texture", "")]
    [Serializable]
    internal class GradientNode : Node, ITextureNode
    {
        public enum GradientDirection
        {
            Horizontal,
            Vertical,
            Diagonal
        }
        
        private INodeOption m_Direction;

        private IPort m_ColorAInput;
        private IPort m_ColorBInput;
        private IPort m_ResolutionInput;
        private IPort m_TextureOutput;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_ColorAInput = context.AddInputPort<Color>("Color A").Build();
            m_ColorBInput = context.AddInputPort<Color>("Color B").Build();
            m_ResolutionInput = context.AddInputPort<Vector2>("Resolution").WithDefaultValue(new Vector2(512, 512)).Build();
            m_TextureOutput = context.AddOutputPort<Texture2D>("Texture").Build();
        }

        public Texture2D EvaluateTexture(IPort port, TextureGraph graph)
        {
            if (port != m_TextureOutput)
                return null;
            
            // 获取方向和尺寸
            GradientDirection direction = GradientDirection.Horizontal;
            if (m_Direction != null && m_Direction.TryGetValue(out GradientDirection dirValue))
                direction = dirValue;
            
            // 获取颜色（从输入端口或使用默认值）
            Color colorA = Color.white;
            var connectedPortA = graph.GetConnectedOutputPort(m_ColorAInput);
            if (connectedPortA != null)
            {
                colorA = graph.EvaluateColorPort(connectedPortA);
            }
            Color colorB = Color.white;
            var connectedPortB = graph.GetConnectedOutputPort(m_ColorBInput);
            if (connectedPortB != null)
            {
                colorB = graph.EvaluateColorPort(connectedPortB);
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
            for (int y = 0; y < (int)resolution.y; y++)
            {
                for (int x = 0; x < (int)resolution.x; x++)
                {
                    float t = CalculateGradientValue(x, y, (int)resolution.x, (int)resolution.y, direction);
                    Color color = Color.Lerp(colorA, colorB, t);
                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();
            return texture;
        }

        private float CalculateGradientValue(int x, int y, int width, int height, GradientDirection direction)
        {
            switch (direction)
            {
                case GradientDirection.Horizontal:
                    return (float)x / (width - 1);

                case GradientDirection.Vertical:
                    return (float)y / (height - 1);

                case GradientDirection.Diagonal:
                    return ((float)x / (width - 1) + (float)y / (height - 1)) * 0.5f;

                default:
                    return 0f;
            }
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_Direction = context.AddOption<GradientDirection>("Direction").Build();
        }
    }
}
