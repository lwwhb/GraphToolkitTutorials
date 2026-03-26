using System;
using Unity.GraphToolkit.Editor;

namespace GraphToolkitTutorials.GraphVersioning
{
    /// <summary>
    /// 平衡输出节点（标记节点）
    /// 无端口、无选项——仅作为图形"已完成"的视觉标识。
    /// BalanceGraphImporter 不依赖此节点，它会扫描所有 EnemyStatsNode 和 WeaponStatsNode。
    /// </summary>
    [Node("Balance Output", "Balance")]
    [UseWithGraph(typeof(BalanceGraph))]
    [Serializable]
    internal class BalanceOutputNode : Node, IBalanceNode { }
}
