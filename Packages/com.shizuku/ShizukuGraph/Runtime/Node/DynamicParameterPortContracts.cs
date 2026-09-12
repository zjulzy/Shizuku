using System.Collections.Generic;

namespace Shizuku.Graph
{
    /// <summary>
    /// 动态参数端口在节点视图中的展示信息。
    /// 端口名称和输入/输出方向分别由 <see cref="ParameterEdgePort.Name"/>
    /// 与 <see cref="ParameterEdgePort.IsOut"/> 决定。
    /// </summary>
    public readonly struct DynamicParameterPortDescriptor
    {
        public ParameterEdgePort Port { get; }
        public string Tooltip { get; }

        public DynamicParameterPortDescriptor(ParameterEdgePort port, string tooltip = null)
        {
            Port = port;
            Tooltip = tooltip;
        }
    }

    /// <summary>
    /// 由节点提供数量可变、保存在集合中的参数端口。
    /// 同步方法必须可重复调用，并负责维护端口稳定身份以及相关边的迁移或删除。
    /// </summary>
    public interface IDynamicParameterPortProvider
    {
        IEnumerable<DynamicParameterPortDescriptor> DynamicParameterPorts { get; }

        /// <returns>端口描述或相关边是否发生了持久化变化。</returns>
        bool SynchronizeDynamicParameterPorts(INodeContext context);
    }

    /// <summary>
    /// 接收节点 Inspector 中序列化字段完成写回后的通知。
    /// 实现可以同步派生数据；返回 true 时 Graph Editor 会重建当前视图和 Inspector。
    /// </summary>
    public interface INodeSerializedFieldChangeHandler
    {
        bool OnSerializedFieldChanged(string fieldName, INodeContext context);
    }
}
