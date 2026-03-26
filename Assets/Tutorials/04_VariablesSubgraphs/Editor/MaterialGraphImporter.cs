using System.IO;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace GraphToolkitTutorials.VariablesSubgraphs
{
    /// <summary>
    /// 材质图形资产导入器
    /// 将 .matgraph 文件转换为真实可用的 URP Material 资源
    /// </summary>
    [ScriptedImporter(1, "matgraph")]
    public class MaterialGraphImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var graph = GraphDatabase.LoadGraphForImporter<MaterialGraph>(ctx.assetPath);
            if (graph == null)
            {
                Debug.LogError($"Failed to load material graph from {ctx.assetPath}");
                return;
            }

            var material = graph.CreateMaterial();
            if (material == null)
            {
                Debug.LogWarning($"MaterialGraph: Failed to create material, using fallback. Path: {ctx.assetPath}");
                material = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit"));
            }

            material.name = Path.GetFileNameWithoutExtension(ctx.assetPath);

            ctx.AddObjectToAsset("main", material);
            ctx.SetMainObject(material);

            Debug.Log($"Material graph imported: {material.name} " +
                      $"BaseColor={material.GetColor("_BaseColor")}, " +
                      $"Metallic={material.GetFloat("_Metallic"):F2}, " +
                      $"Smoothness={material.GetFloat("_Smoothness"):F2}");
        }
    }
}
