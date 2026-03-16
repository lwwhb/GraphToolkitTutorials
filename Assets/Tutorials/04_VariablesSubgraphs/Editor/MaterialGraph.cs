using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace GraphToolkitTutorials.VariablesSubgraphs
{
    /// <summary>
    /// 材质图形 - 演示变量和子图系统
    /// 这个图形系统展示如何使用GraphToolkit的变量和子图功能
    /// </summary>
    [Graph("matgraph", GraphOptions.None)]
    internal class MaterialGraph : Graph
    {
        /// <summary>
        /// 评估颜色端口
        /// </summary>
        public Color EvaluateColorPort(IPort port)
        {
            if (port == null || port.Direction != PortDirection.Output)
                return Color.white;

            var node = port.Node;
            if (node is IColorNode colorNode)
            {
                return colorNode.EvaluateColor(port, this);
            }

            return Color.white;
        }

        /// <summary>
        /// 评估浮点端口
        /// </summary>
        public float EvaluateFloatPort(IPort port)
        {
            if (port == null || port.Direction != PortDirection.Output)
                return 0f;

            var node = port.Node;
            if (node is IFloatNode floatNode)
            {
                return floatNode.EvaluateFloat(port, this);
            }

            return 0f;
        }

        /// <summary>
        /// 获取连接到输入端口的输出端口
        /// </summary>
        public IPort GetConnectedOutputPort(IPort inputPort)
        {
            if (inputPort == null || inputPort.Direction != PortDirection.Input)
                return null;

            foreach (var connection in Connections)
            {
                if (connection.InputPort == inputPort)
                    return connection.OutputPort;
            }

            return null;
        }

        /// <summary>
        /// 查找输出节点
        /// </summary>
        public MaterialOutputNode FindOutputNode()
        {
            foreach (var node in Nodes)
            {
                if (node is MaterialOutputNode outputNode)
                {
                    return outputNode;
                }
            }
            return null;
        }

        /// <summary>
        /// 创建材质数据
        /// </summary>
        public MaterialData CreateMaterialData()
        {
            var outputNode = FindOutputNode();
            if (outputNode == null)
            {
                Debug.LogWarning("MaterialGraph: No output node found");
                return null;
            }

            var materialData = ScriptableObject.CreateInstance<MaterialData>();
            materialData.baseColor = outputNode.EvaluateColor(null, this);
            materialData.metallic = outputNode.GetMetallic();
            materialData.smoothness = outputNode.GetSmoothness();

            return materialData;
        }
    }

    /// <summary>
    /// 材质数据
    /// 存储从图形评估得到的材质属性
    /// </summary>
    public class MaterialData : ScriptableObject
    {
        public Color baseColor = Color.white;
        public float metallic = 0f;
        public float smoothness = 0.5f;
    }
}
