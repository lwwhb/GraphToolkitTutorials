using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.ContextBlocks
{
    /// <summary>
    /// 向量加法块节点 — accumulated + B
    /// TryGetValue 自动读取：有连线则取上游值，无连线则取编辑器内联常量。
    /// </summary>
    [Node("Operation", "")]
    [UseWithContext(typeof(FunctionContextNode))]
    [Serializable]
    internal class AddBlockNode : BlockNode
    {
        private IPort m_B;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_B = context.AddInputPort<Vector3>("B").Build();
        }

        public Vector3 Apply(Vector3 accumulated)
        {
            Vector3 b = Vector3.zero;
            m_B.TryGetValue<Vector3>(out b);
            return accumulated + b;
        }
    }

    /// <summary>
    /// 标量乘法块节点 — accumulated * Factor
    /// TryGetValue 自动读取：有连线则取上游值，无连线则取编辑器内联常量。
    /// </summary>
    [Node("Operation", "")]
    [UseWithContext(typeof(FunctionContextNode))]
    [Serializable]
    internal class MultiplyBlockNode : BlockNode
    {
        private IPort m_Factor;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_Factor = context.AddInputPort<float>("Factor").Build();
        }

        public Vector3 Apply(Vector3 accumulated)
        {
            float factor = 1f;
            m_Factor.TryGetValue<float>(out factor);
            return accumulated * factor;
        }
    }

    /// <summary>
    /// 向量叉积块节点 — Cross(accumulated, B)
    /// TryGetValue 自动读取：有连线则取上游值，无连线则取编辑器内联常量。
    /// </summary>
    [Node("Operation", "")]
    [UseWithContext(typeof(FunctionContextNode))]
    [Serializable]
    internal class CrossBlockNode : BlockNode
    {
        private IPort m_B;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_B = context.AddInputPort<Vector3>("B").Build();
        }

        public Vector3 Apply(Vector3 accumulated)
        {
            Vector3 b = Vector3.one;
            m_B.TryGetValue<Vector3>(out b);
            return Vector3.Cross(accumulated, b);
        }
    }

    /// <summary>
    /// 向量归一化块节点 — normalize(accumulated)，无端口。
    /// </summary>
    [Node("Operation", "")]
    [UseWithContext(typeof(FunctionContextNode))]
    [Serializable]
    internal class NormalizeBlockNode : BlockNode
    {
        public Vector3 Apply(Vector3 accumulated) => accumulated.normalized;
    }
}
