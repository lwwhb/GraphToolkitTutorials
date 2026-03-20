using System;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace GraphToolkitTutorials.ExecutionFlow
{
    /// <summary>
    /// 任务图形 - 执行流图形示例
    /// 演示如何创建基于执行流的图形系统
    /// </summary>
    [Graph("taskgraph", GraphOptions.Default)]
    [Serializable]
    public class TaskGraph : Graph
    {
        [MenuItem("Assets/Create/Graph Toolkit/TaskGraph", false)]
        static void CreateGraphAssetFile()
        {
            GraphDatabase.PromptInProjectBrowserToCreateNewAsset<TaskGraph>();
        }

        /// <summary>
        /// 查找拥有指定端口的节点
        /// </summary>
        public INode FindNodeForPort(IPort port)
        {
            if (port == null) return null;
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
        /// 获取节点在运行时列表中的索引（只计算TaskNode）
        /// </summary>
        public int GetNodeIndex(INode node)
        {
            int index = 0;
            foreach (var n in GetNodes())
            {
                if (n is TaskNode)
                {
                    if (n == node) return index;
                    index++;
                }
            }
            return -1;
        }

        /// <summary>
        /// 查找起始节点
        /// </summary>
        public INode FindStartNode()
        {
            foreach (var node in GetNodes())
            {
                if (node is StartNode startNode)
                    return startNode;
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

            return inputPort.FirstConnectedPort;
        }

        /// <summary>
        /// 获取连接到输出端口的输入端口
        /// </summary>
        public IPort GetConnectedInputPort(IPort outputPort)
        {
            if (outputPort == null || outputPort.Direction != PortDirection.Output)
                return null;

            return outputPort.FirstConnectedPort;
        }

        /// <summary>
        /// 将编辑器图形转换为运行时图形
        /// </summary>
        public Runtime.TaskRuntimeGraph CreateRuntimeGraph()
        {
            var runtimeGraph = ScriptableObject.CreateInstance<Runtime.TaskRuntimeGraph>();

            // 序列化图形变量
            foreach (var variable in GetVariables())
            {
                if (variable.DataType == typeof(bool))
                {
                    variable.TryGetDefaultValue(out bool val);
                    runtimeGraph.variables.Add(new Runtime.TaskRuntimeGraph.RuntimeVariable
                    {
                        name = variable.Name,
                        boolValue = val
                    });
                }
            }

            // 遍历所有节点（与GetNodeIndex保持相同顺序）
            foreach (var node in GetNodes())
            {
                if (node is TaskNode taskNode)
                {
                    if (taskNode is StartNode)
                        runtimeGraph.startNodeIndex = runtimeGraph.nodes.Count;

                    var runtimeNode = taskNode.CreateRuntimeNode(this);
                    runtimeGraph.nodes.Add(runtimeNode);
                }
            }

            return runtimeGraph;
        }
    }
}
