using System;
using System.Collections.Generic;
using GraphToolkitTutorials.AbilitySystem.Runtime;
using Unity.GraphToolkit.Editor;

namespace GraphToolkitTutorials.AbilitySystem
{
    /// <summary>
    /// 等待节点 — 暂停执行指定秒数后继续。
    /// 运行时通过 WaitForSeconds(duration) 协程实现。
    /// </summary>
    [Node("Wait", "Ability/Action")]
    [UseWithGraph(typeof(AbilityGraph))]
    [Serializable]
    internal class WaitNode : Node, IAbilityEditorNode
    {
        private INodeOption m_DurationOption;
        private IPort m_In;
        private IPort m_Next;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_In   = context.AddInputPort("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead).Build();
            m_Next = context.AddOutputPort("Next")
                .WithConnectorUI(PortConnectorUI.Arrowhead).Build();
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_DurationOption = context.AddOption<float>("Duration").Build();
        }

        public AbilityRuntimeNode CreateRuntimeNode(List<INode> allNodes, Dictionary<INode, int> indexMap)
        {
            float duration = 1f;
            m_DurationOption?.TryGetValue(out duration);
            return new WaitRuntimeNode
            {
                duration = duration,
                next     = AbilityGraph.FindNextIndex(m_Next, allNodes, indexMap)
            };
        }
    }

    /// <summary>
    /// 动作节点 — 执行一个具名动作（用 Debug.Log 模拟技能效果）。
    /// 实际项目中可替换为播放特效、造成伤害、施加 Buff 等逻辑。
    /// </summary>
    [Node("AbilityGraph", "")]
    [UseWithGraph(typeof(AbilityGraph))]
    [Serializable]
    internal class LogActionNode : Node, IAbilityEditorNode
    {
        private INodeOption m_MessageOption;
        private IPort m_In;
        private IPort m_Next;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_In   = context.AddInputPort("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead).Build();
            m_Next = context.AddOutputPort("Next")
                .WithConnectorUI(PortConnectorUI.Arrowhead).Build();
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_MessageOption = context.AddOption<string>("Message").Delayed().Build();
        }

        public AbilityRuntimeNode CreateRuntimeNode(List<INode> allNodes, Dictionary<INode, int> indexMap)
        {
            string message = "Action";
            m_MessageOption?.TryGetValue(out message);
            return new LogActionRuntimeNode
            {
                message = message ?? "Action",
                next    = AbilityGraph.FindNextIndex(m_Next, allNodes, indexMap)
            };
        }
    }
}
