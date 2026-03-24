using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using UnityEditor;

namespace GraphToolkitTutorials.AbilitySystem
{
    /// <summary>
    /// 技能执行图 — 演示事件驱动触发 + 并行执行分支。
    /// 文件扩展名 .ability；执行流图（Push 模式）。
    /// </summary>
    [Graph("ability", GraphOptions.Default)]
    [Serializable]
    internal class AbilityGraph : Graph
    {
        [MenuItem("Assets/Create/Graph Toolkit/Ability Graph", false)]
        static void CreateGraphAssetFile()
            => GraphDatabase.PromptInProjectBrowserToCreateNewAsset<AbilityGraph>();

        /// <summary>
        /// 给定一个执行 OUTPUT 端口，返回其连接的下游节点的索引（-1 表示未连接）。
        /// 用于 Importer 中各 Editor 节点构建运行时连接关系。
        /// </summary>
        internal static int FindNextIndex(
            IPort outputPort,
            List<INode> allNodes,
            Dictionary<INode, int> indexMap)
        {
            if (outputPort == null) return -1;
            // 执行 OUTPUT 端口的 FirstConnectedPort 是下游节点的 INPUT 端口
            var connectedInput = outputPort.FirstConnectedPort;
            if (connectedInput == null) return -1;
            foreach (var node in allNodes)
                foreach (var p in node.GetInputPorts())
                    if (p == connectedInput)
                        return indexMap.TryGetValue(node, out int idx) ? idx : -1;
            return -1;
        }
    }
}
