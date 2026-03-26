using System;
using System.Collections.Generic;
using GraphToolkitTutorials.AbilitySystem.Runtime;
using Unity.GraphToolkit.Editor;

namespace GraphToolkitTutorials.AbilitySystem
{
    /// <summary>
    /// 并行节点 — 同时激活 Branch A 和 Branch B 两条执行分支，
    /// 等待两条分支全部执行完毕后，再触发 Done 端口继续后续流程。
    ///
    /// 这是本教程的核心新概念：相比 03 教程的纯串行执行，
    /// ParallelNode 允许多件事同时发生（如同时播放特效和音效）。
    ///
    /// 运行时实现：AbilityRunner 用两个协程分别执行两条分支，
    /// 通过 WaitUntil 等待两个完成标志都变为 true。
    /// </summary>
    [Node("AbilityGraph", "")]
    [UseWithGraph(typeof(AbilityGraph))]
    [Serializable]
    internal class ParallelNode : Node, IAbilityEditorNode
    {
        private IPort m_In;
        private IPort m_BranchA;
        private IPort m_BranchB;
        private IPort m_Done;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            m_In      = context.AddInputPort("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead).Build();
            m_BranchA = context.AddOutputPort("Branch A")
                .WithConnectorUI(PortConnectorUI.Arrowhead).Build();
            m_BranchB = context.AddOutputPort("Branch B")
                .WithConnectorUI(PortConnectorUI.Arrowhead).Build();
            m_Done    = context.AddOutputPort("Done")
                .WithConnectorUI(PortConnectorUI.Arrowhead).Build();
        }

        public AbilityRuntimeNode CreateRuntimeNode(List<INode> allNodes, Dictionary<INode, int> indexMap)
        {
            return new ParallelRuntimeNode
            {
                branchA = AbilityGraph.FindNextIndex(m_BranchA, allNodes, indexMap),
                branchB = AbilityGraph.FindNextIndex(m_BranchB, allNodes, indexMap),
                done    = AbilityGraph.FindNextIndex(m_Done,    allNodes, indexMap)
            };
        }
    }
}
