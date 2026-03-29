/// <summary>
/// 变量类型枚举
/// 内置类型直接枚举，自定义类型统一用 Custom 标记 + GraphVariable.CustomTypeName
/// </summary>
namespace Shizuku.Graph
{
    using Shizuku.Core;
    public enum VariableType
    {
        Int,
        Float,
        Bool,
        String,
        Vector2,
        Vector3,
        GameObject,
        Transform,
        Color,

        /// <summary>
        /// 自定义 ShizukuClass 类型（具体类型名存储在 GraphVariable.CustomTypeName）
        /// </summary>
        Custom = 100,
    }

}
