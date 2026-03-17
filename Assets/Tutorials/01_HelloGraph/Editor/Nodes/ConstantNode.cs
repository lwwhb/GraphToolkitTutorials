using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.HelloGraph
{
    /// <summary>
    /// 常量节点 - 输出一个固定的浮点数值
    /// 这是最简单的节点类型，不依赖任何输入
    /// </summary>
    [Node("Calculator", "Assets/Tutorials/01_HelloGraph/Editor/Icons/constant.png")]
    [Serializable]
    internal class ConstantNode : Node, ICalculatorNode
    {
        private INodeOption m_Value;
        /// <summary>
        /// 定义节点的端口
        /// 常量节点只有一个输出端口
        /// </summary>
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddOutputPort<float>("Value").Build();
        }

        /// <summary>
        /// 评估端口值
        /// 常量节点直接返回存储的值
        /// </summary>
        public float Evaluate(IPort port, CalculatorGraph graph)
        {
            if (m_Value != null)
            {
                m_Value.TryGetValue(out float value);
                return value;
            }
            return 0f;
        }

        /// <summary>
        /// 定义节点的选项（在Inspector中显示的属性）
        /// </summary>
        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_Value = context.AddOption<float>("Value").Build();
        }
    }
}
