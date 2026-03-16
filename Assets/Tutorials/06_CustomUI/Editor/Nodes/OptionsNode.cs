using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.CustomUI
{
    /// <summary>
    /// 选项节点 - 演示各种节点选项的使用
    /// 展示不同类型的Inspector选项
    /// </summary>
    [Node("Options Demo", "Custom")]
    [UseWithGraph(typeof(CustomGraph))]
    internal class OptionsNode : Node, IColorNode
    {
        // 基本类型
        [SerializeField]
        private float m_FloatValue = 0.5f;

        [SerializeField]
        private int m_IntValue = 10;

        [SerializeField]
        private bool m_BoolValue = true;

        [SerializeField]
        private string m_StringValue = "Hello";

        // Unity类型
        [SerializeField]
        private Color m_ColorValue = Color.red;

        [SerializeField]
        private Vector3 m_VectorValue = Vector3.zero;

        // 枚举
        public enum OperationMode
        {
            Add,
            Multiply,
            Mix
        }

        [SerializeField]
        private OperationMode m_Mode = OperationMode.Add;

        // 范围限制
        [SerializeField]
        [Range(0f, 1f)]
        private float m_NormalizedValue = 0.5f;

        private IPort m_Output;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_Output = context.AddOutputPort<Color>("Color").Build();
        }

        public Color EvaluateColor(IPort port, CustomGraph graph)
        {
            // 根据选项返回不同的颜色
            return m_ColorValue;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            // 基本选项
            context.AddOption("Float Value", () => m_FloatValue, v => m_FloatValue = v).Build();

            context.AddOption("Int Value", () => m_IntValue, v => m_IntValue = v).Build();

            context.AddOption("Bool Value", () => m_BoolValue, v => m_BoolValue = v).Build();

            // 延迟更新的字符串（适合文本输入）
            context.AddOption("String Value", () => m_StringValue, v => m_StringValue = v)
                .Delayed()
                .Build();

            // Unity类型
            context.AddOption("Color", () => m_ColorValue, v => m_ColorValue = v).Build();

            context.AddOption("Vector", () => m_VectorValue, v => m_VectorValue = v).Build();

            // 枚举
            context.AddOption("Mode", () => m_Mode, v => m_Mode = v).Build();

            // 带范围限制的值
            context.AddOption("Normalized", () => m_NormalizedValue, v => m_NormalizedValue = Mathf.Clamp01(v)).Build();

            // 带标签和提示的选项
            context.AddOption("Custom Label", () => m_FloatValue, v => m_FloatValue = v)
                .WithLabel("My Custom Label")
                .WithTooltip("This is a custom tooltip")
                .Build();
        }
    }
}
