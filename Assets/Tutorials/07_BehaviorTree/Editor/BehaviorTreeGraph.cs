using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.BehaviorTree
{
    /// <summary>
    /// 行为树图形 - 实战项目：完整的AI行为树系统
    /// 演示如何使用GraphToolkit构建生产级的行为树
    /// </summary>
    [Graph("behaviortree", GraphOptions.None)]
    internal class BehaviorTreeGraph : Graph
    {
        /// <summary>
        /// 查找根节点
        /// </summary>
        public RootNode FindRootNode()
        {
            foreach (var node in GetNodes())
            {
                if (node is RootNode rootNode)
                {
                    return rootNode;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取连接到输入端口的输出端口
        /// </summary>
        public IPort GetConnectedOutputPort(IPort inputPort)
        {
            if (inputPort == null || inputPort.Direction != PortDirection.Input)
                return null;

            foreach (var connection in Connections)
            {
                if (connection.InputPort == inputPort)
                    return connection.OutputPort;
            }

            return null;
        }

        /// <summary>
        /// 获取连接到输出端口的所有输入端口
        /// </summary>
        public System.Collections.Generic.List<IPort> GetConnectedInputPorts(IPort outputPort)
        {
            var ports = new System.Collections.Generic.List<IPort>();

            if (outputPort == null || outputPort.Direction != PortDirection.Output)
                return ports;

            foreach (var connection in Connections)
            {
                if (connection.OutputPort == outputPort)
                {
                    ports.Add(connection.InputPort);
                }
            }

            return ports;
        }

        /// <summary>
        /// 创建运行时行为树
        /// </summary>
        public Runtime.BehaviorTreeRuntime CreateRuntimeTree()
        {
            var runtimeTree = ScriptableObject.CreateInstance<Runtime.BehaviorTreeRuntime>();
            runtimeTree.BuildFromEditorGraph(this);
            return runtimeTree;
        }

        /// <summary>
        /// 验证行为树
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;

            // 检查是否有根节点
            var rootNode = FindRootNode();
            if (rootNode == null)
            {
                errorMessage = "Behavior tree must have a Root node";
                return false;
            }

            // 检查根节点是否有子节点
            var childPorts = GetConnectedInputPorts(rootNode.GetChildPort());
            if (childPorts.Count == 0)
            {
                errorMessage = "Root node must have a child node";
                return false;
            }

            return true;
        }
    }
}
