using System;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace GraphToolkitTutorials.VariablesSubgraphs
{
    /// <summary>
    /// 变量材质子图 — 封装可参数化的 PBR 参数包
    /// 父图可通过子图节点的输入端口注入值，覆盖子图内部的默认值
    ///
    /// 变量系统教学要点（Input + Output 变量）：
    ///   Input  变量 — 由父图注入，成为父图中子图节点的输入端口
    ///   Output 变量 — 暴露给父图，成为父图中子图节点的输出端口
    ///   Local  变量 — 仅在子图内部使用，不对父图暴露
    ///
    /// 默认示例内容（首次创建时自动生成）：
    ///   Input  变量: Tint (Color, 默认白色) — 由父图注入的色调
    ///   Output 变量: BaseColor, Metallic, Smoothness, EmissionColor
    ///
    ///   内部连线：
    ///   VariableNode[Input: Tint] ──→ VariableNode[Output: BaseColor]
    ///   FloatConstant(0.9)        ──→ VariableNode[Output: Metallic]
    ///   FloatConstant(0.7)        ──→ VariableNode[Output: Smoothness]
    ///   ColorConstant(黑色)       ──→ VariableNode[Output: EmissionColor]
    /// </summary>
    [Graph("matvarsubgraph", GraphOptions.SupportsSubgraphs)]
    [Subgraph(typeof(MaterialGraph))]
    [Serializable]
    public class MaterialVariableSubgraph : MaterialGraph
    {
        [MenuItem("Assets/Create/Graph Toolkit/Material Variable Subgraph", false)]
        static void CreateAssetFile()
        {
            GraphDatabase.PromptInProjectBrowserToCreateNewAsset<MaterialVariableSubgraph>();
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

            UndoBeginRecordGraph("Initialize Variable Subgraph");

            // --- Input 变量：由父图注入，成为子图节点的输入端口 ---
            var varTint = CreateVariable<Color>("Tint", Color.white, VariableKind.Input);

            // --- Output 变量：暴露给父图，成为子图节点的输出端口 ---
            var varBaseColor     = CreateVariable<Color>("BaseColor",     new Color(1f, 0.84f, 0f), VariableKind.Output);
            var varMetallic      = CreateVariable<float>("Metallic",      0.9f,                     VariableKind.Output);
            var varSmoothness    = CreateVariable<float>("Smoothness",    0.7f,                     VariableKind.Output);
            var varEmissionColor = CreateVariable<Color>("EmissionColor", Color.black,              VariableKind.Output);

            // --- 节点 ---
            // Tint Input 变量节点（只有输出端口，将变量值传出）
            var nodeTint          = AddVariableNode(varTint, new Vector2(-500, 60));
            
            // Metallic / Smoothness / EmissionColor 常量节点
            var constMetallic      = CreateConstantNode<float>(new Vector2(-300, 200), 0.9f);
            var constSmoothness    = CreateConstantNode<float>(new Vector2(-300, 280), 0.7f);
            var constEmissionColor = CreateConstantNode<Color>(new Vector2(-300, 360), Color.black);

            // --- Output 变量节点 ---
            var nodeBaseColor     = AddVariableNode(varBaseColor,     new Vector2(0, 60));
            var nodeMetallic      = AddVariableNode(varMetallic,      new Vector2(0, 200));
            var nodeSmoothness    = AddVariableNode(varSmoothness,    new Vector2(0, 280));
            var nodeEmissionColor = AddVariableNode(varEmissionColor, new Vector2(0, 360));

            // --- 连线 ---
            Connect((nodeTint as INode).GetOutputPort(0), nodeBaseColor.GetInputPort(0));

            // 常量 → Output Metallic / Smoothness / EmissionColor
            Connect(constMetallic.GetOutputPort(0),      nodeMetallic.GetInputPort(0));
            Connect(constSmoothness.GetOutputPort(0),    nodeSmoothness.GetInputPort(0));
            Connect(constEmissionColor.GetOutputPort(0), nodeEmissionColor.GetInputPort(0));

            UndoEndRecordGraph();
            GraphDatabase.SaveGraph(this);
        }
    }
}
