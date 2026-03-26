using System.Collections.Generic;
using System.IO;
using System.Linq;
using GraphToolkitTutorials.AbilitySystem.Runtime;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace GraphToolkitTutorials.AbilitySystem
{
    /// <summary>
    /// 将 .ability 文件导入为 AbilityRuntimeGraph 主资产。
    ///
    /// 两步构建运行时图：
    ///   1. 遍历所有编辑器节点，建立 INode → index 映射
    ///   2. 每个节点调用 CreateRuntimeNode，通过 FindNextIndex 解析连接索引
    ///
    /// 注意：Graph 不是 UnityEngine.Object，不能 AddObjectToAsset(graph)。
    /// </summary>
    [ScriptedImporter(1, "ability")]
    internal class AbilityImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var graph = GraphDatabase.LoadGraphForImporter<AbilityGraph>(ctx.assetPath);

            var runtimeGraph = ScriptableObject.CreateInstance<AbilityRuntimeGraph>();
            runtimeGraph.name = Path.GetFileNameWithoutExtension(ctx.assetPath);

            if (graph != null)
            {
                var allNodes = graph.GetNodes().ToList();

                // 第一步：建立索引映射（只映射实现了 IAbilityEditorNode 的节点）
                var indexMap = new Dictionary<INode, int>();
                for (int i = 0; i < allNodes.Count; i++)
                    if (allNodes[i] is IAbilityEditorNode)
                        indexMap[allNodes[i]] = i;

                // 第二步：创建运行时节点（含连接索引）
                foreach (var node in allNodes)
                    if (node is IAbilityEditorNode abilityNode)
                        runtimeGraph.nodes.Add(abilityNode.CreateRuntimeNode(allNodes, indexMap));

                Debug.Log($"[AbilitySystem] Imported '{runtimeGraph.name}': {runtimeGraph.nodes.Count} nodes");
            }

            ctx.AddObjectToAsset("main", runtimeGraph);
            ctx.SetMainObject(runtimeGraph);
        }
    }
}
