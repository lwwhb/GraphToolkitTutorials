using UnityEngine;

namespace GraphToolkitTutorials.ContextBlocks
{
    /// <summary>
    /// 着色器函数数据 — 导入 .shaderfunc 后生成的主资产
    /// 文件名必须与类名一致（Unity AddObjectToAsset 要求）
    /// </summary>
    public class ShaderFunctionData : ScriptableObject
    {
        public string functionName;
        public int blockCount;
    }
}
