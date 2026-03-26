using UnityEngine;

namespace GraphToolkitTutorials.VariablesSubgraphs
{
    /// <summary>
    /// MaterialSubgraphAsset — 子图资产的占位对象
    /// .matconstsubgraph 和 .matvarsubgraph 两种子图共用同一资产类型，
    /// 用于在 Project 面板中表示子图文件并允许父图引用它们。
    /// </summary>
    public class MaterialSubgraphAsset : ScriptableObject { }
}
