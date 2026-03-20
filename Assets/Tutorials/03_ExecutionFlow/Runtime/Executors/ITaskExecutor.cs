using System.Collections;

namespace GraphToolkitTutorials.ExecutionFlow.Runtime
{
    /// <summary>
    /// 任务节点执行器接口
    /// 每个运行时节点类型都需要对应的执行器
    /// </summary>
    public interface ITaskExecutor
    {
        /// <summary>
        /// 执行节点
        /// </summary>
        /// <param name="graph">运行时图形</param>
        /// <param name="nodeIndex">当前节点索引</param>
        /// <returns>协程，返回下一个要执行的节点索引</returns>
        IEnumerator Execute(TaskRuntimeGraph graph, int nodeIndex);
    }
}
