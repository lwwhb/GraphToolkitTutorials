using UnityEditor.AssetImporters;
using UnityEngine;
using Unity.GraphToolkit.Editor;

namespace GraphToolkitTutorials.RenderGraphAPI
{
    /// <summary>
    /// 新RenderGraph资产导入器
    /// </summary>
    [ScriptedImporter(1, "newrendergraph")]
    internal class NewRenderGraphImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            // 加载图形
            var graph = GraphDatabase.LoadGraphForImporter<NewRenderGraph>(ctx.assetPath);
            if (graph == null)
            {
                Debug.LogError($"Failed to load graph from {ctx.assetPath}");
                return;
            }

            // 创建运行时图形
            var runtime = graph.CreateRuntime();
            if (runtime == null)
            {
                Debug.LogError($"Failed to create runtime graph from {ctx.assetPath}");
                return;
            }

            // 设置资产名称
            runtime.name = System.IO.Path.GetFileNameWithoutExtension(ctx.assetPath);

            // 添加到资产
            ctx.AddObjectToAsset("main", runtime);
            ctx.SetMainObject(runtime);

            Debug.Log($"Imported NewRenderGraph: {runtime.name}");
        }
    }
}
