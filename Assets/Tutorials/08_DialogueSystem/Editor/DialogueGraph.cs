using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.DialogueSystem
{
    /// <summary>
    /// 对话图形 - 实战项目：完整的对话系统
    /// 演示如何使用GraphToolkit构建生产级的对话系统
    /// </summary>
    [Graph("dialogue", GraphOptions.None)]
    internal class DialogueGraph : Graph
    {
        /// <summary>
        /// 查找起始节点
        /// </summary>
        public StartDialogueNode FindStartNode()
        {
            foreach (var node in Nodes)
            {
                if (node is StartDialogueNode startNode)
                {
                    return startNode;
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
        /// 获取连接到输出端口的输入端口
        /// </summary>
        public IPort GetConnectedInputPort(IPort outputPort)
        {
            if (outputPort == null || outputPort.Direction != PortDirection.Output)
                return null;

            foreach (var connection in Connections)
            {
                if (connection.OutputPort == outputPort)
                    return connection.InputPort;
            }

            return null;
        }

        /// <summary>
        /// 创建运行时对话图形
        /// </summary>
        public Runtime.DialogueRuntimeGraph CreateRuntimeGraph()
        {
            var runtimeGraph = ScriptableObject.CreateInstance<Runtime.DialogueRuntimeGraph>();
            runtimeGraph.BuildFromEditorGraph(this);
            return runtimeGraph;
        }

        /// <summary>
        /// 验证对话图形
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;

            // 检查是否有起始节点
            var startNode = FindStartNode();
            if (startNode == null)
            {
                errorMessage = "Dialogue graph must have a Start node";
                return false;
            }

            return true;
        }
    }
}
