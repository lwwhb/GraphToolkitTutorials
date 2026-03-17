using UnityEditor.AssetImporters;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using UnityEditor;

namespace GraphToolkitTutorials.HelloGraph
{
    [ScriptedImporter(1, "calc")]
    public class CalculatorGraphImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            //设置图标
            Texture2D icon =
                EditorGUIUtility.Load("Assets/Tutorials/01_HelloGraph/Editor/Icons/calculator.png") as Texture2D;
            if (icon != null)
            {
                ctx.AddObjectToAsset("calculator", icon);
                ctx.SetMainObject(icon);
            }
            
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
            
            Debug.Log($"Calculator graph evaluated: {result}");
        }
    }
}