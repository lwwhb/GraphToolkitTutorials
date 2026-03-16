using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.BehaviorTree
{
    /// <summary>
    /// 行为树节点基类
    /// 所有行为树节点都继承自此类
    /// </summary>
    internal abstract class BTNode : Node
    {
        /// <summary>
        /// 父节点端口（输入）
        /// </summary>
        protected IPort m_ParentPort;

        /// <summary>
        /// 添加父节点端口
        /// </summary>
        protected void AddParentPort(IPortDefinitionContext context)
        {
            m_ParentPort = context.AddInputPort("Parent")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        /// <summary>
        /// 创建运行时节点
        /// </summary>
        public abstract Runtime.BTRuntimeNode CreateRuntimeNode(BehaviorTreeGraph graph);

        /// <summary>
        /// 获取节点在图形中的索引
        /// </summary>
        public int GetNodeIndex(BehaviorTreeGraph graph)
        {
            return graph.Nodes.IndexOf(this);
        }
    }

    /// <summary>
    /// 复合节点基类
    /// 可以有多个子节点
    /// </summary>
    internal abstract class CompositeNode : BTNode
    {
        /// <summary>
        /// 子节点端口（输出，多连接）
        /// </summary>
        protected IPort m_ChildrenPort;

        /// <summary>
        /// 添加子节点端口
        /// </summary>
        protected void AddChildrenPort(IPortDefinitionContext context)
        {
            m_ChildrenPort = context.AddOutputPort("Children")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .WithCapacity(PortCapacity.Multiple)
                .Build();
        }

        /// <summary>
        /// 获取所有子节点
        /// </summary>
        public System.Collections.Generic.List<BTNode> GetChildren(BehaviorTreeGraph graph)
        {
            var children = new System.Collections.Generic.List<BTNode>();
            var connectedPorts = graph.GetConnectedInputPorts(m_ChildrenPort);

            foreach (var port in connectedPorts)
            {
                if (port.Node is BTNode btNode)
                {
                    children.Add(btNode);
                }
            }

            return children;
        }
    }

    /// <summary>
    /// 装饰节点基类
    /// 只能有一个子节点
    /// </summary>
    internal abstract class DecoratorNode : BTNode
    {
        /// <summary>
        /// 子节点端口（输出，单连接）
        /// </summary>
        protected IPort m_ChildPort;

        /// <summary>
        /// 添加子节点端口
        /// </summary>
        protected void AddChildPort(IPortDefinitionContext context)
        {
            m_ChildPort = context.AddOutputPort("Child")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        /// <summary>
        /// 获取子节点
        /// </summary>
        public BTNode GetChild(BehaviorTreeGraph graph)
        {
            var connectedPorts = graph.GetConnectedInputPorts(m_ChildPort);
            if (connectedPorts.Count > 0 && connectedPorts[0].Node is BTNode btNode)
            {
                return btNode;
            }
            return null;
        }
    }

    /// <summary>
    /// 叶子节点基类
    /// 没有子节点，执行具体的行为
    /// </summary>
    internal abstract class LeafNode : BTNode
    {
        // 叶子节点没有子节点端口
    }
}
