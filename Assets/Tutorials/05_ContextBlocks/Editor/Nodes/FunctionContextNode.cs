using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.ContextBlocks
{
    /// <summary>
    /// 函数上下文节点 - 定义一个自定义函数
    /// 这是一个ContextNode，可以包含BlockNode
    /// 类似于Shader Graph中的Custom Function节点
    /// </summary>
    [Node("Function", "Shader")]
    [UseWithGraph(typeof(ShaderFunctionGraph))]
    internal class FunctionContextNode : ContextNode, IVectorNode
    {
        [SerializeField]
        private string m_FunctionName = "MyFunction";

        private IPort m_Output;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // 函数的输出端口
            m_Output = context.AddOutputPort<Vector3>("Result").Build();
        }

        public Vector3 EvaluateVector(IPort port, ShaderFunctionGraph graph)
        {
            if (port != m_Output)
                return Vector3.zero;

            // 评估函数内部的块节点
            Vector3 result = Vector3.zero;

            // 查找输出块节点
            foreach (var block in GetBlocks())
            {
                if (block is OutputBlockNode outputBlock)
                {
                    result = outputBlock.EvaluateVector(null, graph);
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// 获取所有块节点
        /// </summary>
        public IReadOnlyList<BlockNode> GetBlocks()
        {
            var blocks = new List<BlockNode>();
            foreach (var node in Graph.Nodes)
            {
                if (node is BlockNode block && block.Context == this)
                {
                    blocks.Add(block);
                }
            }
            return blocks;
        }

        /// <summary>
        /// 查找指定类型的输入块
        /// </summary>
        public T FindInputBlock<T>() where T : InputBlockNode
        {
            foreach (var block in GetBlocks())
            {
                if (block is T inputBlock)
                {
                    return inputBlock;
                }
            }
            return null;
        }

        /// <summary>
        /// 查找输出块
        /// </summary>
        public OutputBlockNode FindOutputBlock()
        {
            foreach (var block in GetBlocks())
            {
                if (block is OutputBlockNode outputBlock)
                {
                    return outputBlock;
                }
            }
            return null;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption("Function Name", () => m_FunctionName, v => m_FunctionName = v).Build();
        }

        /// <summary>
        /// 验证函数必须有输出块
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();

            var outputBlock = FindOutputBlock();
            if (outputBlock == null)
            {
                Debug.LogWarning($"Function '{m_FunctionName}' has no output block");
            }
        }
    }
}
