using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.HelloGraph
{
    /// <summary>
    /// 输出节点 - 显示计算结果
    /// 这个节点用于标记图形的最终输出
    /// </summary>
    [Node("Calculator", "Assets/Tutorials/01_HelloGraph/Editor/Icons/output.png")]
    [Serializable]
    internal class OutputNode : Node, ICalculatorNode
    {
        private IPort m_Input;
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_Input = context.AddInputPort<float>("Result").Build();
        }

        public float Evaluate(IPort port, CalculatorGraph graph)
        {
            var connectedPort = graph.GetConnectedOutputPort(m_Input);
            if (connectedPort != null)
            {
                return graph.EvaluatePort(connectedPort);
            }
            return 0f;
        }
    }
}
