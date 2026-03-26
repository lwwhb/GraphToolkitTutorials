using UnityEngine;

namespace GraphToolkitTutorials.RenderGraphAPI.Runtime
{
    /// <summary>
    /// 诊断 MonoBehaviour — 在 OnGUI 中显示运行时图的节点列表和操作列表。
    ///
    /// 使用方法：
    ///   1. 在场景中任意 GameObject 上挂载此脚本
    ///   2. 将 .newrendergraph 资产（生成的 NewRenderGraphRuntime）拖入 Graph Asset 字段
    ///   3. Play Mode 下屏幕左上角显示节点信息
    ///
    /// 教学意义：
    ///   验证 Importer 正确序列化了 ContextNode + BlockNode 的多态 operations 列表
    /// </summary>
    public class NewRenderGraphTester : MonoBehaviour
    {
        [SerializeField] private NewRenderGraphRuntime m_GraphAsset;

        private void OnGUI()
        {
            if (m_GraphAsset == null)
            {
                GUI.Label(new Rect(10, 10, 400, 20), "[NewRenderGraphTester] No graph asset assigned.");
                return;
            }

            var style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize  = 12,
            };

            float y = 10f;
            float w = 500f;
            float lineH = 18f;

            GUI.Box(new Rect(10, y, w, 30), $"NewRenderGraph: {m_GraphAsset.name}", style);
            y += 30;

            GUI.Label(new Rect(10, y, w, lineH),
                $"  nodes={m_GraphAsset.nodes.Count}  startIndex={m_GraphAsset.startNodeIndex}");
            y += lineH;

            for (int i = 0; i < m_GraphAsset.nodes.Count; i++)
            {
                var node = m_GraphAsset.nodes[i];
                string nodeDesc = node == null ? "null" : node.GetType().Name;
                string suffix   = i == m_GraphAsset.startNodeIndex ? " ← START" : "";
                GUI.Label(new Rect(10, y, w, lineH),
                    $"  [{i}] {nodeDesc}  next={node?.nextNodeIndex}{suffix}");
                y += lineH;

                if (node is RenderPassDataRuntimeNode pass)
                {
                    foreach (var op in pass.operations)
                    {
                        string opDesc = op?.GetType().Name ?? "null";
                        if (op is ClearOperation clear)
                            opDesc += $"(color={clear.clearColor}, depth={clear.clearDepth})";
                        else if (op is BlitOperation blit)
                            opDesc += $"(mat={(blit.material != null ? blit.material.name : "none")})";
                        GUI.Label(new Rect(30, y, w - 20, lineH), $"    op: {opDesc}");
                        y += lineH;
                    }
                }
            }
        }
    }
}
