using System.Collections.Generic;
using UnityEngine;

namespace GraphToolkitTutorials.GraphDrivenURP.Runtime
{
    /// <summary>
    /// 运行时URP图形
    /// 包含所有运行时节点的可序列化版本
    /// </summary>
    public class URPGraphRuntime : ScriptableObject
    {
        /// <summary>
        /// 所有运行时节点
        /// </summary>
        public List<URPRuntimeNode> nodes = new List<URPRuntimeNode>();

        /// <summary>
        /// 起始节点索引
        /// </summary>
        public int startNodeIndex = -1;

        /// <summary>
        /// 从编辑器图形构建运行时URP图形
        /// </summary>
}
