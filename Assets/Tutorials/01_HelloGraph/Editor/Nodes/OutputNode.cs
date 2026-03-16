using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.HelloGraph
{
    /// <summary>
    /// 输出节点 - 显示计算结果
    /// 这个节点用于标记图形的最终输出
    /// </summary>
    [Node("Output", "Calculator")]
    internal class OutputNode : Node, ICalculatorNode
    {
        private IPort m_Input;

        [SerializeField]
        private float m_CachedResult = 0f;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_Input = context.AddInputPort<float>("Input").Build();
        }

        public float Evaluate(IPort port, CalculatorGraph graph)
        {
            var connectedPort = graph.GetConnectedOutputPort(m_Input);
            if (connectedPort != null)
            {
                m_CachedResult = graph.EvaluatePort(connectedPort);
            }
            else
            {
                m_CachedResult = 0f;
            }

            return m_CachedResult;
        }

        /// <summary>
        /// 获取当前缓存的结果
        /// </summary>
        public float GetResult()
        {
            return m_CachedResult;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Result", () => m_CachedResult, null).Build();
        }
    }
}
