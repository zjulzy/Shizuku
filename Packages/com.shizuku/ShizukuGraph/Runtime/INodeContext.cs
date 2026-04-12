using System.Collections.Generic;

namespace Shizuku.Graph
{
    /// <summary>
    /// 节点执行上下文接口。
    /// 图中的节点 → 上下文是 ShizukuGraphBase；
    /// 函数中的节点 → 上下文是 ShizukuMethod。
    /// </summary>
    public interface INodeContext
    {
        /// <summary>
        /// 当前上下文内的 GUID → 节点映射
        /// </summary>
        Dictionary<string, ShizukuNodeBase> Guid2NodeMap { get; }

        /// <summary>
        /// 当前上下文内的 GUID → 边映射
        /// </summary>
        Dictionary<string, ParameterEdge> Guid2EdgeMap { get; }

        /// <summary>
        /// 根图引用（用于访问变量、函数等全局资源）。
        /// 图自身返回 this，方法返回所属的父图。
        /// </summary>
        ShizukuGraphBase RootGraph { get; }
    }
}

