using System.Reflection;
using UnityEngine;

/// <summary>
/// 蓝图基类的辅助扩展方法
/// 提供便捷的节点绑定功能
/// </summary>
namespace Shizuku.Graph
{
    using Shizuku.Core;
    public static class BlueprintExtensions
    {       
        /// <summary>
        /// 注册单个属性的访问器（手动版本）
        /// 用于性能敏感场景或需要自定义逻辑的情况
        /// </summary>
        public static void RegisterProperty<T, TValue>(
            this T behavior, 
            string propertyName,
            System.Func<TValue> getter,
            System.Action<TValue> setter = null) 
            where T : BlueprintBehavior<T>
        {
            // 注册Getter
            behavior.RegisterPropertyGetter(propertyName, () => getter());

            // 注册Setter（如果提供）
            if (setter != null)
            {
                behavior.RegisterPropertySetter(propertyName, (value) => 
                {
                    if (value is TValue typedValue)
                    {
                        setter(typedValue);
                    }
                });
            }
        }

        /// <summary>
        /// 注册只读属性
        /// </summary>
        public static void RegisterReadOnlyProperty<T, TValue>(
            this T behavior,
            string propertyName,
            System.Func<TValue> getter)
            where T : BlueprintBehavior<T>
        {
            behavior.RegisterPropertyGetter(propertyName, () => getter());
        }
    }


}
