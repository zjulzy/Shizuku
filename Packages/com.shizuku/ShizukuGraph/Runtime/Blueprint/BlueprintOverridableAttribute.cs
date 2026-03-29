using System;

/// <summary>
/// 标记可以被蓝图重写的方法
/// 标记后，方法会自动检查蓝图是否实现了对应事件：
/// - 如果蓝图实现了，执行蓝图逻辑
/// - 如果蓝图未实现，执行 C# 默认逻辑
/// </summary>
namespace Shizuku.Graph
{
    using Shizuku.Core;
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class BlueprintOverridableAttribute : Attribute
    {
        /// <summary>
        /// 对应的蓝图事件名（如果为空则使用方法名）
        /// </summary>
        public string EventName { get; }

        /// <summary>
        /// 创建一个可被蓝图重写的方法标记
        /// </summary>
        /// <param name="eventName">蓝图事件名（默认使用方法名）</param>
        public BlueprintOverridableAttribute(string eventName = null)
        {
            EventName = eventName;
        }
    }

}
