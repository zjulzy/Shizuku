
using UnityEngine;

/// <summary>
/// 蓝图行为基类
/// </summary>
/// <remarks>
/// 使用流程：
/// 1. 定义行为类：public class EnemyBehavior : BlueprintBehavior { }
/// 2. 右键菜单"Generate Blueprint" → 自动生成 EnemyBlueprint : ShizukuBluePrint&lt;EnemyBehavior&gt;
/// 3. 在Inspector中将蓝图赋值给 _blueprint 字段
/// 
/// 关键设计：
/// - Blueprint字段类型为ShizukuGraphBase（基类），避免编译时依赖具体蓝图类型
/// - 通过反射调用蓝图的InitializeBehavior(T)方法，保持类型安全
/// - 生成的蓝图类使用泛型ShizukuBluePrint&lt;T&gt;，方便反射和代码生成
/// </remarks>
public abstract class BlueprintBehavior : MonoBehaviour
{
    /// <summary>
    /// 蓝图引用（使用基类类型，避免编译时依赖）
    /// 赋值的时候需要检测泛型类型
    /// </summary>
    [SerializeField]
    private ShizukuGraphBase _blueprint;
    
    /// <summary>
    /// 获取蓝图实例
    /// </summary>
    public ShizukuGraphBase Blueprint => _blueprint;
    
}

