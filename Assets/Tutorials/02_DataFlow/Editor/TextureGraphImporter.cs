using System.IO;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace GraphToolkitTutorials.DataFlow
{
    /// <summary>
    /// 纹理图形资产导入器
    /// 负责导入.texgraph文件并生成纹理资产
    /// </summary>
    [ScriptedImporter(1, "texgraph")]
    internal class TextureGraphImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            // 加载图形
            var graph = GraphDatabase.LoadGraphForImporter<TextureGraph>(ctx.assetPath);

            if (graph == null)
            {
                Debug.LogError($"Failed to load texture graph from {ctx.assetPath}");
                return;
            }

            // 查找输出节点并评估纹理
            Texture2D resultTexture = null;
            OutputNode outputNode = null;

            foreach (var node in graph.Nodes)
            {
                if (node is OutputNode output)
                {
                    outputNode = output;
                    resultTexture = output.EvaluateTexture(null, graph);
                    break;
                }
            }

            if (resultTexture == null)
            {
                Debug.LogWarning($"No output node found or texture generation failed in {ctx.assetPath}");

                // 创建一个默认纹理
                resultTexture = new Texture2D(1, 1);
                resultTexture.SetPixel(0, 0, Color.magenta);
                resultTexture.Apply();
            }

            // 设置纹理名称
            resultTexture.name = Path.GetFileNameWithoutExtension(ctx.assetPath);

            // 添加到资产
            ctx.AddObjectToAsset("texture", resultTexture);
            ctx.SetMainObject(resultTexture);

            // 添加图形本身作为子资产
            ctx.AddObjectToAsset("graph", graph);

            Debug.Log($"Texture graph evaluated: {resultTexture.width}x{resultTexture.height}");
        }
    }
}
