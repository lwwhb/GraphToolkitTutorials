using Unity.GraphToolkit.Editor;

namespace GraphToolkitTutorials.HelloGraph
{
    /// <summary>
    /// 计算器节点接口
    /// 所有计算器节点都需要实现此接口以支持值评估
    /// </summary>
    internal interface ICalculatorNode
    {
        /// <summary>
        /// 评估指定端口的值
        /// </summary>
        /// <param name="port">要评估的输出端口</param>
        /// <param name="graph">所属的图形实例</param>
        /// <returns>计算结果</returns>
        float Evaluate(IPort port, CalculatorGraph graph);
    }
}
