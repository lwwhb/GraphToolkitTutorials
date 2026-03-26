using System;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace GraphToolkitTutorials.ContextBlocks
{
    /// <summary>
    /// 着色器函数图形 - 演示ContextNode和BlockNode
    /// 类似于Shader Graph中的自定义函数节点
    /// </summary>
    [Graph("shaderfunc", GraphOptions.Default)]
    [Serializable]
    public class ShaderFunctionGraph : Graph
    {
        [MenuItem("Assets/Create/Graph Toolkit/ShaderFunctionGraph", false)]
        static void CreateGraphAssetFile()
        {
            GraphDatabase.PromptInProjectBrowserToCreateNewAsset<ShaderFunctionGraph>();
        }

        public override void OnEnable()
        {
            base.OnEnable();
            EditorApplication.delayCall += EnsureDefaultContent;
        }

        private void EnsureDefaultContent()
        {
            EditorApplication.delayCall -= EnsureDefaultContent;

            // 已有 FunctionContextNode，无需添加
            foreach (var node in GetNodes())
                if (node is FunctionContextNode)
                    return;

            AddNode(new FunctionContextNode());
            GraphDatabase.SaveGraph(this);
            // 注意：SaveGraph 触发 reimport 会销毁内部 GraphObjectImp，
            // 此行之后不能再访问 this 的任何成员。
        }

        /// <summary>
        /// 评估浮点端口（仅兜底内置常量节点）
        /// </summary>
        public float EvaluateFloatPort(IPort port)
        {
            if (port == null || port.Direction != PortDirection.Output)
                return 0f;

            // 内置常量节点（ConstantNodeModelImp）的 Output 端口可直接 TryGetValue 读值。
            float fv = 0f;
            port.TryGetValue<float>(out fv);
            return fv;
        }

        /// <summary>
        /// 评估向量端口：用户节点走 EvaluateVector，内置常量节点走 TryGetValue 兜底。
        /// </summary>
        public Vector3 EvaluateVectorPort(IPort port)
        {
            if (port == null || port.Direction != PortDirection.Output)
                return Vector3.zero;

            var node = FindNodeForPort(port);

            // FunctionContextNode 需要调用 EvaluateVector 才能触发计算，
            // 不能用 TryGetValue —— 用户定义的 output port 不存储计算结果。
            if (node is FunctionContextNode fn)
                return fn.EvaluateVector(this);

            if (node is IVariableNode variableNode)
            {
                variableNode.Variable.TryGetDefaultValue(out Vector3 value);
                return value;
            }
            if (node is IConstantNode constantNode)
            {
                constantNode.TryGetValue(out Vector3 value);
                return value;
            }

            // 内置常量节点（ConstantNodeModelImp）不在 GetNodes() 中，
            // 对其 Output 端口调用 TryGetValue 可直接读取存储的常量值。
            port.TryGetValue(out Vector3 fallback);
            return fallback;
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
        /// 查找函数上下文节点
        /// </summary>
        internal FunctionContextNode FindFunctionContext()
        {
            foreach (var node in GetNodes())
            {
                if (node is FunctionContextNode contextNode)
                    return contextNode;
            }
            return null;
        }

        private INode FindNodeForPort(IPort port)
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
    }
}
