using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace GraphToolkitTutorials.GraphDrivenURP
{
    /// <summary>
    /// 完整的图形化 URP 渲染管线。
    /// 教学要点：
    ///   • 执行流图（Push 模式），每个节点代表一个 RenderGraph Pass
    ///   • QualityBranchNode / PlatformBranchNode 在 RecordRenderGraph 阶段分支
    ///   • Editor/Runtime 分离，Importer 将图转为 URPGraphRuntime
    /// 文件扩展名：.urpgraph
    /// </summary>
    [Graph("urpgraph", GraphOptions.Default)]
    [Serializable]
    internal class URPGraph : Graph
    {
        [MenuItem("Assets/Create/Graph Toolkit/URP Graph")]
        static void CreateGraphAssetFile()
            => GraphDatabase.PromptInProjectBrowserToCreateNewAsset<URPGraph>();

        public PipelineStartNode FindStartNode()
        {
            foreach (var node in GetNodes())
                if (node is PipelineStartNode s) return s;
            return null;
        }

        /// <summary>
        /// IPort 没有 .Node 属性，必须遍历所有节点才能反查端口所属节点。
        /// 这是执行流图中连线遍历的标准辅助方法（见教程 7、8、9）。
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

        /// <summary>获取连接到输出端口的下游输入端口（执行流：从上游查下游）。</summary>
        public IPort GetConnectedInputPort(IPort outputPort)
        {
            if (outputPort == null || outputPort.Direction != PortDirection.Output)
                return null;
            return outputPort.FirstConnectedPort;
        }

        /// <summary>获取连接到输入端口的上游输出端口（数据流：从下游查上游）。</summary>
        public IPort GetConnectedOutputPort(IPort inputPort)
        {
            if (inputPort == null || inputPort.Direction != PortDirection.Input)
                return null;
            return inputPort.FirstConnectedPort;
        }

        /// <summary>
        /// 将编辑器图转为运行时图。在 ScriptedImporter 中调用。
        ///
        /// 注意：GetNodes() 返回 IEnumerable，需先转为 List 才能通过遍历得到整数索引。
        /// </summary>
        public Runtime.URPGraphRuntime CreateRuntimeGraph()
        {
            var runtimeGraph = ScriptableObject.CreateInstance<Runtime.URPGraphRuntime>();
            var allNodes = new List<INode>(GetNodes());

            for (int i = 0; i < allNodes.Count; i++)
            {
                if (allNodes[i] is URPNode urpNode)
                {
                    runtimeGraph.nodes.Add(urpNode.CreateRuntimeNode(this));
                    if (allNodes[i] is PipelineStartNode)
                        runtimeGraph.startNodeIndex = runtimeGraph.nodes.Count - 1;
                }
            }
            return runtimeGraph;
        }

        public bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;
            var startNode = FindStartNode();
            if (startNode == null)
            {
                errorMessage = "URP graph must have a Pipeline Start node";
                return false;
            }

            // 检查起始节点是否已连接到下一个节点（如果没有连接，遍历循环会立即退出，Frame Debugger 中看不到任何 Pass）
            if (startNode.GetNextNode(this) == null)
            {
                errorMessage = "Pipeline Start node has no outgoing connection. " +
                               "Connect it to the first pass node or the Pipeline End node.";
                return false;
            }
            return true;
        }
    }
}
