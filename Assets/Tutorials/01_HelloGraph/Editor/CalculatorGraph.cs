using System;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace GraphToolkitTutorials.HelloGraph
{
    /// <summary>
    /// 计算器图形 - GraphToolkit入门示例
    /// 演示如何创建自定义图形类型
    /// </summary>
    [Graph("calc", GraphOptions.Default)]
    [Serializable]
    public class CalculatorGraph : Graph
    {
        [MenuItem("Assets/Create/Graph Toolkit/CalculatorGraph", false)]
        /// <summary>
        /// 创建计算器图资源
        /// </summary>
        static void CreateGraphAssetFile()
        {
            GraphDatabase.PromptInProjectBrowserToCreateNewAsset<CalculatorGraph>();
        }
        
        /// <summary>
        /// 查找拥有指定端口的节点
        /// </summary>
        private INode FindNodeForPort(IPort port)
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
        /// 评估指定输出端口的值
        /// 这是数据流图形的核心方法，通过递归评估连接的节点来计算结果
        /// </summary>
        public float EvaluatePort(IPort port)
        {
            if (port == null || port.Direction != PortDirection.Output)
                return 0f;

            var node = FindNodeForPort(port);
            if (node is ICalculatorNode calcNode)
            {
                return calcNode.Evaluate(port, this);
            }

            return 0f;
        }

        /// <summary>
        /// 查找指定输出端口连接的输入端口
        /// </summary>
        public IPort GetConnectedInputPort(IPort outputPort)
        {
            if (outputPort == null || outputPort.Direction != PortDirection.Output)
                return null;

            return outputPort.FirstConnectedPort;
        }

        /// <summary>
        /// 查找指定输入端口连接的输出端口
        /// </summary>
        public IPort GetConnectedOutputPort(IPort inputPort)
        {
            if (inputPort == null || inputPort.Direction != PortDirection.Input)
                return null;

            return inputPort.FirstConnectedPort;
        }
    }
}
