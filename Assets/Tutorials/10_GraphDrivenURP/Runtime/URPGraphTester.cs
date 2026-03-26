using UnityEngine;

namespace GraphToolkitTutorials.GraphDrivenURP.Runtime
{
    /// <summary>
    /// 诊断 MonoBehaviour：在 OnGUI 显示 URPGraphRuntime 的节点状态。
    ///
    /// 使用方法：
    ///   1. 将此组件挂载到场景中任意 GameObject
    ///   2. 将 .urpgraph 资产（URPGraphRuntime）拖入 URP Graph 字段
    ///   3. Play 模式下左上角显示节点列表与起始节点索引
    /// </summary>
    public class URPGraphTester : MonoBehaviour
    {
        [SerializeField] private URPGraphRuntime m_URPGraph;

        private void OnGUI()
        {
            if (m_URPGraph == null)
            {
                GUI.Label(new Rect(10, 10, 400, 20), "[URPGraphTester] No URP Graph assigned.");
                return;
            }

            float y = 10f;
            GUI.Label(new Rect(10, y, 500, 20),
                $"[URPGraphTester] Nodes: {m_URPGraph.nodes.Count}  startIndex: {m_URPGraph.startNodeIndex}");
            y += 22f;

            for (int i = 0; i < m_URPGraph.nodes.Count; i++)
            {
                var node = m_URPGraph.nodes[i];
                string label = node != null
                    ? $"  [{i}] {node.GetType().Name}  → next: {node.nextNodeIndex}"
                    : $"  [{i}] null";
                GUI.Label(new Rect(10, y, 500, 20), label);
                y += 18f;
            }
        }
    }
}
