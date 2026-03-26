using System.IO;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace GraphToolkitTutorials.GraphDrivenURP
{
    /// <summary>
    /// 将 .urpgraph 文件导入为 URPGraphRuntime 主资产。
    ///
    /// 注意：Graph 不是 UnityEngine.Object，不能 AddObjectToAsset(graph)。
    /// 转换逻辑全部在 Importer 中完成，Runtime 程序集不引用 Editor 程序集。
    /// </summary>
    [ScriptedImporter(1, "urpgraph")]
    internal class URPGraphImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var graph = GraphDatabase.LoadGraphForImporter<URPGraph>(ctx.assetPath);

            Runtime.URPGraphRuntime runtimeGraph;
            if (graph != null)
            {
                if (!graph.Validate(out string errorMessage))
                    Debug.LogWarning($"[URPGraph] {ctx.assetPath}: {errorMessage}");

                runtimeGraph = graph.CreateRuntimeGraph();
                Debug.Log($"[URPGraph] Imported '{Path.GetFileNameWithoutExtension(ctx.assetPath)}': " +
                          $"{runtimeGraph.nodes.Count} nodes, start={runtimeGraph.startNodeIndex}");
            }
            else
            {
                runtimeGraph = ScriptableObject.CreateInstance<Runtime.URPGraphRuntime>();
            }

            runtimeGraph.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
            ctx.AddObjectToAsset("main", runtimeGraph);
            ctx.SetMainObject(runtimeGraph);
        }
    }
}
