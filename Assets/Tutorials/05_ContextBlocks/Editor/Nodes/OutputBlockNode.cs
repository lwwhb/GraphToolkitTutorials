using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.ContextBlocks
{
    /// <summary>
    /// 输出块节点
    /// 定义函数的返回值
    /// BlockNode只能存在于ContextNode内部
    /// </summary>
    [Node("Output", "Shader/Block")]
    [UseWithGraph(typeof(ShaderFunctionGraph))]
    internal class OutputBlockNode : BlockNode, IVectorNode
    {
        private IPort m_Input;

        /// <summary>
        /// 所属的上下文节点
        /// </summary>
        public FunctionContextNode Context
        {
            get
            {
                foreach (var node in Graph.GetNodes())
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
            m_Input = context.AddInputPort<Vector3>("Result").Build();
        }

        public Vector3 EvaluateVector(IPort port, ShaderFunctionGraph graph)
        {
            // 评估输入端口
            var connectedPort = graph.GetConnectedOutputPort(m_Input);
            if (connectedPort != null)
            {
                return graph.EvaluateVectorPort(connectedPort);
            }

            return Vector3.zero;
        }
    }
}
