using System.IO;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace GraphToolkitTutorials.GraphDrivenURP
{
    /// <summary>
    /// URP图形资产导入器
    /// 负责导入.urpgraph文件并生成运行时URP图形
    /// </summary>
    [ScriptedImporter(1, "urpgraph")]
    internal class URPGraphImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            // 加载编辑器图形
            var editorGraph = GraphDatabase.LoadGraphForImporter<URPGraph>(ctx.assetPath);

            if (editorGraph == null)
            {
                Debug.LogError($"Failed to load URP graph from {ctx.assetPath}");
                return;
            }

            // 验证URP图形
            if (!editorGraph.Validate(out string errorMessage))
            {
                Debug.LogError($"URP graph validation failed: {errorMessage}");
            }

            // 创建运行时URP图形
            var runtimeGraph = editorGraph.CreateRuntimeGraph();
            runtimeGraph.name = Path.GetFileNameWithoutExtension(ctx.assetPath);

            // 添加到资产
            ctx.AddObjectToAsset("main", runtimeGraph);
            ctx.SetMainObject(runtimeGraph);

            // 添加编辑器图形作为子资产
            ctx.AddObjectToAsset("editor_graph", editorGraph);

            Debug.Log($"URP graph imported: {runtimeGraph.nodes.Count} nodes");
        }
    }
}
