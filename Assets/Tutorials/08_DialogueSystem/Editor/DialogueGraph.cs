using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using UnityEditor;

namespace GraphToolkitTutorials.DialogueSystem
{
    /// <summary>
    /// 对话图形 - 实战项目：完整的对话系统
    /// 演示如何使用GraphToolkit构建生产级的对话系统。
    /// 范式：执行流（Push），Editor/Runtime 分离。
    /// 文件扩展名：.dialogue
    /// </summary>
    [Graph("dialogue", GraphOptions.Default)]
    [Serializable]
    internal class DialogueGraph : Graph
    {
        [MenuItem("Assets/Create/Graph Toolkit/Dialogue Graph")]
        static void CreateGraphAssetFile()
            => GraphDatabase.PromptInProjectBrowserToCreateNewAsset<DialogueGraph>();

        /// <summary>
        /// 查找起始节点
        /// </summary>
        public StartDialogueNode FindStartNode()
        {
            foreach (var node in GetNodes())
                if (node is StartDialogueNode startNode)
                    return startNode;
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
        /// 获取连接到输出端口的下游输入端口（单连接）
        /// </summary>
        public IPort GetConnectedInputPort(IPort outputPort)
        {
            if (outputPort == null || outputPort.Direction != PortDirection.Output)
                return null;
            return outputPort.FirstConnectedPort;
        }

        /// <summary>
        /// 验证对话图形
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;
            if (FindStartNode() == null)
            {
                errorMessage = "Dialogue graph must have a Start node";
                return false;
            }
            return true;
        }
    }
}
