using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.VariablesSubgraphs
{
    /// <summary>
    /// 子图节点 - 引用另一个MaterialGraph作为子图
    /// 演示如何使用ISubgraphNode接口
    /// </summary>
    [Node("Subgraph", "Material")]
    [UseWithGraph(typeof(MaterialGraph))]
    internal class SubgraphNode : Node, ISubgraphNode, IColorNode
    {
        [SerializeField]
        private MaterialGraph m_Subgraph;

        private IPort m_Output;

        /// <summary>
        /// 子图资产
        /// </summary>
        public Graph Subgraph
        {
            get => m_Subgraph;
            set => m_Subgraph = value as MaterialGraph;
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // 如果有子图，根据子图的输入/输出变量创建端口
            if (m_Subgraph != null)
            {
                // 为子图的输入变量创建输入端口
                foreach (var variable in m_Subgraph.Variables)
                {
                    if (variable.Kind == VariableKind.Input)
                    {
                        if (variable.Type == typeof(Color))
                        {
                            context.AddInputPort<Color>(variable.Name).Build();
                        }
                        else if (variable.Type == typeof(float))
                        {
                            context.AddInputPort<float>(variable.Name).Build();
                        }
                    }
                }

                // 为子图的输出变量创建输出端口
                foreach (var variable in m_Subgraph.Variables)
                {
                    if (variable.Kind == VariableKind.Output)
                    {
                        if (variable.Type == typeof(Color))
                        {
                            context.AddOutputPort<Color>(variable.Name).Build();
                        }
                        else if (variable.Type == typeof(float))
                        {
                            context.AddOutputPort<float>(variable.Name).Build();
                        }
                    }
                }
            }
            else
            {
                // 没有子图时，创建默认输出端口
                m_Output = context.AddOutputPort<Color>("Output").Build();
            }
        }

        public Color EvaluateColor(IPort port, MaterialGraph graph)
        {
            if (m_Subgraph == null)
            {
                Debug.LogWarning($"SubgraphNode '{Name}': No subgraph assigned");
                return Color.white;
            }

            // 1. 将输入端口的值传递给子图的输入变量
            foreach (var inputPort in Ports)
            {
                if (inputPort.Direction == PortDirection.Input)
                {
                    var connectedPort = graph.GetConnectedOutputPort(inputPort);
                    if (connectedPort != null)
                    {
                        // 找到对应的子图变量
                        var variable = FindSubgraphVariable(inputPort.Name, VariableKind.Input);
                        if (variable != null)
                        {
                            if (variable.Type == typeof(Color))
                            {
                                variable.Value = graph.EvaluateColorPort(connectedPort);
                            }
                            else if (variable.Type == typeof(float))
                            {
                                variable.Value = graph.EvaluateFloatPort(connectedPort);
                            }
                        }
                    }
                }
            }

            // 2. 评估子图
            var outputNode = m_Subgraph.FindOutputNode();
            if (outputNode != null)
            {
                var result = outputNode.EvaluateColor(null, m_Subgraph);

                // 3. 将结果写入子图的输出变量
                if (port != null)
                {
                    var variable = FindSubgraphVariable(port.Name, VariableKind.Output);
                    if (variable != null && variable.Type == typeof(Color))
                    {
                        variable.Value = result;
                    }
                }

                return result;
            }

            return Color.white;
        }

        /// <summary>
        /// 查找子图中的变量
        /// </summary>
        private IVariable FindSubgraphVariable(string name, VariableKind kind)
        {
            if (m_Subgraph == null)
                return null;

            foreach (var variable in m_Subgraph.Variables)
            {
                if (variable.Name == name && variable.Kind == kind)
                {
                    return variable;
                }
            }
            return null;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Subgraph",
                () => m_Subgraph,
                v => {
                    m_Subgraph = v;
                    // 子图改变时需要重新定义端口
                    Graph?.OnGraphChanged();
                }
            ).Build();
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            // 验证子图不能引用自己（避免循环引用）
            if (m_Subgraph == Graph)
            {
                Debug.LogError($"SubgraphNode '{Name}': Cannot reference self as subgraph!");
                m_Subgraph = null;
            }
        }
    }
}
