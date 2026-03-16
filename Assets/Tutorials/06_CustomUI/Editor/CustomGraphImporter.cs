using System.IO;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace GraphToolkitTutorials.CustomUI
{
    /// <summary>
    /// 自定义图形资产导入器
    /// </summary>
    [ScriptedImporter(1, "customgraph")]
    internal class CustomGraphImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            // 加载图形
            var graph = GraphDatabase.LoadGraphForImporter<CustomGraph>(ctx.assetPath);

            if (graph == null)
            {
                Debug.LogError($"Failed to load custom graph from {ctx.assetPath}");
                return;
            }

            // 创建图形数据资产
            var graphData = ScriptableObject.CreateInstance<CustomGraphData>();
            graphData.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
            graphData.graph = graph;
            graphData.nodeCount = graph.Nodes.Count;
            graphData.connectionCount = graph.Connections.Count;

            // 添加到资产
            ctx.AddObjectToAsset("main", graphData);
            ctx.SetMainObject(graphData);

            // 添加图形本身作为子资产
            ctx.AddObjectToAsset("graph", graph);

            Debug.Log($"Custom graph imported: {graphData.nodeCount} nodes, {graphData.connectionCount} connections");
        }
    }

    /// <summary>
    /// 自定义图形数据
    /// </summary>
    [System.Serializable]
    public class CustomGraphData : ScriptableObject
    {
        public int nodeCount;
        public int connectionCount;
    }
}
