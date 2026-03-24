using System;
using System.IO;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GraphToolkitTutorials.VariablesSubgraphs
{
    /// <summary>
    /// 材质主图形 — 生成 URP Material 资源
    /// - [Graph] 注册文件扩展名 .matgraph
    /// - GraphOptions.SupportsSubgraphs 启用子图支持：框架为 [Subgraph(typeof(MaterialGraph))] 的图自动生成子图节点
    /// </summary>
    [Graph("matgraph", GraphOptions.SupportsSubgraphs)]
    [Serializable]
    public class MaterialGraph : Graph
    {
        [MenuItem("Assets/Create/Graph Toolkit/MaterialGraph", false)]
        static void CreateGraphAssetFile()
        {
            GraphDatabase.PromptInProjectBrowserToCreateNewAsset<MaterialGraph>();
        }

        public override void OnEnable()
        {
            base.OnEnable();
            if (NeedsOutputNode() && FindOutputNode() == null)
                EditorApplication.delayCall += EnsureOutputNode;
            else if (NeedsOutputNode() && NodeCount == 1)
                EditorApplication.delayCall += EnsureExampleMainGraph;
        }

        /// <summary>
        /// 子类可覆写此方法控制是否自动添加 MaterialOutputNode。
        /// MaterialSubgraph 返回 false，无需输出节点。
        /// </summary>
        protected virtual bool NeedsOutputNode() => true;

        private void EnsureOutputNode()
        {
            EditorApplication.delayCall -= EnsureOutputNode;
            // 对象可能已被销毁，需先判空
            if (this == null || FindOutputNode() != null)
                return;
            UndoBeginRecordGraph("Add Material Output Node");
            AddNode(new MaterialOutputNode());
            UndoEndRecordGraph();
            GraphDatabase.SaveGraph(this);
        }

        /// <summary>
        /// 示例主图自动连线：
        /// 当主图只有 MaterialOutputNode 时，自动寻找同目录下的 .matsubgraph 文件，
        /// 添加子图节点并将 Output 变量端口连接到 MaterialOutputNode 对应端口。
        ///
        /// ISubgraphNode 教学要点：
        ///   框架为 MaterialSubgraph（标注了 [Subgraph(typeof(MaterialGraph))]）自动生成子图节点。
        ///   该节点实现 ISubgraphNode 接口，暴露子图的 Output 变量为输出端口、Input 变量为输入端口。
        ///   EvaluateColorPort / EvaluateFloatPort 通过 (node is ISubgraphNode) 检测并递归进入子图求值。
        /// </summary>
        private void EnsureExampleMainGraph()
        {
            EditorApplication.delayCall -= EnsureExampleMainGraph;
            if (this == null || NodeCount != 1) return;

            // 找到同目录下的 .matvarsubgraph 或 .matconstsubgraph 文件
            var graphPath = GraphDatabase.GetGraphAssetPath(this);
            if (string.IsNullOrEmpty(graphPath)) return;
            var dir = Path.GetDirectoryName(graphPath);
            var subgraphFiles = Directory.GetFiles(dir, "*.matvarsubgraph");
            if (subgraphFiles.Length == 0)
                subgraphFiles = Directory.GetFiles(dir, "*.matconstsubgraph");
            if (subgraphFiles.Length == 0) return;

            MaterialGraph subgraph = GraphDatabase.LoadGraph<MaterialVariableSubgraph>(subgraphFiles[0]);
            if (subgraph == null)
                subgraph = GraphDatabase.LoadGraph<MaterialConstSubgraph>(subgraphFiles[0]);
            if (subgraph == null) return;

            UndoBeginRecordGraph("Build Example Material Graph");

            // 添加子图节点（框架自动生成，实现 ISubgraphNode）
            // ISubgraphNode.GetSubgraph() 返回 subgraph 实例，端口由子图 Output 变量自动生成
            var subgraphNode = AddSubgraphNode(subgraph, new Vector2(-350, 80));
            var subgraphINode = subgraphNode as INode;
            if (subgraphINode == null) { UndoEndRecordGraph(); return; }

            // 找到 MaterialOutputNode
            var outputNode = FindOutputNode();

            // 连线：子图节点输出端口 → MaterialOutputNode 输入端口
            // 子图节点端口的 Name 是变量 GUID，DisplayName 才是变量名
            // 因此用 GetOutputPortByDisplayName 辅助方法按 DisplayName 查找
            var baseColorOut   = GetPortByDisplayName(subgraphINode.GetOutputPorts(), "BaseColor");
            var metallicOut    = GetPortByDisplayName(subgraphINode.GetOutputPorts(), "Metallic");
            var smoothnessOut  = GetPortByDisplayName(subgraphINode.GetOutputPorts(), "Smoothness");
            var emissionOut    = GetPortByDisplayName(subgraphINode.GetOutputPorts(), "EmissionColor");

            if (outputNode != null)
            {
                var outputINode = outputNode as INode;
                if (baseColorOut  != null) Connect(baseColorOut,  outputINode.GetInputPortByName("Base Color"));
                if (metallicOut   != null) Connect(metallicOut,   outputINode.GetInputPortByName("Metallic"));
                if (smoothnessOut != null) Connect(smoothnessOut, outputINode.GetInputPortByName("Smoothness"));
                if (emissionOut   != null) Connect(emissionOut,   outputINode.GetInputPortByName("Emission Color"));

                // EmissionIntensity 用一个浮点常量节点提供（1.5 = 有自发光效果）
                var emissionIntensityConst = CreateConstantNode<float>(new Vector2(-350, 320), 1.5f);
                Connect(emissionIntensityConst.GetOutputPort(0), outputINode.GetInputPortByName("Emission Intensity"));
            }

            UndoEndRecordGraph();
            GraphDatabase.SaveGraph(this);
        }

        /// <summary>
        /// 按 DisplayName 在端口列表中查找端口。
        /// 框架自动生成的子图节点端口，Name 是变量 GUID，DisplayName 是变量名。
        /// </summary>
        private static IPort GetPortByDisplayName(System.Collections.Generic.IEnumerable<IPort> ports, string displayName)
        {
            foreach (var p in ports)
                if (p.DisplayName == displayName) return p;
            return null;
        }

        /// <summary>
        /// 评估颜色端口
        /// 按优先级处理：IColorNode → IConstantNode → IVariableNode → ISubgraphNode
        /// </summary>
        public Color EvaluateColorPort(IPort port)
        {
            if (port == null || port.Direction != PortDirection.Output)
                return Color.white;

            var node = FindNodeForPort(port);

            if (node is IColorNode colorNode)
                return colorNode.EvaluateColor(port, this);

            if (node is IConstantNode constantNode)
            {
                Color color = Color.white;
                constantNode.TryGetValue(out color);
                return color;
            }

            if (node is IVariableNode variableNode)
            {
                Color varColor = Color.white;
                variableNode.Variable?.TryGetDefaultValue(out varColor);
                return varColor;
            }

            // 框架自动生成的子图节点实现 ISubgraphNode
            if (node is ISubgraphNode subgraphNode)
                return EvaluateSubgraphColorPort(node, subgraphNode, port);

            return Color.white;
        }

        /// <summary>
        /// 评估浮点端口
        /// 按优先级处理：IFloatNode → IConstantNode → IVariableNode → ISubgraphNode
        /// </summary>
        public float EvaluateFloatPort(IPort port)
        {
            if (port == null || port.Direction != PortDirection.Output)
                return 0f;

            var node = FindNodeForPort(port);

            if (node is IFloatNode floatNode)
                return floatNode.EvaluateFloat(port, this);

            if (node is IConstantNode constantNode)
            {
                float value = 0f;
                constantNode.TryGetValue(out value);
                return value;
            }

            if (node is IVariableNode variableNode)
            {
                float varValue = 0f;
                variableNode.Variable?.TryGetDefaultValue(out varValue);
                return varValue;
            }

            if (node is ISubgraphNode subgraphNode)
                return EvaluateSubgraphFloatPort(node, subgraphNode, port);

            return 0f;
        }

        /// <summary>
        /// 子图颜色端口求值：
        /// 1. 将主图输入端口的值注入子图 Input 变量
        /// 2. 找到子图中对应名称的 Output IVariableNode，先尝试追溯上游连接，再读默认值
        /// </summary>
        private Color EvaluateSubgraphColorPort(INode subgraphNodeAsINode, ISubgraphNode subgraphNode, IPort outputPort)
        {
            var subgraph = subgraphNode.GetSubgraph() as MaterialGraph;
            if (subgraph == null) return Color.white;

            InjectSubgraphInputs(subgraphNodeAsINode, subgraph);

            foreach (var node in subgraph.GetNodes())
            {
                if (node is IVariableNode vn
                    && vn.Variable?.Name == outputPort.DisplayName
                    && vn.Variable.VariableKind == VariableKind.Output)
                {
                    foreach (var inPort in node.GetInputPorts())
                    {
                        var conn = subgraph.GetConnectedOutputPort(inPort);
                        if (conn != null) return subgraph.EvaluateColorPort(conn);
                    }
                    Color c = Color.white;
                    vn.Variable.TryGetDefaultValue(out c);
                    return c;
                }
            }
            return Color.white;
        }

        /// <summary>
        /// 子图浮点端口求值（同 EvaluateSubgraphColorPort）
        /// </summary>
        private float EvaluateSubgraphFloatPort(INode subgraphNodeAsINode, ISubgraphNode subgraphNode, IPort outputPort)
        {
            var subgraph = subgraphNode.GetSubgraph() as MaterialGraph;
            if (subgraph == null) return 0f;

            InjectSubgraphInputs(subgraphNodeAsINode, subgraph);

            foreach (var node in subgraph.GetNodes())
            {
                if (node is IVariableNode vn
                    && vn.Variable?.Name == outputPort.DisplayName
                    && vn.Variable.VariableKind == VariableKind.Output)
                {
                    foreach (var inPort in node.GetInputPorts())
                    {
                        var conn = subgraph.GetConnectedOutputPort(inPort);
                        if (conn != null) return subgraph.EvaluateFloatPort(conn);
                    }
                    float f = 0f;
                    vn.Variable.TryGetDefaultValue(out f);
                    return f;
                }
            }
            return 0f;
        }

        /// <summary>
        /// 将主图中连接到子图节点输入端口的值注入子图的 Input 变量。
        /// 有连线时取上游节点的计算值；无连线时取端口本身存储的默认值（Node Properties 面板里改的值）。
        /// </summary>
        private void InjectSubgraphInputs(INode subgraphNodeAsINode, MaterialGraph subgraph)
        {
            foreach (var inputPort in subgraphNodeAsINode.GetInputPorts())
            {
                var connected = GetConnectedOutputPort(inputPort);

                foreach (var variable in subgraph.GetVariables())
                {
                    if (variable.Name != inputPort.DisplayName || variable.VariableKind != VariableKind.Input)
                        continue;

                    if (variable.DataType == typeof(Color))
                    {
                        Color value = Color.white;
                        if (connected != null)
                            value = EvaluateColorPort(connected);
                        else
                            inputPort.TryGetValue(out value);
                        variable.TrySetDefaultValue(value);
                    }
                    else if (variable.DataType == typeof(float))
                    {
                        float value = 0f;
                        if (connected != null)
                            value = EvaluateFloatPort(connected);
                        else
                            inputPort.TryGetValue(out value);
                        variable.TrySetDefaultValue(value);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// 获取连接到输入端口的输出端口
        /// </summary>
        public IPort GetConnectedOutputPort(IPort inputPort)
        {
            if (inputPort == null || inputPort.Direction != PortDirection.Input)
                return null;
            return inputPort.FirstConnectedPort;
        }

        /// <summary>
        /// 根据端口查找所属节点
        /// </summary>
        public INode FindNodeForPort(IPort port)
        {
            if (port == null) return null;
            foreach (var node in GetNodes())
            {
                foreach (var p in node.GetInputPorts())
                    if (p == port) return node;
                foreach (var p in node.GetOutputPorts())
                    if (p == port) return node;
            }
            return null;
        }

        private MaterialOutputNode FindOutputNode()
        {
            foreach (var node in GetNodes())
            {
                if (node is MaterialOutputNode outputNode)
                    return outputNode;
            }
            return null;
        }

        /// <summary>
        /// 评估图形并创建真实的 URP Material 资源
        /// </summary>
        public Material CreateMaterial()
        {
            var outputNode = FindOutputNode();
            if (outputNode == null)
                return null;

            outputNode.EvaluateAll(this);

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("MaterialGraph: Cannot find URP/Lit shader. Ensure URP is installed.");
                return null;
            }

            var mat = new Material(shader);
            mat.SetColor("_BaseColor", outputNode.GetBaseColor());
            mat.SetFloat("_Metallic",   Mathf.Clamp01(outputNode.GetMetallic()));
            mat.SetFloat("_Smoothness", Mathf.Clamp01(outputNode.GetSmoothness()));

            float intensity = outputNode.GetEmissionIntensity();
            if (intensity > 0f)
            {
                mat.SetColor("_EmissionColor", outputNode.GetEmission() * intensity);
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }

            return mat;
        }
    }
}
