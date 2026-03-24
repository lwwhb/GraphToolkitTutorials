using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.ContextBlocks
{
    /// <summary>
    /// 函数上下文节点 — 容纳 BlockNode，对外暴露 Input 输入端口和 Result 输出端口。
    ///
    /// 求值流程：
    ///   1. 从 Input 端口取初始值（未连接则为 Vector3.zero）
    ///   2. 依次应用每个 BlockNode 的变换，累积结果
    ///   3. 将最终结果从 Result 端口输出
    ///
    /// 框架内置 ContextNode API：
    ///   BlockNodes      — IEnumerable&lt;BlockNode&gt;，遍历所有子块
    ///   BlockCount      — int，子块数量
    ///   GetBlock(index) — 按索引获取子块
    ///
    /// 注意：Graph.GetNodes() 不包含 BlockNode，必须通过 ContextNode.BlockNodes 访问。
    /// </summary>
    [Node("Function", "")]
    [UseWithGraph(typeof(ShaderFunctionGraph))]
    [Serializable]
    internal class FunctionContextNode : ContextNode
    {
        private IPort m_Input;
        private IPort m_Output;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_Input  = context.AddInputPort<Vector3>("Input").Build();
            m_Output = context.AddOutputPort<Vector3>("Result").Build();
        }

        public Vector3 EvaluateVector(ShaderFunctionGraph graph)
        {
            // TryGetValue 对 INPUT 端口只读内联常量，不读连线上游值。
            // 必须通过 GetConnectedOutputPort + EvaluateVectorPort 取得上游值；
            // EvaluateVectorPort 对内置常量节点（ConstantNodeModelImp）有 TryGetValue 兜底。
            Vector3 result = Vector3.zero;
            var upstream = graph.GetConnectedOutputPort(m_Input);
            if (upstream != null)
                result = graph.EvaluateVectorPort(upstream);
            else
                m_Input.TryGetValue<Vector3>(out result); // 未连线时读内联常量

            foreach (var block in BlockNodes)
            {
                if(block is AddBlockNode add)       
                    result = add.Apply(result);
                else if (block is MultiplyBlockNode mul)  
                    result = mul.Apply(result);
                else if (block is CrossBlockNode cross)    
                    result = cross.Apply(result);
                else if (block is NormalizeBlockNode norm) 
                    result = norm.Apply(result);
            }

            return result;
        }
    }
}
