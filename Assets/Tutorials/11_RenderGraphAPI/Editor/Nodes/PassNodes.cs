using System;
using Unity.GraphToolkit.Editor;

namespace GraphToolkitTutorials.RenderGraphAPI
{
    /// <summary>
    /// 渲染通道节点 — ContextNode，通过内嵌 BlockNode 描述一组 GPU 操作。
    ///
    /// 教学要点（ContextNode 核心模式）：
    ///   • 继承 RenderGraphNode（ContextNode），可容纳多个 BlockNode
    ///   • CreateRuntimeNode 遍历 BlockNodes，将每个 BlockNode 转为对应 PassOperation
    ///   • Graph.GetNodes() 不包含 BlockNode，必须通过 ContextNode.BlockNodes 属性访问
    ///   • 同一通道内可组合 Clear → DrawOpaque → DrawTransparent 等多种操作
    ///
    /// 与 Tutorial 10 的对比：
    ///   T10：每种操作对应独立节点（OpaquePassNode、TransparentPassNode 等）
    ///   T11：一个 RenderPassNode（ContextNode）内嵌多个 BlockNode，灵活组合操作
    /// </summary>
    [Node("RenderGraphPass", "")]
    [UseWithGraph(typeof(NewRenderGraph))]
    [Serializable]
    internal class RenderPassNode : RenderGraphNode
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddExecutionPorts(context);
        }

        public override Runtime.RenderGraphRuntimeNode CreateRuntimeNode(NewRenderGraph graph)
        {
            var node = new Runtime.RenderPassDataRuntimeNode
            {
                nextNodeIndex = GetNextNodeIndex(graph)
            };

            // 遍历 BlockNodes（注意：GetNodes() 不包含 BlockNode，必须用 BlockNodes 属性）
            foreach (var block in BlockNodes)
            {
                if (block is ClearBlockNode clear)
                    node.operations.Add(clear.CreateOperation());
                else if (block is DrawOpaqueBlockNode opaque)
                    node.operations.Add(opaque.CreateOperation());
                else if (block is DrawTransparentBlockNode transparent)
                    node.operations.Add(transparent.CreateOperation());
                else if (block is BlitBlockNode blit)
                    node.operations.Add(blit.CreateOperation());
            }

            return node;
        }
    }
}
