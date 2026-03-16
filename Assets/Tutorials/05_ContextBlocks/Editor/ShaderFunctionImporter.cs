using System.IO;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace GraphToolkitTutorials.ContextBlocks
{
    /// <summary>
    /// 着色器函数图形资产导入器
    /// 负责导入.shaderfunc文件
    /// </summary>
    [ScriptedImporter(1, "shaderfunc")]
    internal class ShaderFunctionImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            // 加载图形
            var graph = GraphDatabase.LoadGraphForImporter<ShaderFunctionGraph>(ctx.assetPath);

            if (graph == null)
            {
                Debug.LogError($"Failed to load shader function graph from {ctx.assetPath}");
                return;
            }

            // 查找函数上下文节点
            var functionContext = graph.FindFunctionContext();
            if (functionContext == null)
            {
                Debug.LogWarning($"No function context found in {ctx.assetPath}");
            }
            else
            {
                // 评估函数
                var result = functionContext.EvaluateVector(null, graph);
                Debug.Log($"Function evaluated: {result}");
            }

            // 创建函数数据资产
            var functionData = ScriptableObject.CreateInstance<ShaderFunctionData>();
            functionData.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
            functionData.graph = graph;

            if (functionContext != null)
            {
                functionData.functionName = functionContext.Name;
                functionData.blockCount = functionContext.GetBlocks().Count;
            }

            // 添加到资产
            ctx.AddObjectToAsset("main", functionData);
            ctx.SetMainObject(functionData);

            // 添加图形本身作为子资产
            ctx.AddObjectToAsset("graph", graph);

            Debug.Log($"Shader function imported: {functionData.functionName}, {functionData.blockCount} blocks");
        }
    }

    /// <summary>
    /// 着色器函数数据
    /// 存储函数图形的信息
    /// </summary>
    [System.Serializable]
    public class ShaderFunctionData : ScriptableObject
    {
        public string functionName;
        public int blockCount;
    }
}
