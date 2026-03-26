using System;
using System.Collections.Generic;
using UnityEngine;

namespace GraphToolkitTutorials.RenderGraphAPI.Runtime
{
    /// <summary>
    /// 运行时渲染图 — 存储序列化的节点列表和起始节点索引。
    ///
    /// 教学要点：
    ///   • [SerializeReference] 支持多态列表（is 类型检查才能正确匹配子类）
    ///   • [SerializeField] 会导致多态列表 is 检查始终失败（只序列化基类字段）
    ///   • RenderPassDataRuntimeNode.operations 同理也需要 [SerializeReference]
    /// </summary>
    public class NewRenderGraphRuntime : ScriptableObject
    {
        public int startNodeIndex = -1;

        [SerializeReference]
        public List<RenderGraphRuntimeNode> nodes = new List<RenderGraphRuntimeNode>();

        public RenderGraphRuntimeNode GetNode(int index)
        {
            if (index >= 0 && index < nodes.Count) return nodes[index];
            return null;
        }

        public RenderGraphRuntimeNode GetStartNode() => GetNode(startNodeIndex);
    }

    // ─── 节点基类 ────────────────────────────────────────────────────────────────

    [Serializable]
    public abstract class RenderGraphRuntimeNode
    {
        public int nextNodeIndex = -1;
    }

    [Serializable]
    public class PipelineStartRuntimeNode : RenderGraphRuntimeNode { }

    [Serializable]
    public class PipelineEndRuntimeNode : RenderGraphRuntimeNode { }

    /// <summary>
    /// 渲染通道运行时节点 — 包含有序的 PassOperation 列表。
    ///
    /// 教学要点（T11 核心设计）：
    ///   • 对应 Editor 侧的 RenderPassNode（ContextNode）
    ///   • [SerializeReference] 支持 PassOperation 多态序列化
    ///   • 列表中操作按顺序执行（Clear → DrawOpaque → DrawTransparent）
    ///   • BlitOperation 因读写约束需在独立双 Pass 中执行（与 T10 一致）
    /// </summary>
    [Serializable]
    public class RenderPassDataRuntimeNode : RenderGraphRuntimeNode
    {
        [SerializeReference]
        public List<PassOperation> operations = new List<PassOperation>();
    }

    // ─── PassOperation 层次 ──────────────────────────────────────────────────────
    //
    // 对比 Tutorial 10：T10 每种操作对应独立的运行时节点类型
    //                   T11 统一用 PassOperation 子类，多个操作共存于一个 RenderPassDataRuntimeNode

    [Serializable]
    public abstract class PassOperation { }

    [Serializable]
    public class ClearOperation : PassOperation
    {
        public Color clearColor = Color.black;
        public bool  clearDepth = true;
    }

    [Serializable]
    public class DrawOpaqueOperation : PassOperation
    {
        public LayerMask layerMask = ~0;
    }

    [Serializable]
    public class DrawTransparentOperation : PassOperation
    {
        public LayerMask layerMask = ~0;
    }

    [Serializable]
    public class BlitOperation : PassOperation
    {
        public Material material;
    }
}
