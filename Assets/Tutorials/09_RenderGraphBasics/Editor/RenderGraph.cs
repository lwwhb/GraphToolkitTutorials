using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.RenderGraphBasics
{
    /// <summary>
    /// 渲染图形 - 终极目标：图形化URP渲染管线
    /// 演示如何使用GraphToolkit构建渲染管线
    /// </summary>
    [Graph("rendergraph", GraphOptions.None)]
    internal class RenderGraph : Graph
    {
        /// <summary>
        /// 查找相机节点
        /// </summary>
        public CameraNode FindCameraNode()
        {
            foreach (var node in Nodes)
            {
                if (node is CameraNode cameraNode)
                {
                    return cameraNode;
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
        /// 创建运行时渲染图形
        /// </summary>
        public Runtime.RenderGraphRuntime CreateRuntimeGraph()
        {
            var runtimeGraph = ScriptableObject.CreateInstance<Runtime.RenderGraphRuntime>();
            runtimeGraph.BuildFromEditorGraph(this);
            return runtimeGraph;
        }

        /// <summary>
        /// 验证渲染图形
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;

            // 检查是否有相机节点
            var cameraNode = FindCameraNode();
            if (cameraNode == null)
            {
                errorMessage = "Render graph must have a Camera node";
                return false;
            }

            return true;
        }
    }
}
