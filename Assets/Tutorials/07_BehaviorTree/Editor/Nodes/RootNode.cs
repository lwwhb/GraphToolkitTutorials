using Unity.GraphToolkit.Editor;

namespace GraphToolkitTutorials.BehaviorTree
{
    /// <summary>
    /// 根节点 — 行为树的起点，有且只有一个子节点
    /// </summary>
    [Node("Root", "Behavior Tree")]
    [UseWithGraph(typeof(BehaviorTreeGraph))]
    [System.Serializable]
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
            if (connectedPorts.Count > 0)
            {
                var node = graph.FindNodeForPort(connectedPorts[0]);
                return node as BTNode;
            }
            return null;
        }

        public override Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph)
        {
            var child = GetChild(graph);
            return new Runtime.RootNode
            {
                childIndex = child != null ? child.GetNodeIndex(graph) : -1
            };
        }
    }
}
