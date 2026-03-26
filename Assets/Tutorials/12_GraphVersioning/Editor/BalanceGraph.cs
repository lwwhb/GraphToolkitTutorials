using System;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace GraphToolkitTutorials.GraphVersioning
{
    /// <summary>
    /// 游戏平衡配置图形
    ///
    /// 版本控制核心教学：
    ///   - m_SchemaVersion 记录图形资产的 schema 版本号
    ///   - 当节点结构升级（新增/删除字段）时，递增 CurrentSchemaVersion
    ///   - BalanceGraphImporter 在导入时检测版本差异并执行迁移逻辑
    ///
    /// V1（schema 1）：WeaponStatsNode 没有 Range 字段
    /// V2（schema 2）：WeaponStatsNode 新增 Range 字段，迁移策略：BaseDamage >= 30 时设 Range = 2.5
    /// </summary>
    [Graph("balancegraph", GraphOptions.Default)]
    [Serializable]
    public class BalanceGraph : Graph
    {
        /// <summary>当前最新 schema 版本</summary>
        public const int CurrentSchemaVersion = 2;

        /// <summary>
        /// 本文件保存时的 schema 版本。
        /// 0 表示旧格式（无版本字段），通常视同 V1。
        /// </summary>
        [SerializeField] private int m_SchemaVersion = 0;

        /// <summary>向 Importer 暴露 schema 版本</summary>
        public int SchemaVersion => m_SchemaVersion;

        [MenuItem("Assets/Create/Graph Toolkit/Balance Graph")]
        static void CreateGraphAssetFile()
        {
            GraphDatabase.PromptInProjectBrowserToCreateNewAsset<BalanceGraph>();
        }

        public override void OnEnable()
        {
            base.OnEnable();
            // 新建图形时自动填充示例节点
            if (NodeCount == 0)
                EditorApplication.delayCall += EnsureExampleContent;
        }

        private void EnsureExampleContent()
        {
            EditorApplication.delayCall -= EnsureExampleContent;
            if (this == null || NodeCount != 0) return;

            UndoBeginRecordGraph("Add Balance Graph Example Nodes");

            // 将版本标记为当前最新版本
            m_SchemaVersion = CurrentSchemaVersion;

            // 添加示例节点
            AddNode(new EnemyStatsNode());   // 默认值：Goblin, 100 HP
            AddNode(new WeaponStatsNode());   // 默认值：Sword, 20 DMG
            AddNode(new BalanceOutputNode()); // 标记节点

            UndoEndRecordGraph();
            GraphDatabase.SaveGraph(this);
        }
    }
}
