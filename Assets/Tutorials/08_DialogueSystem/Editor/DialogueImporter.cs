using System.Collections.Generic;
using System.IO;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace GraphToolkitTutorials.DialogueSystem
{
    /// <summary>
    /// 将 .dialogue 文件导入为 DialogueRuntimeGraph 主资产。
    ///
    /// 注意：Graph 不是 UnityEngine.Object，不能 AddObjectToAsset(graph)。
    /// 转换逻辑全部在 Importer 中完成，Runtime 程序集不引用 Editor 程序集。
    /// </summary>
    [ScriptedImporter(1, "dialogue")]
    internal class DialogueImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var graph = GraphDatabase.LoadGraphForImporter<DialogueGraph>(ctx.assetPath);

            var runtimeGraph = ScriptableObject.CreateInstance<Runtime.DialogueRuntimeGraph>();
            runtimeGraph.name = Path.GetFileNameWithoutExtension(ctx.assetPath);

            if (graph != null)
            {
                if (!graph.Validate(out string errorMessage))
                    Debug.LogWarning($"[DialogueSystem] {ctx.assetPath}: {errorMessage}");

                var allNodes = new List<INode>(graph.GetNodes());
                for (int i = 0; i < allNodes.Count; i++)
                {
                    if (allNodes[i] is DialogueNode dn)
                    {
                        runtimeGraph.nodes.Add(dn.CreateRuntimeNode(graph));
                        if (allNodes[i] is StartDialogueNode)
                            runtimeGraph.startNodeIndex = runtimeGraph.nodes.Count - 1;
                    }
                }

                Debug.Log($"[DialogueSystem] Imported '{runtimeGraph.name}': {runtimeGraph.nodes.Count} nodes, start={runtimeGraph.startNodeIndex}");
            }

            ctx.AddObjectToAsset("main", runtimeGraph);
            ctx.SetMainObject(runtimeGraph);
        }
    }
}
