using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.RenderGraphBasics
{
    /// <summary>
    /// 渲染图形 - 图形化URP渲染管线
    /// 演示如何使用GraphToolkit构建渲染管线。
    /// 范式：执行流（Push），Editor/Runtime 分离。
    /// 文件扩展名：.rendergraph
    /// </summary>
    [Graph("rendergraph", GraphOptions.Default)]
    [Serializable]
    internal class RenderGraph : Graph
    {
        [UnityEditor.MenuItem("Assets/Create/Graph Toolkit/Render Graph")]
        static void CreateGraphAssetFile()
            => GraphDatabase.PromptInProjectBrowserToCreateNewAsset<RenderGraph>();

        /// <summary>
        /// 查找相机节点（作为图的起点）
        /// </summary>
        public CameraNode FindCameraNode()
        {
            foreach (var node in GetNodes())
                if (node is CameraNode cameraNode)
                    return cameraNode;
            return null;
        }

        /// <summary>
        /// 根据端口反查所属节点。
        /// 注意：IPort 没有 .Node 属性，必须遍历查找。
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
        /// 获取连接到输出端口的下游输入端口（执行流：从上游查下游）
        /// </summary>
        public IPort GetConnectedInputPort(IPort outputPort)
        {
            if (outputPort == null || outputPort.Direction != PortDirection.Output)
                return null;
            return outputPort.FirstConnectedPort;
        }

        /// <summary>
        /// 获取连接到输入端口的上游输出端口（数据流：从下游查上游）
        /// </summary>
        public IPort GetConnectedOutputPort(IPort inputPort)
        {
            if (inputPort == null || inputPort.Direction != PortDirection.Input)
                return null;
            return inputPort.FirstConnectedPort;
        }

        /// <summary>
        /// 将编辑器图转换为运行时图。在 ScriptedImporter 中调用。
        /// </summary>
        public Runtime.RenderGraphRuntime CreateRuntimeGraph()
        {
            var runtimeGraph = ScriptableObject.CreateInstance<Runtime.RenderGraphRuntime>();

            var allNodes = new List<INode>(GetNodes());
            for (int i = 0; i < allNodes.Count; i++)
            {
                if (allNodes[i] is RenderNode rn)
                {
                    runtimeGraph.nodes.Add(rn.CreateRuntimeNode(this));
                    if (allNodes[i] is CameraNode)
                        runtimeGraph.startNodeIndex = runtimeGraph.nodes.Count - 1;
                }
            }

            return runtimeGraph;
        }

        /// <summary>
        /// 验证渲染图形
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;
            if (FindCameraNode() == null)
            {
                errorMessage = "Render graph must have a Camera node";
                return false;
            }
            return true;
        }
    }
}
