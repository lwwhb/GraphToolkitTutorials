using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.ContextBlocks
{
    /// <summary>
    /// 输出节点 — 普通 Node（非 BlockNode），接收 FunctionContextNode 的 Result 端口值并打印。
    /// 连接方式：FunctionContextNode.Result → OutputNode.Result
    /// </summary>
    [Node("Output", "")]
    [UseWithGraph(typeof(ShaderFunctionGraph))]
    [Serializable]
    internal class OutputNode : Node
    {
        private IPort m_Input;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_Input = context.AddInputPort<Vector3>("Result").Build();
        }

        /// <summary>
        /// 求值并打印结果，由 Importer 调用。
        /// </summary>
        public void Evaluate(ShaderFunctionGraph graph)
        {
            // TryGetValue 对 INPUT 端口只读内联常量，不读连线上游值。
            // 必须通过 GetConnectedOutputPort 取得上游输出端口，再用 EvaluateVectorPort 求值。
            Vector3 result = Vector3.zero;
            var upstream = graph.GetConnectedOutputPort(m_Input);
            if (upstream != null)
                result = graph.EvaluateVectorPort(upstream);
            else
                m_Input.TryGetValue<Vector3>(out result);
            Debug.Log($"[ShaderFunction] Result = {result}");
        }
    }
}
