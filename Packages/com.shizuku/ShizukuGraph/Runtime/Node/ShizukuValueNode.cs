using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine;

namespace Shizuku.Graph
{
    using Shizuku.Core;

    [Serializable]
    public abstract class ShizukuValueNode : ShizukuNodeBase
    {
        public override bool SupportControlInput => false;
        public override bool SupportControlOutput => false;

        // ============================================================
        // 拉取代（Pull Generation）缓存机制
        // ============================================================
        //
        // 解决的问题：菱形依赖导致同一个值节点被重复计算
        //   RunnableNode
        //   ├── ValueC → ValueA   (第一次计算 A)
        //   └── ValueD → ValueA   (第二次计算 A — 多余)
        //
        // 工作方式：
        // - 每次 ShizukuRunnableNode.Execute() 发起 GetInputValues() 前，
        //   递增 CurrentPullGeneration（全局静态计数器）
        // - 值节点在 GetOutputValues() 中比较自身 _lastComputedGeneration
        //   与 CurrentPullGeneration，相同则跳过计算
        // - 不同的 RunnableNode 执行时 generation 不同，缓存自然失效，
        //   保证 SetVariable 等副作用能被后续节点正确读取
        // ============================================================

        /// <summary>
        /// 全局拉取代计数器，由 ShizukuRunnableNode.Execute() 递增
        /// </summary>
        internal static uint CurrentPullGeneration = 0;

        /// <summary>
        /// 该节点上次计算时的拉取代
        /// </summary>
        [NonSerialized]
        private uint _lastComputedGeneration = 0;

        /// <summary>
        /// 获取输出值（带拉取代缓存）
        /// 同一次 pull 内多次调用只会计算一次，后续直接返回缓存结果
        /// </summary>
        public sealed override void GetOutputValues()
        {
            // 同一次 pull 内已计算过，直接跳过
            if (_lastComputedGeneration == CurrentPullGeneration)
                return;

            _lastComputedGeneration = CurrentPullGeneration;

            // 先获取所有输入值（触发依赖节点计算）
            GetInputValues();

            // 再执行子类的计算逻辑
            OnComputeOutputValues();
        }

        /// <summary>
        /// 子类实现此方法来计算输出值
        /// 调用时输入值已经准备好（GetInputValues 已自动调用）
        /// </summary>
        protected virtual void OnComputeOutputValues() { }

        /// <summary>
        /// 手动使缓存失效，强制下次调用时重新计算
        /// </summary>
        public void InvalidateCache()
        {
            _lastComputedGeneration = 0;
        }
    }
}
