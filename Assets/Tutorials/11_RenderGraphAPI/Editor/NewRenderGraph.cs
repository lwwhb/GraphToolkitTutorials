using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace GraphToolkitTutorials.RenderGraphAPI
{
    /// <summary>
    /// 新 RenderGraph API 驱动的渲染管线图。
    ///
    /// 教学要点：
    ///   • 执行流图（Push 模式）：每个节点代表一组 GPU 操作
    ///   • 节点使用 ContextNode：RenderPassNode 内嵌多个 BlockNode 描述操作列表
    ///   • Editor/Runtime 分离：Importer 将图转为 NewRenderGraphRuntime ScriptableObject
    ///   • IPort 无 .Node 属性 → 必须用 FindNodeForPort 反查节点
    ///   • GetNodes() 返回 IEnumerable，无 IndexOf → 必须用显式循环获取整数索引
    /// 文件扩展名：.newrendergraph
    /// </summary>
    [Graph("newrendergraph", GraphOptions.Default)]
    [Serializable]
    internal class NewRenderGraph : Graph
    {
        [MenuItem("Assets/Create/Graph Toolkit/New RenderGraph")]
        static void CreateGraphAssetFile()
            => GraphDatabase.PromptInProjectBrowserToCreateNewAsset<NewRenderGraph>();

        public PipelineStartNode FindStartNode()
        {
            foreach (var node in GetNodes())
                if (node is PipelineStartNode s) return s;
            return null;
        }

        /// <summary>
        /// IPort 没有 .Node 属性，必须遍历所有节点才能反查端口所属节点。
        /// 这是执行流图中连线遍历的标准辅助方法（见教程 5、7、8、9、10）。
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

        /// <summary>
        /// 将编辑器图转为运行时图。在 ScriptedImporter 中调用。
        ///
        /// 注意：GetNodes() 返回 IEnumerable，需显式循环计数 RenderGraphNode 索引。
        ///       使用 ScriptableObject.CreateInstance 创建 ScriptableObject（不能用 new）。
        /// </summary>
        public Runtime.NewRenderGraphRuntime CreateRuntime()
        {
            var runtime = ScriptableObject.CreateInstance<Runtime.NewRenderGraphRuntime>();

            foreach (var node in GetNodes())
            {
                if (node is RenderGraphNode renderNode)
                {
                    runtime.nodes.Add(renderNode.CreateRuntimeNode(this));

                    if (node is PipelineStartNode)
                        runtime.startNodeIndex = runtime.nodes.Count - 1;
                }
            }

            return runtime;
        }

        public bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;
            var startNode = FindStartNode();
            if (startNode == null)
            {
                errorMessage = "RenderGraph must have a Pipeline Start node";
                return false;
            }
            if (startNode.GetNextNodeIndex(this) < 0)
            {
                errorMessage = "Pipeline Start node has no outgoing connection. " +
                               "Connect it to a Render Pass node or Pipeline End node.";
                return false;
            }
            return true;
        }
    }
}
