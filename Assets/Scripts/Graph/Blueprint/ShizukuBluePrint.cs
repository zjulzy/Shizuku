using UnityEngine;

// TODO: 基于模板的蓝图系统
// 初步构想是蓝图模板绑定BlueprintBehavior类型，就可以在具体的蓝图类中重写BlueprintBehavior中的虚方法
// 在新建一个BlueprintBehavior类型后，通过自动生成代码生成对应的蓝图类

/// <summary>
/// 蓝图基类（泛型版本，用于代码生成和类型安全）
/// T: 对应的BlueprintBehavior类型
/// </summary>
/// <remarks>
/// 使用流程：
/// 1. 先定义 EnemyBehavior : BlueprintBehavior（不会有编译错误）
/// 2. 右键菜单"Generate Blueprint" → 自动生成 EnemyBlueprint : ShizukuBluePrint&lt;EnemyBehavior&gt;
/// 3. 生成器通过反射获取 EnemyBehavior 的成员，生成强类型的初始化代码
/// </remarks>
public abstract class ShizukuBluePrint<T> : ShizukuGraphBase where T : BlueprintBehavior
{
    private T _behavior;
}

