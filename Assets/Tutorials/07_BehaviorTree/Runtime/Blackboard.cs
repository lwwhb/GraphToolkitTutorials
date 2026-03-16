using System.Collections.Generic;
using UnityEngine;

namespace GraphToolkitTutorials.BehaviorTree.Runtime
{
    /// <summary>
    /// 黑板 - 存储行为树的共享数据
    /// </summary>
    public class Blackboard
    {
        private Dictionary<string, object> m_Data = new Dictionary<string, object>();

        /// <summary>
        /// 设置值
        /// </summary>
        public void SetValue<T>(string key, T value)
        {
            m_Data[key] = value;
        }

        /// <summary>
        /// 获取值
        /// </summary>
        public T GetValue<T>(string key, T defaultValue = default)
        {
            if (m_Data.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// 检查是否存在键
        /// </summary>
        public bool HasKey(string key)
        {
            return m_Data.ContainsKey(key);
        }

        /// <summary>
        /// 移除键
        /// </summary>
        public void RemoveKey(string key)
        {
            m_Data.Remove(key);
        }

        /// <summary>
        /// 清空所有数据
        /// </summary>
        public void Clear()
        {
            m_Data.Clear();
        }

        /// <summary>
        /// 获取所有键
        /// </summary>
        public IEnumerable<string> GetAllKeys()
        {
            return m_Data.Keys;
        }
    }
}
