using System;
using UnityEngine;

namespace Shizuku.Graph
{
    /// <summary>
    /// 跨帧节点每次 Tick 后的状态。
    /// </summary>
    public enum ShizukuLatentTickResult
    {
        Running,
        Completed,
        Failed
    }

    /// <summary>
    /// 可跨多帧执行的控制流节点。
    /// 同一个运行时节点实例同一时间只允许存在一次执行；完成或失败后由图恢复对应分支。
    /// </summary>
    [Serializable]
    public abstract class ShizukuLatentNode : ShizukuRunnableNode
    {
        [NonSerialized]
        private bool _isLatentActive;

        [NonSerialized]
        private bool _blocksRootExecution;

        public bool IsLatentActive => _isLatentActive;

        protected virtual ChainPort StartedPort => null;
        protected virtual ChainPort CompletedPort => null;
        protected virtual ChainPort FailedPort => null;

        protected sealed override void OnExecute()
        {
            if (_isLatentActive)
            {
                ShizukuErrorReporter.LogError(
                    $"Latent 节点不允许重入: {GetType().Name} ({GUID})",
                    this);
                return;
            }

            var graph = RootGraph;
            if (graph == null)
            {
                ShizukuErrorReporter.LogError("Latent 节点尚未绑定运行时图", this);
                return;
            }

            if (!graph.TryRegisterLatentNode(this, out _blocksRootExecution, out var rejectionReason))
            {
                ShizukuErrorReporter.LogError(rejectionReason, this);
                return;
            }

            _isLatentActive = true;

            var started = false;
            try
            {
                started = OnLatentStart();
            }
            catch (Exception exception)
            {
                ShizukuErrorReporter.LogException(
                    exception,
                    this,
                    ShizukuExecutionContext.Current);
            }

            if (!_isLatentActive)
                return;

            if (!started)
            {
                FinishLatent(ShizukuLatentTickResult.Failed);
                return;
            }

            ExecuteSubChain(StartedPort);
        }

        protected sealed override bool OnSelectNextNode(out string nextNodeGUID)
        {
            nextNodeGUID = null;
            return false;
        }

        /// <summary>
        /// 启动跨帧操作。返回 false 会结束本次执行并进入 Failed 分支。
        /// </summary>
        protected abstract bool OnLatentStart();

        /// <summary>
        /// 由所属图每帧调用，直到返回 Completed 或 Failed。
        /// </summary>
        protected abstract ShizukuLatentTickResult OnLatentTick();

        /// <summary>
        /// 图重新初始化或销毁时取消正在运行的操作。取消不会触发任何控制流出口。
        /// </summary>
        protected virtual void OnLatentCancel()
        {
        }

        /// <summary>
        /// 完成或失败后、恢复对应控制流之前调用，用于释放本次操作状态。
        /// </summary>
        protected virtual void OnLatentFinished(ShizukuLatentTickResult result)
        {
        }

        internal void TickLatentFromGraph()
        {
            if (!_isLatentActive)
                return;

            ShizukuLatentTickResult result;
            try
            {
                result = OnLatentTick();
            }
            catch (Exception exception)
            {
                ShizukuErrorReporter.LogException(
                    exception,
                    this,
                    ShizukuExecutionContext.Current);
                result = ShizukuLatentTickResult.Failed;
            }

            if (result != ShizukuLatentTickResult.Running)
                FinishLatent(result);
        }

        internal void CancelLatentFromGraph()
        {
            if (!_isLatentActive)
                return;

            var graph = RootGraph;
            _isLatentActive = false;
            _blocksRootExecution = false;
            graph?.UnregisterLatentNode(this);

            try
            {
                OnLatentCancel();
            }
            catch (Exception exception)
            {
                ShizukuErrorReporter.LogException(
                    exception,
                    this,
                    ShizukuExecutionContext.Current);
            }
        }

        public override void DisposeRuntime()
        {
            CancelLatentFromGraph();
            base.DisposeRuntime();
        }

        private void FinishLatent(ShizukuLatentTickResult result)
        {
            var graph = RootGraph;
            var blocksRootExecution = _blocksRootExecution;
            var continuationPort = result == ShizukuLatentTickResult.Completed
                ? CompletedPort
                : FailedPort;

            _isLatentActive = false;
            _blocksRootExecution = false;
            graph?.UnregisterLatentNode(this);

            try
            {
                OnLatentFinished(result);
            }
            catch (Exception exception)
            {
                ShizukuErrorReporter.LogException(
                    exception,
                    this,
                    ShizukuExecutionContext.Current);
            }

            if (graph == null)
                return;

            using (graph.EnterExecutionScope(blocksRootExecution))
            {
                ExecuteSubChain(continuationPort);
            }
        }
    }
}
