using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.VariablesSubgraphs
{
    /// <summary>
    /// 变量节点 - 获取或设置图形变量的值
    /// 演示如何使用IVariableNode接口
    /// </summary>
    [Node("Variable", "Material")]
    [UseWithGraph(typeof(MaterialGraph))]
    internal class VariableNode : Node, IVariableNode, IColorNode, IFloatNode
    {
        [SerializeField]
        private string m_VariableGuid;

        private IPort m_ValueInput;
        private IPort m_ValueOutput;

        /// <summary>
        /// 关联的变量
        /// </summary>
        public IVariable Variable
        {
            get
            {
                if (Graph == null || string.IsNullOrEmpty(m_VariableGuid))
                    return null;

                foreach (var variable in Graph.Variables)
                {
                    if (variable.Guid == m_VariableGuid)
                        return variable;
                }
                return null;
            }
            set
            {
                m_VariableGuid = value?.Guid;
            }
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            var variable = Variable;
            if (variable == null)
            {
                // 如果没有关联变量，创建通用端口
                m_ValueInput = context.AddInputPort("Value").Build();
                m_ValueOutput = context.AddOutputPort("Value").Build();
                return;
            }

            // 根据变量类型创建端口
            if (variable.Type == typeof(Color))
            {
                if (variable.Kind == VariableKind.Input || variable.Kind == VariableKind.Local)
                {
                    m_ValueInput = context.AddInputPort<Color>("Value").Build();
                }
                if (variable.Kind == VariableKind.Output || variable.Kind == VariableKind.Local)
                {
                    m_ValueOutput = context.AddOutputPort<Color>("Value").Build();
                }
            }
            else if (variable.Type == typeof(float))
            {
                if (variable.Kind == VariableKind.Input || variable.Kind == VariableKind.Local)
                {
                    m_ValueInput = context.AddInputPort<float>("Value").Build();
                }
                if (variable.Kind == VariableKind.Output || variable.Kind == VariableKind.Local)
                {
                    m_ValueOutput = context.AddOutputPort<float>("Value").Build();
                }
            }
        }

        public Color EvaluateColor(IPort port, MaterialGraph graph)
        {
            var variable = Variable;
            if (variable == null || variable.Type != typeof(Color))
                return Color.white;

            // 如果是输入变量或局部变量，先评估输入端口
            if ((variable.Kind == VariableKind.Input || variable.Kind == VariableKind.Local) && m_ValueInput != null)
            {
                var connectedPort = graph.GetConnectedOutputPort(m_ValueInput);
                if (connectedPort != null)
                {
                    var color = graph.EvaluateColorPort(connectedPort);
                    variable.Value = color;
                    return color;
                }
            }

            // 返回变量的当前值
            if (variable.Value is Color colorValue)
            {
                return colorValue;
            }

            return Color.white;
        }

        public float EvaluateFloat(IPort port, MaterialGraph graph)
        {
            var variable = Variable;
            if (variable == null || variable.Type != typeof(float))
                return 0f;

            // 如果是输入变量或局部变量，先评估输入端口
            if ((variable.Kind == VariableKind.Input || variable.Kind == VariableKind.Local) && m_ValueInput != null)
            {
                var connectedPort = graph.GetConnectedOutputPort(m_ValueInput);
                if (connectedPort != null)
                {
                    var value = graph.EvaluateFloatPort(connectedPort);
                    variable.Value = value;
                    return value;
                }
            }

            // 返回变量的当前值
            if (variable.Value is float floatValue)
            {
                return floatValue;
            }

            return 0f;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            var variable = Variable;
            if (variable != null)
            {
                context.AddOption("Variable", () => variable.Name, null).Build();
                context.AddOption("Type", () => variable.Type.Name, null).Build();
                context.AddOption("Kind", () => variable.Kind.ToString(), null).Build();
            }
        }
    }
}
