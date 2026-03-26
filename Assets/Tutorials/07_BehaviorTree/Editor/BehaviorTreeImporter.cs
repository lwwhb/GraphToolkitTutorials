using System.Collections.Generic;
using System.IO;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace GraphToolkitTutorials.BehaviorTree
{
    /// <summary>
    /// 行为树图形资产导入器
    /// 负责将 .behaviortree 文件转换为 BehaviorTreeRuntime ScriptableObject。
    ///
    /// 转换流程：
    ///   1. LoadGraphForImporter 加载编辑器图形
    ///   2. 遍历所有 BTNode，调用 CreateRuntimeNode(graph) 生成运行时节点列表
    ///   3. 记录 rootNodeIndex（根节点在列表中的位置）
    ///   4. 将 BehaviorTreeRuntime 作为主资产输出
    /// </summary>
    [ScriptedImporter(1, "behaviortree")]
    internal class BehaviorTreeImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var graph = GraphDatabase.LoadGraphForImporter<BehaviorTreeGraph>(ctx.assetPath);

            var runtimeTree = ScriptableObject.CreateInstance<Runtime.BehaviorTreeRuntime>();
            runtimeTree.name = Path.GetFileNameWithoutExtension(ctx.assetPath);

            if (graph != null)
            {
                // 可选：验证结构（仅用于调试提示，不阻断导入）
                if (!graph.Validate(out string errorMessage))
                    Debug.LogWarning($"[BehaviorTree] {ctx.assetPath}: {errorMessage}");

                // 收集所有编辑器节点（与 GetNodeIndex 使用相同的节点列表顺序）
                var allNodes = new List<Unity.GraphToolkit.Editor.INode>(graph.GetNodes());

                // 为每个 BTNode 创建对应的运行时节点
                for (int i = 0; i < allNodes.Count; i++)
                {
                    if (allNodes[i] is BTNode btNode)
                        runtimeTree.nodes.Add(btNode.CreateRuntimeNode(graph));
                }

                // 记录根节点索引
                for (int i = 0; i < allNodes.Count; i++)
                {
                    if (allNodes[i] is RootNode)
                    {
                        runtimeTree.rootNodeIndex = i;
                        break;
                    }
                }

                Debug.Log($"[BehaviorTree] Imported: {runtimeTree.nodes.Count} nodes, root={runtimeTree.rootNodeIndex}");
            }

            ctx.AddObjectToAsset("main", runtimeTree);
            ctx.SetMainObject(runtimeTree);
        }
    }
}
