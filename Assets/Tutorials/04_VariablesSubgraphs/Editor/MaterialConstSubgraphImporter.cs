using System.IO;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace GraphToolkitTutorials.VariablesSubgraphs
{
    /// <summary>
    /// 常量子图导入器 — 处理 .matconstsubgraph 文件
    /// 子图不生成 Material 资产，只将文件注册为可被父图引用的子图资源
    /// </summary>
    [ScriptedImporter(1, "matconstsubgraph")]
    public class MaterialConstSubgraphImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            GraphDatabase.LoadGraphForImporter<MaterialConstSubgraph>(ctx.assetPath);

            var asset = ScriptableObject.CreateInstance<MaterialSubgraphAsset>();
            asset.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
            ctx.AddObjectToAsset("main", asset);
            ctx.SetMainObject(asset);
        }
    }
}
