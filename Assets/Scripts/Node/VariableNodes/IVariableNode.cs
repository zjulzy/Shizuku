/// <summary>
/// 变量节点接口，用于标识 Get/Set 变量节点
/// 提供 TargetVariableType 以支持变量选择器按类型过滤
/// </summary>
public interface IVariableNode
{
    VariableType TargetVariableType { get; }
}

