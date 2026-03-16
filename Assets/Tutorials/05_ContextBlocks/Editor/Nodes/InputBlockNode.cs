using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.ContextBlocks
{
    /// <summary>
    /// 输入块节点基类
    /// BlockNode只能存在于ContextNode内部
    /// </summary>
    internal abstract class InputBlockNode : BlockNode, IVectorNode
    {
        protected IPort m_Output;

        /// <summary>
        /// 所属的上下文节点
        /// </summary>
        public FunctionContextNode Context
        {
            get
            {
                // BlockNode必须有一个父ContextNode
                foreach (var node in Graph.Nodes)
                {
                    if (node is FunctionContextNode context)
                    {
                        foreach (var block in context.GetBlocks())
                        {
                            if (block == this)
                                return context;
                        }
                    }
                }
                return null;
            }
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_Output = context.AddOutputPort<Vector3>("Value").Build();
        }

        public abstract Vector3 EvaluateVector(IPort port, ShaderFunctionGraph graph);
    }

    /// <summary>
    /// 输入A块节点
    /// 代表函数的第一个输入参数
    /// </summary>
    [Node("Input A", "Shader/Block")]
    [UseWithGraph(typeof(ShaderFunctionGraph))]
    internal class InputABlockNode : InputBlockNode
    {
        [SerializeField]
        private Vector3 m_DefaultValue = Vector3.zero;

        public override Vector3 EvaluateVector(IPort port, ShaderFunctionGraph graph)
        {
            // 在实际应用中，这个值会从函数调用者传入
            // 这里返回默认值用于演示
            return m_DefaultValue;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Default Value", () => m_DefaultValue, v => m_DefaultValue = v).Build();
        }
    }

    /// <summary>
    /// 输入B块节点
    /// 代表函数的第二个输入参数
    /// </summary>
    [Node("Input B", "Shader/Block")]
    [UseWithGraph(typeof(ShaderFunctionGraph))]
    internal class InputBBlockNode : InputBlockNode
    {
        [SerializeField]
        private Vector3 m_DefaultValue = Vector3.one;

        public override Vector3 EvaluateVector(IPort port, ShaderFunctionGraph graph)
        {
            return m_DefaultValue;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Default Value", () => m_DefaultValue, v => m_DefaultValue = v).Build();
        }
    }
}
