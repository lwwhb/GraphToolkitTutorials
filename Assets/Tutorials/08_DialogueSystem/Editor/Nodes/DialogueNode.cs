using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;

namespace GraphToolkitTutorials.DialogueSystem
{
    /// <summary>
    /// 对话节点基类
    /// 所有对话节点都继承自此类
    /// </summary>
    [Serializable]
    internal abstract class DialogueNode : Node
    {
        /// <summary>
        /// 输入端口（执行流）
        /// </summary>
        protected IPort m_InputPort;

        /// <summary>
        /// 添加输入端口
        /// </summary>
        protected void AddInputPort(IPortDefinitionContext context)
        {
            m_InputPort = context.AddInputPort("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        /// <summary>
        /// 创建运行时节点
        /// </summary>
        public abstract Runtime.DialogueRuntimeNode CreateRuntimeNode(DialogueGraph graph);

        /// <summary>
        /// 获取节点在图形中的索引
        /// </summary>
        public int GetNodeIndex(DialogueGraph graph)
        {
            var allNodes = new List<INode>(graph.GetNodes());
            for (int i = 0; i < allNodes.Count; i++)
                if (allNodes[i] == this) return i;
            return -1;
        }
    }
}
