using System.Collections.Generic;
using UnityEngine;

namespace GraphToolkitTutorials.GraphDrivenURP.Runtime
{
    /// <summary>
    /// 运行时 URP 图形资产（ScriptableObject）。
    ///
    /// 教学重点：
    ///   • [SerializeReference] 支持 URPRuntimeNode 子类的多态序列化与反序列化。
    ///     若使用 [SerializeField]，Unity 只序列化基类字段，运行时 is 类型检查会失败。
    ///   • GetStartNode() 返回 PipelineStartNode，由 GraphDrivenURPFeature 驱动遍历。
    /// </summary>
    public class URPGraphRuntime : ScriptableObject
    {
        [SerializeReference]
        public List<URPRuntimeNode> nodes = new List<URPRuntimeNode>();

        public int startNodeIndex = -1;

        public URPRuntimeNode GetNode(int index)
        {
            if (index >= 0 && index < nodes.Count)
                return nodes[index];
            return null;
        }

        public PipelineStartNode GetStartNode()
        {
            if (startNodeIndex >= 0 && startNodeIndex < nodes.Count)
                return nodes[startNodeIndex] as PipelineStartNode;
            return null;
        }
    }
}
