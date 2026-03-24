using System.Collections.Generic;
using GraphToolkitTutorials.AbilitySystem.Runtime;
using Unity.GraphToolkit.Editor;

namespace GraphToolkitTutorials.AbilitySystem
{
    /// <summary>
    /// 所有编辑器侧技能节点必须实现此接口。
    /// CreateRuntimeNode 负责创建运行时节点并解析连接索引。
    /// </summary>
    internal interface IAbilityEditorNode
    {
        AbilityRuntimeNode CreateRuntimeNode(
            List<INode> allNodes,
            Dictionary<INode, int> indexMap);
    }
}
