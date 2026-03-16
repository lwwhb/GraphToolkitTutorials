using System.Collections.Generic;

namespace GraphToolkitTutorials.DialogueSystem.Runtime
{
    /// <summary>
    /// 对话变量存储
    /// 存储对话过程中的变量
    /// </summary>
    public class DialogueVariables
    {
        private Dictionary<string, string> m_Variables = new Dictionary<string, string>();

        /// <summary>
        /// 设置变量
        /// </summary>
        public void SetVariable(string key, string value)
        {
            m_Variables[key] = value;
        }

        /// <summary>
        /// 获取变量
        /// </summary>
        public string GetVariable(string key, string defaultValue = "")
        {
            if (m_Variables.TryGetValue(key, out var value))
            {
                return value;
            }
            return defaultValue;
        }

        /// <summary>
        /// 检查变量是否存在
        /// </summary>
        public bool HasVariable(string key)
        {
            return m_Variables.ContainsKey(key);
        }

        /// <summary>
        /// 移除变量
        /// </summary>
        public void RemoveVariable(string key)
        {
            m_Variables.Remove(key);
        }

        /// <summary>
        /// 清空所有变量
        /// </summary>
        public void Clear()
        {
            m_Variables.Clear();
        }

        /// <summary>
        /// 获取所有变量键
        /// </summary>
        public IEnumerable<string> GetAllKeys()
        {
            return m_Variables.Keys;
        }
    }
}
