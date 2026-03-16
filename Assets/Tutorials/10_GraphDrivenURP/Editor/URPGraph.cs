using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.GraphDrivenURP
{
    /// <summary>
    /// 完整的图形化URP渲染管线
    /// 终极目标：通过图形编辑器配置整个渲染管线
    /// </summary>
    [Graph("urpgraph", GraphOptions.None)]
    internal class URPGraph : Graph
    {
        /// <summary>
        /// 查找管线起始节点
        /// </summary>
        public PipelineStartNode FindStartNode()
        {
            foreach (var node in GetNodes())
            {
                if (node is PipelineStartNode startNode)
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
        /// 创建运行时URP图形
        /// </summary>
        public Runtime.URPGraphRuntime CreateRuntimeGraph()
        {
            var runtimeGraph = ScriptableObject.CreateInstance<Runtime.URPGraphRuntime>();
            runtimeGraph.BuildFromEditorGraph(this);
            return runtimeGraph;
        }

        /// <summary>
        /// 验证URP图形
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;

            // 检查是否有起始节点
            var startNode = FindStartNode();
            if (startNode == null)
            {
                errorMessage = "URP graph must have a Pipeline Start node";
                return false;
            }

            return true;
        }
    }
}
