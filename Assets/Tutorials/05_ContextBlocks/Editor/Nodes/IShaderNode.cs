using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.ContextBlocks
{
    /// <summary>
    /// 浮点节点接口
    /// </summary>
    internal interface IFloatNode
    {
        float EvaluateFloat(IPort port, ShaderFunctionGraph graph);
    }

    /// <summary>
    /// 向量节点接口
    /// </summary>
    internal interface IVectorNode
    {
        Vector3 EvaluateVector(IPort port, ShaderFunctionGraph graph);
    }
}
