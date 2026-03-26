using UnityEngine;

namespace GraphToolkitTutorials.RenderGraphBasics.Runtime
{
    /// <summary>
    /// 测试辅助脚本：在屏幕上显示 RenderGraphRuntime 状态，
    /// 帮助验证 GraphDrivenRendererFeature 是否正确配置。
    ///
    /// 使用方法：
    ///   1. 将此脚本挂载到场景中任意 GameObject。
    ///   2. 将 BasicRenderGraph 资产（.rendergraph 生成的 ScriptableObject）
    ///      拖入 m_RenderGraph 字段。
    ///   3. 确保 URP Renderer 已添加 GraphDrivenRendererFeature，
    ///      并在 Feature 的 Render Graph 字段指定同一资产。
    ///   4. 播放游戏，在 Frame Debugger 中查看 "Graph Driven Render" Pass。
    /// </summary>
    public class RenderGraphTester : MonoBehaviour
    {
        [SerializeField]
        private RenderGraphRuntime m_RenderGraph;

        private void Start()
        {
            if (m_RenderGraph == null)
            {
                Debug.LogWarning("[RenderGraphBasics] RenderGraphTester: m_RenderGraph 未赋值。" +
                                 "请将 BasicRenderGraph runtime 资产拖入 Inspector。");
                return;
            }

            var cameraNode = m_RenderGraph.GetCameraNode();
            Debug.Log($"[RenderGraphBasics] RenderGraphRuntime 已加载：" +
                      $"nodes={m_RenderGraph.nodes.Count}, " +
                      $"startNodeIndex={m_RenderGraph.startNodeIndex}, " +
                      $"cameraNode={(cameraNode != null ? "OK" : "NOT FOUND")}");
        }

        private void OnGUI()
        {
            const float w = 400f;
            const float h = 130f;
            var rect = new Rect(10, 10, w, h);

            string nodeInfo;
            string cameraInfo;
            if (m_RenderGraph == null)
            {
                nodeInfo   = "<color=red>未赋值</color> — 请在 Inspector 指定 RenderGraphRuntime";
                cameraInfo = "";
            }
            else
            {
                var cam = m_RenderGraph.GetCameraNode();
                nodeInfo   = $"<color=lime>OK</color>  节点数={m_RenderGraph.nodes.Count}  startIndex={m_RenderGraph.startNodeIndex}";
                cameraInfo = $"CameraNode: {(cam != null ? "<color=lime>OK</color>" : "<color=red>NOT FOUND</color>")}";
            }

            string msg =
                "<b>=== 09 RenderGraph Basics ===</b>\n" +
                $"RenderGraphRuntime: {nodeInfo}\n" +
                $"{cameraInfo}\n" +
                "\n<color=yellow>打开 Window→Analysis→Frame Debugger\n查看 \"Graph Driven Render\" Pass</color>";

            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize  = 13,
                richText  = true,
                alignment = TextAnchor.UpperLeft,
                padding   = new RectOffset(8, 8, 8, 8),
            };
            style.normal.textColor = Color.white;

            GUI.Box(rect, msg, style);
        }
    }
}
