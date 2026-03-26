using System;
using System.Collections.Generic;
using GraphToolkitTutorials.AbilitySystem.Runtime;
using Unity.GraphToolkit.Editor;

namespace GraphToolkitTutorials.AbilitySystem
{
    /// <summary>
    /// 事件触发节点 — 技能图的入口。
    ///
    /// 当外部调用 AbilityRunner.FireEvent(eventName) 时，
    /// 运行时从匹配 EventName 的 OnEventRuntimeNode 开始执行。
    ///
    /// 这是"事件驱动"执行流的关键：图不主动轮询，
    /// 而是等待外部事件激活对应入口节点。
    /// </summary>
    [Node("AbilityGraph", "")]
    [UseWithGraph(typeof(AbilityGraph))]
    [Serializable]
    internal class OnEventNode : Node, IAbilityEditorNode
    {
        private INodeOption m_EventNameOption;
        private IPort m_Next;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_Next = context.AddOutputPort("Next")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_EventNameOption = context.AddOption<string>("Event Name").Delayed().Build();
        }

        public AbilityRuntimeNode CreateRuntimeNode(List<INode> allNodes, Dictionary<INode, int> indexMap)
        {
            string eventName = "Default";
            m_EventNameOption?.TryGetValue(out eventName);
            return new OnEventRuntimeNode
            {
                eventName = eventName ?? "Default",
                next      = AbilityGraph.FindNextIndex(m_Next, allNodes, indexMap)
            };
        }
    }
}
