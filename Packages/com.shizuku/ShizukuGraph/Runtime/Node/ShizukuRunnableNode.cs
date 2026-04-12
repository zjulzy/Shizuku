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

            // ---- 正常执行 ----
            GetInputValues();
            OnExecute();

            if (OnSelectNextNode(out var guid))
            {
                if (_context.Guid2NodeMap.TryGetValue(guid, out var nextNode))
                {
                    if (nextNode is ShizukuRunnableNode runnable)
                    {
                        return runnable.Execute();
                    }
                    else
                    {
                        Debug.LogError($"Next node is not a runnable node: {guid}");
                    }
                }
                else
                {
                    Debug.LogError($"Next node not found: {guid}");
                }
            }

            return ExecuteResult.Continue;
        }

        protected abstract void OnExecute();
        protected abstract bool OnSelectNextNode(out string nextNodeGUID);
    }
}
