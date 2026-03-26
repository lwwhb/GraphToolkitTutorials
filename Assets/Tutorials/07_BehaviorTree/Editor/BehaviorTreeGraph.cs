using System.Collections.Generic;
using Unity.GraphToolkit.Editor;

namespace GraphToolkitTutorials.BehaviorTree
{
    /// <summary>
    /// 行为树图形 - 实战项目：完整的AI行为树系统
    ///
    /// 演示如何使用GraphToolkit构建生产级的行为树。
    /// 范式：执行流（Push），Editor/Runtime 分离。
    /// 文件扩展名：.behaviortree
    /// </summary>
    [Graph("behaviortree", GraphOptions.Default)]
    [System.Serializable]
    internal class BehaviorTreeGraph : Graph
    {
        /// <summary>
        /// 查找根节点
        /// </summary>
        public RootNode FindRootNode()
        {
            foreach (var node in GetNodes())
                if (node is RootNode rootNode)
                    return rootNode;
            return null;
        }

        /// <summary>
        /// 根据端口查找所属节点
        /// </summary>
        public INode FindNodeForPort(IPort port)
        {
            foreach (var node in GetNodes())
            {
                foreach (var p in node.GetInputPorts())
                    if (p == port) return node;
                foreach (var p in node.GetOutputPorts())
                    if (p == port) return node;
            }
            return null;
        }

        /// <summary>
        /// 获取连接到输入端口的上游输出端口
        /// </summary>
        public IPort GetConnectedOutputPort(IPort inputPort)
        {
            if (inputPort == null || inputPort.Direction != PortDirection.Input)
                return null;
            return inputPort.FirstConnectedPort;
        }

        /// <summary>
        /// 获取连接到输出端口的所有下游输入端口（用于多连接端口，如 Children）
        /// </summary>
        public List<IPort> GetConnectedInputPorts(IPort outputPort)
        {
            var ports = new List<IPort>();
            if (outputPort == null || outputPort.Direction != PortDirection.Output)
                return ports;

            foreach (var node in GetNodes())
                foreach (var inputPort in node.GetInputPorts())
                    if (inputPort.FirstConnectedPort == outputPort)
                        ports.Add(inputPort);

            return ports;
        }

        /// <summary>
        /// 验证行为树结构
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;

            var rootNode = FindRootNode();
            if (rootNode == null)
            {
                errorMessage = "Behavior tree must have a Root node";
                return false;
            }

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
