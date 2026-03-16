using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.CustomUI
{
    /// <summary>
    /// 浮点节点接口
    /// </summary>
    internal interface IFloatNode
    {
        float EvaluateFloat(IPort port, CustomGraph graph);
    }

    /// <summary>
    /// 颜色节点接口
    /// </summary>
    internal interface IColorNode
    {
        Color EvaluateColor(IPort port, CustomGraph graph);
    }
}
