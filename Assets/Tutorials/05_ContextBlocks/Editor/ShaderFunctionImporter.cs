using System.IO;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace GraphToolkitTutorials.ContextBlocks
{
    /// <summary>
    /// 着色器函数图形资产导入器
    /// 负责导入 .shaderfunc 文件，生成 ShaderFunctionData 资产。
    ///
    /// 注意：Graph 不是 UnityEngine.Object，不能作为子资产添加到 ctx。
    /// 只需将 ShaderFunctionData（ScriptableObject）设为主对象即可。
    /// </summary>
    [ScriptedImporter(1, "shaderfunc")]
    internal class ShaderFunctionImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var graph = GraphDatabase.LoadGraphForImporter<ShaderFunctionGraph>(ctx.assetPath);

            var functionData = ScriptableObject.CreateInstance<ShaderFunctionData>();
            functionData.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
            functionData.functionName = functionData.name;

            if (graph != null)
            {
                var functionContext = graph.FindFunctionContext();
                if (functionContext != null)
                    functionData.blockCount = functionContext.BlockCount;

                foreach (var node in graph.GetNodes())
                {
                    if (node is OutputNode outputNode)
                    {
                        outputNode.Evaluate(graph);
                        break;
                    }
                }
            }

            ctx.AddObjectToAsset("main", functionData);
            ctx.SetMainObject(functionData);
        }
    }
}
