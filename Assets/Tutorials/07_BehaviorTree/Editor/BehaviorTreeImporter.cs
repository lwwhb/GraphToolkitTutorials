using System.IO;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace GraphToolkitTutorials.BehaviorTree
{
    /// <summary>
    /// 行为树图形资产导入器
    /// 负责导入.behaviortree文件并生成运行时行为树
    /// </summary>
    [ScriptedImporter(1, "behaviortree")]
    internal class BehaviorTreeImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            // 加载编辑器图形
            var editorGraph = GraphDatabase.LoadGraphForImporter<BehaviorTreeGraph>(ctx.assetPath);

            if (editorGraph == null)
            {
                Debug.LogError($"Failed to load behavior tree from {ctx.assetPath}");
                return;
            }

            // 验证行为树
            if (!editorGraph.Validate(out string errorMessage))
            {
                Debug.LogError($"Behavior tree validation failed: {errorMessage}");
            }

            // 创建运行时行为树
            var runtimeTree = editorGraph.CreateRuntimeTree();
            runtimeTree.name = Path.GetFileNameWithoutExtension(ctx.assetPath);

            // 添加到资产
            ctx.AddObjectToAsset("main", runtimeTree);
            ctx.SetMainObject(runtimeTree);

            // 添加编辑器图形作为子资产
            ctx.AddObjectToAsset("editor_graph", editorGraph);

            Debug.Log($"Behavior tree imported: {runtimeTree.nodes.Count} nodes");
        }
    }
}
