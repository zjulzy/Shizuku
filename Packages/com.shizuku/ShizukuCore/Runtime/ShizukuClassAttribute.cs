using System;

/// <summary>
/// 标记类可以在蓝图中使用
/// - 作为自定义变量类型
/// - 作为端口参数类型
/// </summary>
/// <remarks>
/// 使用示例：
/// <code>
/// [ShizukuClass("敌人配置", "游戏/配置")]
/// public class EnemyConfig
/// {
///     public float MaxHealth;
///     public float AttackPower;
/// }
/// </code>
/// </remarks>
namespace Shizuku.Core
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
    public class ShizukuClassAttribute : Attribute
    {
        /// <summary>
        /// 显示名称（可选，默认使用类名）
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 分类路径，使用 "/" 分隔（如 "游戏/配置"）
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// 类型描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 是否在变量创建菜单中显示
        /// </summary>
        public bool ShowInVariableMenu { get; set; }

        /// <summary>
        /// 排序顺序
        /// </summary>
        public int Order { get; set; }

        public ShizukuClassAttribute(string displayName = null, string category = "Custom")
        {
            DisplayName = displayName;
            Category = category;
            ShowInVariableMenu = true;
            Order = 0;
        }
    }


}
