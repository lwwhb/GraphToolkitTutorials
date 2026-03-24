using System;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace GraphToolkitTutorials.VariablesSubgraphs
{
    /// <summary>
    /// 常量材质子图 — 封装固定 PBR 参数包，所有输出值由常量节点驱动
    ///
    /// 变量系统教学要点（Output 变量）：
    ///   Output 变量 — 暴露给父图，成为父图中子图节点的输出端口
    ///   在子图内部，Output 变量节点有一个输入端口，接收常量节点的值
    ///   父图通过 ISubgraphNode 接口读取这些输出端口的值
    ///
    /// 默认示例内容（首次创建时自动生成）：
    ///   ColorConstant(#FFD700) ──→ VariableNode[Output: BaseColor]
    ///   FloatConstant(0.9)     ──→ VariableNode[Output: Metallic]
    ///   FloatConstant(0.7)     ──→ VariableNode[Output: Smoothness]
    ///   ColorConstant(黑色)    ──→ VariableNode[Output: EmissionColor]
    /// </summary>
    [Graph("matconstsubgraph", GraphOptions.SupportsSubgraphs)]
    [Subgraph(typeof(MaterialGraph))]
    [Serializable]
    public class MaterialConstSubgraph : MaterialGraph
    {
        [MenuItem("Assets/Create/Graph Toolkit/Material Const Subgraph", false)]
        static void CreateAssetFile()
        {
            GraphDatabase.PromptInProjectBrowserToCreateNewAsset<MaterialConstSubgraph>();
        }

        protected override bool NeedsOutputNode() => false;

        public override void OnEnable()
        {
            base.OnEnable();
            if (VariableCount == 0)
                EditorApplication.delayCall += EnsureExampleContent;
        }

        private void EnsureExampleContent()
        {
            EditorApplication.delayCall -= EnsureExampleContent;
            if (this == null || VariableCount > 0)
                return;

            UndoBeginRecordGraph("Initialize Const Subgraph");

            // --- Output 变量：暴露给父图，成为子图节点的输出端口 ---
            var varBaseColor     = CreateVariable<Color>("BaseColor",     new Color(1f, 0.84f, 0f), VariableKind.Output);
            var varMetallic      = CreateVariable<float>("Metallic",      0.9f,                     VariableKind.Output);
            var varSmoothness    = CreateVariable<float>("Smoothness",    0.7f,                     VariableKind.Output);
            var varEmissionColor = CreateVariable<Color>("EmissionColor", Color.black,              VariableKind.Output);

            // --- 常量节点：为每个 Output 变量提供固定值 ---
            var constBaseColor     = CreateConstantNode<Color>(new Vector2(-300, 0),   new Color(1f, 0.84f, 0f));
            var constMetallic      = CreateConstantNode<float>(new Vector2(-300, 120), 0.9f);
            var constSmoothness    = CreateConstantNode<float>(new Vector2(-300, 200), 0.7f);
            var constEmissionColor = CreateConstantNode<Color>(new Vector2(-300, 300), Color.black);

            // --- Output 变量节点 ---
            var nodeBaseColor     = AddVariableNode(varBaseColor,     new Vector2(0, 0));
            var nodeMetallic      = AddVariableNode(varMetallic,      new Vector2(0, 120));
            var nodeSmoothness    = AddVariableNode(varSmoothness,    new Vector2(0, 200));
            var nodeEmissionColor = AddVariableNode(varEmissionColor, new Vector2(0, 300));

            // --- 连线：常量 → Output 变量节点输入端口 ---
            Connect(constBaseColor.GetOutputPort(0),     nodeBaseColor.GetInputPort(0));
            Connect(constMetallic.GetOutputPort(0),      nodeMetallic.GetInputPort(0));
            Connect(constSmoothness.GetOutputPort(0),    nodeSmoothness.GetInputPort(0));
            Connect(constEmissionColor.GetOutputPort(0), nodeEmissionColor.GetInputPort(0));

            UndoEndRecordGraph();
            GraphDatabase.SaveGraph(this);
        }
    }
}
