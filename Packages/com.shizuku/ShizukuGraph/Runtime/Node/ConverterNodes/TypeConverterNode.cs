using System;
using UnityEngine;

/// <summary>
/// 类型转换节点基类
/// 所有类型转换节点继承此类
/// 显式地在图中表现类型转换过程，类似 UE 蓝图
/// </summary>
namespace Shizuku.Graph
{
    using Shizuku.Core;
    [Serializable]
    public abstract class TypeConverterNode : ShizukuValueNode
    {
        /// <summary>
        /// 转换节点使用蓝色标题栏
        /// </summary>
        public override Color TitleBarColor => new Color(0.5f, 0.7f, 1f, 1f);

        /// <summary>
        /// 输入端口的值类型（由子类指定）
        /// </summary>
        public abstract Type InputType { get; }

        /// <summary>
        /// 输出端口的值类型（由子类指定）
        /// </summary>
        public abstract Type OutputType { get; }

        /// <summary>
        /// 执行类型转换逻辑（由子类实现）
        /// </summary>
        protected abstract void ConvertValue();

        /// <summary>
        /// 计算输出值：执行类型转换
        /// 输入值已由基类 GetOutputValues() 自动准备好
        /// </summary>
        protected override void OnComputeOutputValues()
        {
            ConvertValue();
        }
    }


}
