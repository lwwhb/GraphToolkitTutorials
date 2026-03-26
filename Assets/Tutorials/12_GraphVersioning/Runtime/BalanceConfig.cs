using System;
using UnityEngine;

namespace GraphToolkitTutorials.GraphVersioning.Runtime
{
    /// <summary>
    /// 敌人配置数据
    /// </summary>
    [Serializable]
    public class EnemyConfig
    {
        public string enemyName;
        public int health;
        public int attack;
        public int defense;
    }

    /// <summary>
    /// 武器配置数据
    /// </summary>
    [Serializable]
    public class WeaponConfig
    {
        public string weaponName;
        public int baseDamage;
        public float attackSpeed;
        /// <summary>
        /// V2 新增字段：攻击范围。V1 资产通过迁移逻辑自动补全。
        /// </summary>
        public float range;
    }

    /// <summary>
    /// 游戏数值平衡配置表
    /// 由 BalanceGraphImporter 从 .balancegraph 文件生成。
    /// </summary>
    public class BalanceConfig : ScriptableObject
    {
        [SerializeField] public EnemyConfig[] enemies = Array.Empty<EnemyConfig>();
        [SerializeField] public WeaponConfig[] weapons = Array.Empty<WeaponConfig>();
    }
}
