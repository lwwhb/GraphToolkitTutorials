using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.GraphDrivenURP
{
    /// <summary>
    /// 管线起始节点
    /// </summary>
    [Node("Pipeline Start", "URP")]
    [UseWithGraph(typeof(URPGraph))]
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
            var runtimeNode = new Runtime.PipelineStartNode();

            var nextNode = GetNextNode(graph);
            if (nextNode != null)
            {
                runtimeNode.nextNodeIndex = nextNode.GetNodeIndex(graph);
            }
            else
            {
                runtimeNode.nextNodeIndex = -1;
            }

            return runtimeNode;
        }
    }

    /// <summary>
    /// 管线结束节点
    /// </summary>
    [Node("Pipeline End", "URP")]
    [UseWithGraph(typeof(URPGraph))]
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
