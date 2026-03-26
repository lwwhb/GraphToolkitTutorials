using System;
using Unity.GraphToolkit.Editor;

namespace GraphToolkitTutorials.GraphDrivenURP
{
    /// <summary>管线起始节点 — 图的执行入口，只有 Out 端口。</summary>
    [Node("Pipeline Start", "URP")]
    [UseWithGraph(typeof(URPGraph))]
    [Serializable]
    internal class PipelineStartNode : URPNode
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_ExecutionOut = context.AddOutputPort("Out")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        public override Runtime.URPRuntimeNode CreateRuntimeNode(URPGraph graph)
        {
            var nextNode = GetNextNode(graph);
            return new Runtime.PipelineStartNode
            {
                nextNodeIndex = nextNode != null ? nextNode.GetNodeIndex(graph) : -1
            };
        }
    }

    /// <summary>管线结束节点 — 图的执行终点，只有 In 端口。</summary>
    [Node("Pipeline End", "URP")]
    [UseWithGraph(typeof(URPGraph))]
    [Serializable]
    internal class PipelineEndNode : URPNode
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_ExecutionIn = context.AddInputPort("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        public override Runtime.URPRuntimeNode CreateRuntimeNode(URPGraph graph)
        {
            return new Runtime.PipelineEndNode();
        }
    }
}
