using System.IO;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace GraphToolkitTutorials.RenderGraphBasics
{
    /// <summary>
    /// 将 .rendergraph 文件导入为 RenderGraphRuntime 主资产。
    ///
    /// 注意：Graph 不是 UnityEngine.Object，不能 AddObjectToAsset(graph)。
    /// 转换逻辑全部在 Importer 中完成，Runtime 程序集不引用 Editor 程序集。
    /// </summary>
    [ScriptedImporter(1, "rendergraph")]
    internal class RenderGraphImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var editorGraph = GraphDatabase.LoadGraphForImporter<RenderGraph>(ctx.assetPath);

            if (editorGraph == null)
            {
                Debug.LogError($"[RenderGraphBasics] Failed to load render graph from {ctx.assetPath}");
                return;
            }

            if (!editorGraph.Validate(out string errorMessage))
                Debug.LogWarning($"[RenderGraphBasics] {ctx.assetPath}: {errorMessage}");

            var runtimeGraph = editorGraph.CreateRuntimeGraph();
            runtimeGraph.name = Path.GetFileNameWithoutExtension(ctx.assetPath);

            ctx.AddObjectToAsset("main", runtimeGraph);
            ctx.SetMainObject(runtimeGraph);

            Debug.Log($"[RenderGraphBasics] Imported '{runtimeGraph.name}': {runtimeGraph.nodes.Count} nodes, start={runtimeGraph.startNodeIndex}");
        }
    }
}
