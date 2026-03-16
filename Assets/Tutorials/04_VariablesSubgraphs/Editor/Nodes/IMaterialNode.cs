using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.VariablesSubgraphs
{
    /// <summary>
    /// 颜色节点接口
    /// </summary>
    internal interface IColorNode
    {
        Color EvaluateColor(IPort port, MaterialGraph graph);
    }

    /// <summary>
    /// 浮点节点接口
    /// </summary>
    internal interface IFloatNode
    {
        float EvaluateFloat(IPort port, MaterialGraph graph);
    }
}
