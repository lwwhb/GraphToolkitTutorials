using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.ExecutionFlow
{
    /// <summary>
    /// 任务图形 - 执行流图形示例
    /// 演示如何创建基于执行流的图形系统
    /// </summary>
    [Graph("taskgraph", GraphOptions.None)]
    internal class TaskGraph : Graph
    {
        /// <summary>
        /// 查找起始节点
        /// 执行流图形通常有一个明确的起始点
        /// </summary>
        public StartNode FindStartNode()
        {
            foreach (var node in Nodes)
            {
                if (node is StartNode startNode)
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
        /// 创建运行时图形
        /// 将编辑器图形转换为可序列化的运行时图形
        /// </summary>
        public TaskRuntimeGraph CreateRuntimeGraph()
        {
            var runtimeGraph = ScriptableObject.CreateInstance<TaskRuntimeGraph>();
            runtimeGraph.BuildFromEditorGraph(this);
            return runtimeGraph;
        }
    }
}
