using Unity.GraphToolkit.Editor;

namespace GraphToolkitTutorials.RenderGraphAPI
{
    /// <summary>
    /// 新RenderGraph API渲染图形
    /// 使用Unity 6的新RenderGraph API替代旧的CommandBuffer方式
    /// </summary>
    [Graph("newrendergraph", GraphOptions.None)]
    internal class NewRenderGraph : Graph
    {
        /// <summary>
        /// 创建运行时渲染图
        /// </summary>
        public Runtime.NewRenderGraphRuntime CreateRuntime()
        {
            var runtime = new Runtime.NewRenderGraphRuntime();

            // 收集所有渲染节点
            foreach (var node in Nodes)
            {
                if (node is RenderGraphNode renderNode)
                {
                    runtime.AddNode(renderNode.CreateRuntimeNode(this));
                }
            }

            // 找到起始节点
            foreach (var node in Nodes)
            {
                if (node is PipelineStartNode startNode)
                {
                    runtime.StartNodeIndex = startNode.GetNodeIndex(this);
                    break;
                }
            }

            return runtime;
        }
    }
}
