using System;
using System.Collections.Generic;

namespace Shizuku.Graph
{
    public partial class ShizukuGraphBase
    {
        [NonSerialized]
        private List<LatentExecution> _activeLatentExecutions = new();

        [NonSerialized]
        private int _rootExecutionDepth;

        [NonSerialized]
        private int _latentRestrictionDepth;

        [NonSerialized]
        private string _latentRestrictionReason;

        public int ActiveLatentNodeCount => _activeLatentExecutions?.Count ?? 0;

        internal bool IsExecutingRootChain => _rootExecutionDepth > 0;

        internal bool TryRegisterLatentNode(
            ShizukuLatentNode node,
            out bool blocksRootExecution,
            out string rejectionReason)
        {
            blocksRootExecution = false;
            rejectionReason = null;

            if (!_runtimeInitialized)
            {
                rejectionReason = "Latent 节点只能在已初始化的运行时图中执行";
                return false;
            }

            if (_latentRestrictionDepth > 0)
            {
                rejectionReason = string.IsNullOrEmpty(_latentRestrictionReason)
                    ? "当前执行上下文不支持 Latent 节点"
                    : _latentRestrictionReason;
                return false;
            }

            _activeLatentExecutions ??= new List<LatentExecution>();
            for (var index = 0; index < _activeLatentExecutions.Count; index++)
            {
                if (ReferenceEquals(_activeLatentExecutions[index].Node, node))
                {
                    rejectionReason = $"Latent 节点不允许重入: {node.GetType().Name} ({node.GUID})";
                    return false;
                }
            }

            blocksRootExecution = IsExecutingRootChain;
            _activeLatentExecutions.Add(new LatentExecution(node, blocksRootExecution));
            return true;
        }

        internal void UnregisterLatentNode(ShizukuLatentNode node)
        {
            if (_activeLatentExecutions == null)
                return;

            _activeLatentExecutions.RemoveAll(execution =>
                ReferenceEquals(execution.Node, node));
        }

        internal ExecutionScope EnterExecutionScope(bool isRootExecution)
        {
            return new ExecutionScope(this, isRootExecution);
        }

        internal LatentRestrictionScope DisallowLatentExecution(string reason)
        {
            return new LatentRestrictionScope(this, reason);
        }

        private bool TickLatentExecutions()
        {
            var rootWasBlocked = HasBlockingRootExecution();
            if (_activeLatentExecutions == null || _activeLatentExecutions.Count == 0)
                return rootWasBlocked;

            var snapshot = _activeLatentExecutions.ToArray();
            foreach (var execution in snapshot)
            {
                if (execution?.Node == null || !_activeLatentExecutions.Contains(execution))
                    continue;

                using (EnterExecutionScope(execution.BlocksRootExecution))
                {
                    execution.Node.TickLatentFromGraph();
                }
            }

            return rootWasBlocked;
        }

        private bool HasBlockingRootExecution()
        {
            if (_activeLatentExecutions == null)
                return false;

            foreach (var execution in _activeLatentExecutions)
            {
                if (execution != null && execution.BlocksRootExecution)
                    return true;
            }

            return false;
        }

        private ExecuteResult ExecuteRoot(ShizukuRootNode root)
        {
            using (EnterExecutionScope(true))
            {
                return root.StartExcute();
            }
        }

        private void CancelActiveLatentExecutions()
        {
            if (_activeLatentExecutions == null || _activeLatentExecutions.Count == 0)
                return;

            var snapshot = _activeLatentExecutions.ToArray();
            foreach (var execution in snapshot)
            {
                try
                {
                    execution?.Node?.CancelLatentFromGraph();
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(exception);
                }
            }

            _activeLatentExecutions.Clear();
        }

        internal readonly struct ExecutionScope : IDisposable
        {
            private readonly ShizukuGraphBase _graph;
            private readonly bool _isRootExecution;

            internal ExecutionScope(ShizukuGraphBase graph, bool isRootExecution)
            {
                _graph = graph;
                _isRootExecution = isRootExecution;
                if (_graph != null && _isRootExecution)
                    _graph._rootExecutionDepth++;
            }

            public void Dispose()
            {
                if (_graph != null && _isRootExecution && _graph._rootExecutionDepth > 0)
                    _graph._rootExecutionDepth--;
            }
        }

        internal readonly struct LatentRestrictionScope : IDisposable
        {
            private readonly ShizukuGraphBase _graph;
            private readonly string _previousReason;

            internal LatentRestrictionScope(ShizukuGraphBase graph, string reason)
            {
                _graph = graph;
                _previousReason = graph?._latentRestrictionReason;
                if (_graph == null)
                    return;

                _graph._latentRestrictionDepth++;
                _graph._latentRestrictionReason = reason;
            }

            public void Dispose()
            {
                if (_graph == null || _graph._latentRestrictionDepth <= 0)
                    return;

                _graph._latentRestrictionDepth--;
                _graph._latentRestrictionReason = _previousReason;
            }
        }

        private sealed class LatentExecution
        {
            public readonly ShizukuLatentNode Node;
            public readonly bool BlocksRootExecution;

            public LatentExecution(ShizukuLatentNode node, bool blocksRootExecution)
            {
                Node = node;
                BlocksRootExecution = blocksRootExecution;
            }
        }
    }
}
