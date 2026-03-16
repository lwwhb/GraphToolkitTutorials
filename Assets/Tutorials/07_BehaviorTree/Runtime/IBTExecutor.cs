using System.Collections;

namespace GraphToolkitTutorials.BehaviorTree.Runtime
{
    /// <summary>
    /// 行为树节点执行器接口
    /// </summary>
    public interface IBTExecutor
    {
        /// <summary>
        /// 执行节点
        /// </summary>
        /// <param name="tree">运行时行为树</param>
        /// <param name="nodeIndex">当前节点索引</param>
        /// <param name="blackboard">黑板</param>
        /// <returns>协程，返回节点状态</returns>
        IEnumerator Execute(BehaviorTreeRuntime tree, int nodeIndex, Blackboard blackboard);
    }
}
