using System.Collections.Generic;
using UnityEngine;

namespace GraphToolkitTutorials.AbilitySystem.Runtime
{
    /// <summary>
    /// 技能运行时图 — ScriptableObject 主资产，由 AbilityImporter 生成。
    /// 包含序列化的运行时节点列表，不依赖任何 Editor API。
    /// </summary>
    public class AbilityRuntimeGraph : ScriptableObject
    {
        [SerializeReference]
        public List<AbilityRuntimeNode> nodes = new List<AbilityRuntimeNode>();

        /// <summary>按事件名查找触发节点的索引，未找到返回 -1。</summary>
        public int FindTrigger(string eventName)
        {
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i] is OnEventRuntimeNode trigger && trigger.eventName == eventName)
                    return i;
            return -1;
        }
    }
}
