using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 节点执行结果，用于调试时中断递归链
/// </summary>
namespace Shizuku.Graph
{
    using Shizuku.Core;
    public enum ExecuteResult
    {
        /// <summary>链正常完成</summary>
        Continue,
        /// <summary>链被断点/单步中断</summary>
        Halted,

    }

    [Serializable]
    public abstract partial class ShizukuRunnableNode : ShizukuNormalNode
    {
        public sealed override bool SupportControlInput => true;
        public sealed override bool SupportControlOutput => true;

        // ---- 循环依赖保护 ----
        [NonSerialized] private bool _executing;
        private const int MaxChainDepth = 2000;
        [NonSerialized] private static int _chainDepth;

        public override void Init(INodeContext context)
        {
            base.Init(context);
        }

        public ExecuteResult Execute()
        {
    #if UNITY_EDITOR
            if (ShizukuDebugger.Enabled)
            {
                var debugResult = DebugCheck();
                if (debugResult == ExecuteResult.Halted)
                    return ExecuteResult.Halted;
            }
    #endif

            // 循环依赖保护
            if (_executing)
            {
                Debug.LogError($"[Shizuku] 检测到循环执行: {GetType().Name} ({GUID})");
                return ExecuteResult.Continue;
            }

            if (++_chainDepth > MaxChainDepth)
            {
                _chainDepth--;
                Debug.LogError($"[Shizuku] 执行链深度超过 {MaxChainDepth}，可能存在无限递归");
                return ExecuteResult.Continue;
            }

            _executing = true;
            try
            {
                // ---- 正常执行 ----
                // 递增拉取代，使值节点缓存失效（同一次 pull 内仍可复用）
                ShizukuValueNode.CurrentPullGeneration++;
                GetInputValues();
                OnExecute();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Shizuku] 节点执行异常 [{GetType().Name}] GUID={GUID}: {e.Message}\n{e.StackTrace}");
                _executing = false;
                _chainDepth--;
                return ExecuteResult.Continue;
            }
            _executing = false;

            ExecuteResult result = ExecuteResult.Continue;
            if (OnSelectNextNode(out var guid))
            {
                if (_context.Guid2NodeMap.TryGetValue(guid, out var nextNode))
                {
                    if (nextNode is ShizukuRunnableNode runnable)
                    {
                        result = runnable.Execute();
                    }
                    else if (nextNode is MethodReturnNode || nextNode is BlueprintReturnNode)
                    {
                        result = ExecuteResult.Continue;
                    }
                    else
                    {
                        Debug.LogError($"[Shizuku] 下一个节点不是合法的可执行节点: {guid}");
                    }
                }
                else
                {
                    Debug.LogError($"[Shizuku] 找不到下一个节点: {guid}");
                }
            }

            _chainDepth--;
            return result;
        }

        protected abstract void OnExecute();
        protected abstract bool OnSelectNextNode(out string nextNodeGUID);

        /// <summary>
        /// 执行一条子链（从 ChainPort 指向的节点开始），用于循环体等场景。
        /// 返回 ExecuteResult，调用者可检测 Break / Halted。
        /// </summary>
        protected ExecuteResult ExecuteSubChain(ChainPort port)
        {
            if (port == null || string.IsNullOrEmpty(port.NextNodeGuid)) return ExecuteResult.Continue;
            if (!_context.Guid2NodeMap.TryGetValue(port.NextNodeGuid, out var node)) return ExecuteResult.Continue;
            if (node is ShizukuRunnableNode runnable)
                return runnable.Execute();
            return ExecuteResult.Continue;
        }
    }
}
