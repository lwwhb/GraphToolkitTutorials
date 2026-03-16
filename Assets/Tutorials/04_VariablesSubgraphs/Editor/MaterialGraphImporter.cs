using System.IO;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace GraphToolkitTutorials.VariablesSubgraphs
{
    /// <summary>
    /// 材质图形资产导入器
    /// 负责导入.matgraph文件并生成材质数据资产
    /// </summary>
    [ScriptedImporter(1, "matgraph")]
    internal class MaterialGraphImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            // 加载图形
            var graph = GraphDatabase.LoadGraphForImporter<MaterialGraph>(ctx.assetPath);

            if (graph == null)
            {
                Debug.LogError($"Failed to load material graph from {ctx.assetPath}");
                return;
            }

            // 创建材质数据
            var materialData = graph.CreateMaterialData();

            if (materialData == null)
            {
                Debug.LogWarning($"Failed to create material data from {ctx.assetPath}");
                materialData = ScriptableObject.CreateInstance<MaterialData>();
            }

            materialData.name = Path.GetFileNameWithoutExtension(ctx.assetPath);

            // 添加到资产
            ctx.AddObjectToAsset("main", materialData);
            ctx.SetMainObject(materialData);

            // 添加图形本身作为子资产
            ctx.AddObjectToAsset("graph", graph);

            Debug.Log($"Material graph imported: BaseColor={materialData.baseColor}, Metallic={materialData.metallic}, Smoothness={materialData.smoothness}");
        }
    }
}
