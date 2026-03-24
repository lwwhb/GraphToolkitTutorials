using System;

namespace GraphToolkitTutorials.AbilitySystem.Runtime
{
    /// <summary>
    /// 技能运行时节点基类。
    /// 所有节点使用 [SerializeReference] 多态序列化，必须标注 [Serializable]。
    /// 连接关系用 int 索引（-1 表示未连接），不依赖任何 Unity Editor API。
    /// </summary>
    [Serializable]
    public abstract class AbilityRuntimeNode { }
}
