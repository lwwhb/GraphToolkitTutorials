using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.DataFlow
{
    /// <summary>
    /// Vector2 常量节点
    /// 输出一个固定的二维向量值
    /// </summary>
    [Node("Texture", "")]
    [Serializable]
    internal class Vector2Node : Node, IVector2Node
    {
        private INodeOption m_Value;
        private IPort m_Output;
        
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_Output = context.AddOutputPort<Vector2>("Value").Build();
        }
        
        public Vector2 EvaluateVector2(IPort port, TextureGraph graph)
        {
            if (m_Value != null && m_Value.TryGetValue(out Vector2 value))
            {
                return value;
            }
            return Vector2.one;
        }
        
        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_Value = context.AddOption<Vector2>("Value").Build();
        }
    }
}