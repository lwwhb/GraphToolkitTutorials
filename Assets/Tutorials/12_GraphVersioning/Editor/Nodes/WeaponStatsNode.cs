using System;
using Unity.GraphToolkit.Editor;

namespace GraphToolkitTutorials.GraphVersioning
{
    /// <summary>
    /// 武器属性节点
    ///
    /// 版本变更示例：
    ///   V1 schema：仅有 Weapon Name / Base Damage / Attack Speed
    ///   V2 schema：新增 Range 选项
    ///
    /// 兼容性策略：
    ///   - 新选项 Range 在 V1 资产中不存在，TryGetValue 返回 false
    ///   - BalanceGraphImporter 检测 SchemaVersion &lt; 2 时应用迁移：
    ///     BaseDamage &gt;= 30 → Range = 2.5（远程武器），否则 Range = 1.0
    ///
    /// 关键教学点：
    ///   GraphToolkit 以 Option 名称字符串为键存储选项值，
    ///   新增选项不破坏旧资产；重命名选项则会丢失已有值。
    /// </summary>
    [Node("Weapon Stats", "Balance")]
    [UseWithGraph(typeof(BalanceGraph))]
    [Serializable]
    internal class WeaponStatsNode : Node, IBalanceNode
    {
        private INodeOption m_NameOption;
        private INodeOption m_BaseDamageOption;
        private INodeOption m_AttackSpeedOption;
        private INodeOption m_RangeOption;   // V2 新增

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_NameOption        = context.AddOption<string>("Weapon Name").Delayed().Build();
            m_BaseDamageOption  = context.AddOption<int>("Base Damage").Build();
            m_AttackSpeedOption = context.AddOption<float>("Attack Speed").Build();
            m_RangeOption       = context.AddOption<float>("Range").Build();   // V2
        }

        public string GetName()
        {
            string v = "Weapon";
            m_NameOption?.TryGetValue(out v);
            return v;
        }

        public int GetBaseDamage()
        {
            int v = 10;
            m_BaseDamageOption?.TryGetValue(out v);
            return v;
        }

        public float GetAttackSpeed()
        {
            float v = 1.0f;
            m_AttackSpeedOption?.TryGetValue(out v);
            return v;
        }

        /// <summary>
        /// 读取 Range 选项。
        /// V1 资产中该选项未存储，返回 false；Importer 将执行迁移逻辑。
        /// </summary>
        public bool TryGetRange(out float range)
        {
            range = 1.0f;
            return m_RangeOption != null && m_RangeOption.TryGetValue(out range);
        }
    }
}
