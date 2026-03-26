using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;
using Unity.GraphToolkit.Editor;

namespace GraphToolkitTutorials.RenderGraphAPI
{
    /// <summary>
    /// 新 RenderGraph 资产导入器。
    ///
    /// 教学要点：
    ///   • ScriptedImporter 将 .newrendergraph（文本图）转为 NewRenderGraphRuntime（ScriptableObject）
    ///   • CreateRuntime() 内部使用 ScriptableObject.CreateInstance（不能用 new）
    ///   • runtime 是 ScriptableObject → 用 ctx.AddObjectToAsset 注册，不能用 Graph（非 UnityEngine.Object）
    /// </summary>
    [ScriptedImporter(1, "newrendergraph")]
    internal class NewRenderGraphImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var graph = GraphDatabase.LoadGraphForImporter<NewRenderGraph>(ctx.assetPath);
            if (graph == null)
            {
                Debug.LogError($"[NewRenderGraph] Failed to load graph from {ctx.assetPath}");
                return;
            }

            if (!graph.Validate(out var errorMsg))
            {
                Debug.LogWarning($"[NewRenderGraph] Validation failed for '{ctx.assetPath}': {errorMsg}");
            }

            var runtime = graph.CreateRuntime();
            runtime.name = Path.GetFileNameWithoutExtension(ctx.assetPath);

            ctx.AddObjectToAsset("main", runtime);
            ctx.SetMainObject(runtime);

            Debug.Log($"[NewRenderGraph] Imported '{runtime.name}': " +
                      $"{runtime.nodes.Count} nodes, startIndex={runtime.startNodeIndex}");
        }
    }
}
