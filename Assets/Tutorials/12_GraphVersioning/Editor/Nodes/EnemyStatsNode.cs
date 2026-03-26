using System;
using Unity.GraphToolkit.Editor;

namespace GraphToolkitTutorials.GraphVersioning
{
    /// <summary>
    /// 敌人属性节点
    /// 通过节点选项定义一个敌人的基础数值。
    /// 注意：此节点只有选项（Options），没有端口（Ports）——
    /// 平衡配置表是纯数据，无需连线表达逻辑关系。
    /// </summary>
    [Node("Enemy Stats", "Balance")]
    [UseWithGraph(typeof(BalanceGraph))]
    [Serializable]
    internal class EnemyStatsNode : Node, IBalanceNode
    {
        private INodeOption m_NameOption;
        private INodeOption m_HealthOption;
        private INodeOption m_AttackOption;
        private INodeOption m_DefenseOption;

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            m_NameOption    = context.AddOption<string>("Enemy Name").Delayed().Build();
            m_HealthOption  = context.AddOption<int>("Health").Build();
            m_AttackOption  = context.AddOption<int>("Attack").Build();
            m_DefenseOption = context.AddOption<int>("Defense").Build();
        }

        public string GetName()
        {
            string v = "Enemy";
            m_NameOption?.TryGetValue(out v);
            return v;
        }

        public int GetHealth()
        {
            int v = 100;
            m_HealthOption?.TryGetValue(out v);
            return v;
        }

        public int GetAttack()
        {
            int v = 10;
            m_AttackOption?.TryGetValue(out v);
            return v;
        }

        public int GetDefense()
        {
            int v = 5;
            m_DefenseOption?.TryGetValue(out v);
            return v;
        }
    }
}
