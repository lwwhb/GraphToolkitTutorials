using System.IO;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace GraphToolkitTutorials.ExecutionFlow
{
    /// <summary>
    /// 任务图形资产导入器
    /// 负责导入.taskgraph文件并生成运行时图形资产
    /// </summary>
    [ScriptedImporter(1, "taskgraph")]
    public class TaskGraphImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            // 加载编辑器图形
            var editorGraph = GraphDatabase.LoadGraphForImporter<TaskGraph>(ctx.assetPath);

            if (editorGraph == null)
            {
                Debug.LogError($"Failed to load task graph from {ctx.assetPath}");
                return;
            }

            // 创建运行时图形
            var runtimeGraph = editorGraph.CreateRuntimeGraph();
            runtimeGraph.name = Path.GetFileNameWithoutExtension(ctx.assetPath);

            // 添加到资产
            ctx.AddObjectToAsset("main", runtimeGraph);
            ctx.SetMainObject(runtimeGraph);

            // 添加编辑器图形作为子资产
            //ctx.AddObjectToAsset("editor_graph", editorGraph);

            Debug.Log($"Task graph imported: {runtimeGraph.nodes.Count} nodes");
        }
    }
}
