using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.CustomUI
{
    /// <summary>
    /// 多端口节点 - 演示不同的端口配置
    /// 展示端口容量、连接器样式等特性
    /// </summary>
    [Node("Multi Port", "Custom")]
    [UseWithGraph(typeof(CustomGraph))]
    internal class MultiPortNode : Node, IFloatNode
    {
        private IPort m_SingleInput;
        private IPort m_MultipleInput;
        private IPort m_ExecutionInput;
        private IPort m_Output;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // 单连接输入端口（默认）
            m_SingleInput = context.AddInputPort<float>("Single Input")
                .WithCapacity(PortCapacity.Single)
                .Build();

            // 多连接输入端口
            m_MultipleInput = context.AddInputPort<float>("Multiple Input")
                .WithCapacity(PortCapacity.Multiple)
                .Build();

            // 执行流端口（箭头样式）
            m_ExecutionInput = context.AddInputPort("Execution")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // 输出端口
            m_Output = context.AddOutputPort<float>("Output").Build();
        }

        public float EvaluateFloat(IPort port, CustomGraph graph)
        {
            if (port != m_Output)
                return 0f;

            float result = 0f;

            // 评估单连接输入
            var connectedPort = graph.GetConnectedOutputPort(m_SingleInput);
            if (connectedPort != null)
            {
                result += graph.EvaluateFloatPort(connectedPort);
            }

            // 评估多连接输入（需要遍历所有连接）
            foreach (var connection in graph.Connections)
            {
                if (connection.InputPort == m_MultipleInput)
                {
                    result += graph.EvaluateFloatPort(connection.OutputPort);
                }
            }

            return result;
        }
    }
}
