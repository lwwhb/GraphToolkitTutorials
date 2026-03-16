using System;
using UnityEngine;

namespace GraphToolkitTutorials.DialogueSystem.Runtime
{
    /// <summary>
    /// 运行时对话节点基类
    /// </summary>
    [Serializable]
    public abstract class DialogueRuntimeNode
    {
        public string nodeType;

        protected DialogueRuntimeNode()
        {
            nodeType = GetType().Name;
        }
    }

    /// <summary>
    /// 起始节点（运行时）
    /// </summary>
    [Serializable]
    public class StartNode : DialogueRuntimeNode
    {
        public int nextNodeIndex = -1;
    }

    /// <summary>
    /// 对话文本节点（运行时）
    /// </summary>
    [Serializable]
    public class DialogueTextNode : DialogueRuntimeNode
    {
        public string speakerName;
        public string dialogueText;
        public Sprite speakerPortrait;
        public int nextNodeIndex = -1;
    }

    /// <summary>
    /// 选择节点（运行时）
    /// </summary>
    [Serializable]
    public class ChoiceNode : DialogueRuntimeNode
    {
        public string[] optionTexts;
        public int[] nextNodeIndices;
    }

    /// <summary>
    /// 条件分支节点（运行时）
    /// </summary>
    [Serializable]
    public class BranchNode : DialogueRuntimeNode
    {
        public string conditionKey;
        public string expectedValue;
        public int trueNodeIndex = -1;
        public int falseNodeIndex = -1;
    }

    /// <summary>
    /// 设置变量节点（运行时）
    /// </summary>
    [Serializable]
    public class SetVariableNode : DialogueRuntimeNode
    {
        public string variableKey;
        public string variableValue;
        public int nextNodeIndex = -1;
    }

    /// <summary>
    /// 事件节点（运行时）
    /// </summary>
    [Serializable]
    public class EventNode : DialogueRuntimeNode
    {
        public string eventName;
        public string eventParameter;
        public int nextNodeIndex = -1;
    }

    /// <summary>
    /// 结束节点（运行时）
    /// </summary>
    [Serializable]
    public class EndNode : DialogueRuntimeNode
    {
    }
}
