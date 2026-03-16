using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.BehaviorTree
{
    /// <summary>
    /// 根节点 - 行为树的起点
    /// </summary>
    [Node("Root", "Behavior Tree")]
    [UseWithGraph(typeof(BehaviorTreeGraph))]
    internal class RootNode : BTNode
    {
        private IPort m_ChildPort;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // 根节点只有子节点端口，没有父节点端口
            m_ChildPort = context.AddOutputPort("Child")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        public IPort GetChildPort()
        {
            return m_ChildPort;
        }

        public BTNode GetChild(BehaviorTreeGraph graph)
        {
            var connectedPorts = graph.GetConnectedInputPorts(m_ChildPort);
            if (connectedPorts.Count > 0 && connectedPorts[0].Node is BTNode btNode)
            {
                return btNode;
            }
            return null;
        }

        public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
        {
            var runtimeNode = new Runtime.RootNode();

            var child = GetChild(graph);
            if (child != null)
            {
                runtimeNode.childIndex = child.GetNodeIndex(graph);
            }
            else
            {
                runtimeNode.childIndex = -1;
            }

            return runtimeNode;
        }
    }
}
