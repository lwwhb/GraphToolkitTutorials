using System.IO;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace GraphToolkitTutorials.HelloGraph
{
    /// <summary>
    /// 计算器图形资产导入器
    /// 负责导入.calc文件并评估计算结果
    /// </summary>
    [ScriptedImporter(1, "calc")]
    internal class CalculatorImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            // 加载图形
            var graph = GraphDatabase.LoadGraphForImporter<CalculatorGraph>(ctx.assetPath);

            if (graph == null)
            {
                Debug.LogError($"Failed to load calculator graph from {ctx.assetPath}");
                return;
            }

            // 查找输出节点并评估结果
            float result = 0f;
            OutputNode outputNode = null;

            foreach (var node in graph.GetNodes())
            {
                if (node is OutputNode output)
                {
                    outputNode = output;
                    // 评估输出节点（这会递归评估整个图形）
                    result = output.Evaluate(null, graph);
                    break;
                }
            }

            // 创建一个ScriptableObject来存储结果
            var resultAsset = ScriptableObject.CreateInstance<CalculatorResult>();
            resultAsset.result = result;
            resultAsset.graphAsset = graph;

            // 添加到资产
            ctx.AddObjectToAsset("main", resultAsset);
            ctx.SetMainObject(resultAsset);

            // 添加图形本身作为子资产
            ctx.AddObjectToAsset("graph", graph);

            Debug.Log($"Calculator graph evaluated: {result}");
        }
    }

    /// <summary>
    /// 计算器结果资产
    /// 存储图形评估的结果
    /// </summary>
    [System.Serializable]
    public class CalculatorResult : ScriptableObject
    {
        public float result;
    }
}
