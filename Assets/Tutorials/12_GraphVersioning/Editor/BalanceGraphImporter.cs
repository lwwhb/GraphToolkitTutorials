using System.Collections.Generic;
using System.IO;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;
using GraphToolkitTutorials.GraphVersioning.Runtime;

namespace GraphToolkitTutorials.GraphVersioning
{
    /// <summary>
    /// 平衡配置图形导入器
    ///
    /// 版本迁移核心逻辑：
    ///
    ///   版本号通过两处共同确认：
    ///   1. [ScriptedImporter(version, "balancegraph")] — Unity 的导入器版本。
    ///      递增此值会强制 Unity 对所有 .balancegraph 资产执行完整重导入。
    ///      当迁移逻辑本身发生变化时应当递增。
    ///
    ///   2. BalanceGraph.SchemaVersion — 图形资产内部的 schema 版本。
    ///      记录该资产保存时对应的节点结构版本，用于在导入时判断是否需要迁移。
    ///
    ///   V1 → V2 迁移策略（范围字段自动补全）：
    ///     旧资产（SchemaVersion &lt; 2）中的 WeaponStatsNode 没有 Range 选项，
    ///     TryGetRange 返回 false，此时按 BaseDamage 推断范围：
    ///       BaseDamage &gt;= 30 → range = 2.5（远程武器）
    ///       BaseDamage &lt;  30 → range = 1.0（近战武器）
    /// </summary>
    [ScriptedImporter(2, "balancegraph")]
    public class BalanceGraphImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            // ── 1. 加载图形 ────────────────────────────────────────────────
            var graph = GraphDatabase.LoadGraphForImporter<BalanceGraph>(ctx.assetPath);
            if (graph == null)
            {
                Debug.LogError($"[BalanceGraph] 无法加载图形：{ctx.assetPath}");
                return;
            }

            int schemaVersion = graph.SchemaVersion;
            bool needsMigration = schemaVersion < BalanceGraph.CurrentSchemaVersion;

            if (needsMigration)
                Debug.Log($"[BalanceGraph] 检测到旧版 schema（v{schemaVersion}），执行迁移 → v{BalanceGraph.CurrentSchemaVersion}：{ctx.assetPath}");

            // ── 2. 收集节点数据 ────────────────────────────────────────────
            var enemies = new List<EnemyConfig>();
            var weapons = new List<WeaponConfig>();

            foreach (var node in graph.GetNodes())
            {
                if (node is EnemyStatsNode enemyNode)
                {
                    enemies.Add(new EnemyConfig
                    {
                        enemyName = enemyNode.GetName(),
                        health    = enemyNode.GetHealth(),
                        attack    = enemyNode.GetAttack(),
                        defense   = enemyNode.GetDefense(),
                    });
                }
                else if (node is WeaponStatsNode weaponNode)
                {
                    var config = new WeaponConfig
                    {
                        weaponName  = weaponNode.GetName(),
                        baseDamage  = weaponNode.GetBaseDamage(),
                        attackSpeed = weaponNode.GetAttackSpeed(),
                    };

                    // ── 3. V1→V2 迁移：Range 字段 ────────────────────────
                    if (weaponNode.TryGetRange(out float storedRange))
                    {
                        // V2 资产：直接使用存储值
                        config.range = storedRange;
                    }
                    else
                    {
                        // V1 资产或未填写 Range：按 BaseDamage 推断
                        config.range = config.baseDamage >= 30 ? 2.5f : 1.0f;
                        if (needsMigration)
                            Debug.Log($"[BalanceGraph] 武器 '{config.weaponName}' 迁移 Range → {config.range}");
                    }

                    weapons.Add(config);
                }
            }

            // ── 4. 生成 BalanceConfig ScriptableObject ────────────────────
            var balanceConfig = ScriptableObject.CreateInstance<BalanceConfig>();
            balanceConfig.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
            balanceConfig.enemies = enemies.ToArray();
            balanceConfig.weapons = weapons.ToArray();

            ctx.AddObjectToAsset("balance_config", balanceConfig);
            ctx.SetMainObject(balanceConfig);

            Debug.Log($"[BalanceGraph] 导入完成：{enemies.Count} 敌人，{weapons.Count} 武器（schema v{schemaVersion}）");
        }
    }
}
