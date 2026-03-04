# ShizukuClass 和 ShizukuFunction 使用指南

## 🎯 目标

本指南帮助你快速上手使用 `[ShizukuClass]` 和 `[ShizukuFunction]` 特性，将自定义类型和方法暴露给蓝图系统。

---

## 📚 基础概念

### 什么是 ShizukuClass？

`[ShizukuClass]` 用于标记类或结构体，使其可以：
- 作为蓝图变量的类型
- 作为节点端口的参数类型
- 在编辑器中可视化编辑

### 什么是 ShizukuFunction？

`[ShizukuFunction]` 用于标记方法，使其可以：
- 自动生成对应的蓝图节点
- 在蓝图中显式调用
- 封装 C# 实现的复杂逻辑

---

## 🚀 5 分钟快速开始

### 步骤 1：创建工具类

创建文件 `Assets/Scripts/MyMathUtils.cs`：

```csharp
using UnityEngine;

public static class MyMathUtils
{
    [ShizukuFunction("计算距离", "我的工具/数学")]
    public static float CalculateDistance(Vector3 pointA, Vector3 pointB)
    {
        return Vector3.Distance(pointA, pointB);
    }
}
```

### 步骤 2：生成节点

1. 打开 **Shizuku > Function Node Generator**
2. 点击 **🔄 Refresh** 按钮
3. 找到 "MyMathUtils" 分组，点击 **Generate**
4. 等待 Unity 编译完成

### 步骤 3：在蓝图中使用

1. 打开任意蓝图编辑器
2. 右键 → 搜索"计算距离"
3. 创建节点并连接

**完成！** 🎉 你已经成功创建了第一个自定义函数节点。

---

## 📖 详细教程

### 教程 1：创建数学工具库

**目标**：创建一组常用的数学函数节点。

#### 1.1 创建工具类

```csharp
using UnityEngine;

/// <summary>
/// 数学工具库
/// </summary>
public static class MathToolkit
{
    /// <summary>
    /// 计算平方
    /// </summary>
    [ShizukuFunction("平方", "数学/基础", Pure = true)]
    public static float Square(float value)
    {
        return value * value;
    }

    /// <summary>
    /// 计算平方根
    /// </summary>
    [ShizukuFunction("平方根", "数学/基础", Pure = true)]
    public static float SquareRoot(float value)
    {
        return Mathf.Sqrt(value);
    }

    /// <summary>
    /// 限制值在 0-1 之间
    /// </summary>
    [ShizukuFunction("限制01", "数学/基础", Pure = true)]
    public static float Clamp01(float value)
    {
        return Mathf.Clamp01(value);
    }

    /// <summary>
    /// 线性插值
    /// </summary>
    [ShizukuFunction("插值", "数学/高级", Pure = true)]
    public static float Lerp(float a, float b, float t)
    {
        return Mathf.Lerp(a, b, t);
    }
}
```

#### 1.2 生成节点

打开 Function Node Generator 窗口：

```
📦 MathToolkit
  ├─ 平方          ⚠️ [Generate]
  ├─ 平方根        ⚠️ [Generate]
  ├─ 限制01        ⚠️ [Generate]
  └─ 插值          ⚠️ [Generate]
```

点击 **Generate All Missing**，生成所有节点。

#### 1.3 在蓝图中使用

创建一个计算斜边长度的蓝图：

```
[Float: a] ──┐
             ├─→ [平方] ──┐
[Float: b] ──┘           ├─→ [加法] ──→ [平方根] ──→ [Result]
                         │
[Float: b] ──→ [平方] ──┘
```

---

### 教程 2：自定义数据类型

**目标**：创建自定义数据结构并在蓝图中使用。

#### 2.1 定义数据类型

```csharp
using System;
using UnityEngine;

/// <summary>
/// 角色属性数据
/// </summary>
[ShizukuClass("角色属性", "游戏/角色")]
[Serializable]
public class CharacterStats
{
    [Tooltip("当前生命值")]
    public float CurrentHealth = 100f;
    
    [Tooltip("最大生命值")]
    public float MaxHealth = 100f;
    
    [Tooltip("攻击力")]
    public float Attack = 10f;
    
    [Tooltip("防御力")]
    public float Defense = 5f;
    
    [Tooltip("移动速度")]
    public float MoveSpeed = 5f;
}
```

**重要**：必须添加 `[Serializable]` 特性！

#### 2.2 创建操作函数

```csharp
public static class CharacterStatsUtils
{
    [ShizukuFunction("受到伤害", "游戏/战斗")]
    public static float ApplyDamage(float currentHealth, float damage)
    {
        return Mathf.Max(0, currentHealth - damage);
    }

    [ShizukuFunction("计算伤害", "游戏/战斗")]
    public static float CalculateDamage(float attackPower, float defense)
    {
        float reduction = defense * 0.5f;
        return Mathf.Max(1, attackPower - reduction);
    }

    [ShizukuFunction("是否存活", "游戏/状态", Pure = true)]
    public static bool IsAlive(float currentHealth)
    {
        return currentHealth > 0;
    }
}
```

#### 2.3 在蓝图中使用（未来版本）

```
[CharacterStats 变量: player] ──→ [Get.CurrentHealth] ──┐
                                                        ├─→ [受到伤害] ──→ [Set.CurrentHealth]
[Float: damage] ────────────────────────────────────────┘
```

**注意**：变量系统集成计划在 v0.2.1 实现。

---

### 教程 3：字符串操作

**目标**：创建字符串处理工具。

#### 3.1 创建工具类

```csharp
public static class StringToolkit
{
    [ShizukuFunction("拼接", "字符串/操作", Pure = true)]
    public static string Concat(string a, string b)
    {
        return string.Concat(a, b);
    }

    [ShizukuFunction("转大写", "字符串/转换", Pure = true)]
    public static string ToUpperCase(string text)
    {
        return text?.ToUpper() ?? "";
    }

    [ShizukuFunction("转小写", "字符串/转换", Pure = true)]
    public static string ToLowerCase(string text)
    {
        return text?.ToLower() ?? "";
    }

    [ShizukuFunction("包含", "字符串/查询", Pure = true)]
    public static bool Contains(string text, string substring)
    {
        return text?.Contains(substring) ?? false;
    }

    [ShizukuFunction("替换", "字符串/操作", Pure = true)]
    public static string Replace(string text, string oldValue, string newValue)
    {
        return text?.Replace(oldValue, newValue) ?? "";
    }
}
```

#### 3.2 实际应用示例

制作一个玩家名称格式化器：

```
[String: playerName] ──→ [转大写] ──→ [拼接] ──→ [Log]
                                       ↑
[String: "[玩家] "] ───────────────────┘
```

输入："alice" → 输出："[玩家] ALICE"

---

### 教程 4：GameObject 工具

**目标**：创建常用的 GameObject 操作节点。

#### 4.1 创建工具类

```csharp
using UnityEngine;

public static class GameObjectToolkit
{
    [ShizukuFunction("设置激活", "GameObject/状态")]
    public static void SetActive(GameObject obj, bool active)
    {
        if (obj != null)
        {
            obj.SetActive(active);
        }
    }

    [ShizukuFunction("查找对象", "GameObject/查询")]
    public static GameObject FindObjectByName(string name)
    {
        return GameObject.Find(name);
    }

    [ShizukuFunction("实例化", "GameObject/创建")]
    public static GameObject Instantiate(GameObject prefab, Vector3 position)
    {
        return Object.Instantiate(prefab, position, Quaternion.identity);
    }

    [ShizukuFunction("销毁", "GameObject/销毁")]
    public static void Destroy(GameObject obj)
    {
        if (obj != null)
        {
            Object.Destroy(obj);
        }
    }

    [ShizukuFunction("获取位置", "Transform/属性", Pure = true)]
    public static Vector3 GetPosition(Transform transform)
    {
        return transform != null ? transform.position : Vector3.zero;
    }

    [ShizukuFunction("设置位置", "Transform/属性")]
    public static void SetPosition(Transform transform, Vector3 position)
    {
        if (transform != null)
        {
            transform.position = position;
        }
    }
}
```

---

## 💡 最佳实践

### ✅ DO - 推荐做法

#### 1. 使用清晰的命名和分类

```csharp
[ShizukuFunction("计算伤害", "游戏/战斗/伤害系统")]
```

而不是：
```csharp
[ShizukuFunction("Calc", "Utils")]
```

#### 2. 标记纯函数

对于无副作用的函数，添加 `Pure = true`：

```csharp
[ShizukuFunction("距离", "数学", Pure = true)]
public static float Distance(Vector3 a, Vector3 b)
{
    return Vector3.Distance(a, b);
}
```

这有助于：
- 性能优化（可缓存结果）
- 代码理解（明确无副作用）

#### 3. 添加描述

```csharp
[ShizukuFunction("插值", "数学/高级", 
    Description = "在两个值之间进行线性插值，t=0返回a，t=1返回b")]
public static float Lerp(float a, float b, float t)
{
    return Mathf.Lerp(a, b, t);
}
```

#### 4. 使用 XML 文档注释

```csharp
/// <summary>
/// 计算两点之间的距离
/// </summary>
/// <param name="a">第一个点</param>
/// <param name="b">第二个点</param>
/// <returns>两点之间的欧几里得距离</returns>
[ShizukuFunction("距离", "数学/向量", Pure = true)]
public static float Distance(Vector3 a, Vector3 b)
{
    return Vector3.Distance(a, b);
}
```

#### 5. 合理组织分类

```
数学/
  ├─ 基础/           # 加减乘除、平方、开方
  ├─ 高级/           # 插值、曲线、噪声
  └─ 向量/           # 向量运算

游戏/
  ├─ 战斗/
  │   ├─ 伤害/
  │   └─ Buff/
  ├─ 角色/
  └─ 物品/
```

### ❌ DON'T - 避免做法

#### 1. 不要标记不稳定的 API

频繁修改签名会导致生成的节点过时：

```csharp
// ❌ 错误示例：签名可能会频繁变化
[ShizukuFunction("临时测试", "测试")]
public static void TempTest(int a, int b, string c) { }
```

#### 2. 不要标记过于简单的操作

```csharp
// ❌ 错误示例：太简单，直接用数学节点更好
[ShizukuFunction("加法", "数学")]
public static int Add(int a, int b) { return a + b; }
```

#### 3. 不要在函数中包含复杂逻辑

```csharp
// ❌ 错误示例：逻辑太复杂，应该封装在类中
[ShizukuFunction("处理战斗", "游戏")]
public static void ProcessCombat(...)
{
    // 100+ 行复杂逻辑...
}
```

应该拆分为多个小函数：

```csharp
[ShizukuFunction("计算伤害", "战斗")]
public static float CalculateDamage(...) { }

[ShizukuFunction("应用伤害", "战斗")]
public static void ApplyDamage(...) { }

[ShizukuFunction("检查死亡", "战斗")]
public static bool CheckDeath(...) { }
```

---

## 🔧 常见问题

### Q1: 生成的节点在哪里？

**A**: 默认生成在 `Assets/Scripts/Node/DerivedNodes/Generated/` 目录下。

你可以在 Function Node Generator 窗口中修改生成路径。

### Q2: 修改了函数签名后需要重新生成吗？

**A**: 是的。修改参数或返回值后，需要点击 **Regenerate** 重新生成节点类。

### Q3: 支持方法重载吗？

**A**: 支持。系统会为每个重载生成不同的节点类。

示例：
```csharp
[ShizukuFunction("距离", "数学")]
public static float Distance(Vector2 a, Vector2 b) { ... }

[ShizukuFunction("距离", "数学")]
public static float Distance(Vector3 a, Vector3 b) { ... }
```

生成的节点：
- `MathUtils_Distance_Vector2_Vector2_Node`
- `MathUtils_Distance_Vector3_Vector3_Node`

### Q4: 可以标记实例方法吗？

**A**: 当前版本（v0.2.0）主要支持静态方法。实例方法支持计划在 v0.3.0 实现。

### Q5: 如何删除不需要的节点？

**A**: 在 Function Node Generator 窗口中，找到对应节点，点击 **Delete** 按钮。

### Q6: Pure 参数有什么用？

**A**: 标记 `Pure = true` 表示该函数是纯函数（无副作用）。这有助于：
- 未来的性能优化（缓存结果）
- 代码理解和维护
- 调试和分析

### Q7: 可以在运行时动态添加函数吗？

**A**: 不可以。所有函数必须在编译时标记，并通过代码生成工具生成节点类。

---

## 📊 性能提示

### 1. 优先使用静态方法

静态方法调用性能更好，且不需要实例对象：

```csharp
✅ public static float Calculate(...) { }
❌ public float Calculate(...) { }  // 需要实例
```

### 2. 避免频繁的 GameObject.Find

```csharp
// ❌ 错误示例
[ShizukuFunction("获取玩家", "游戏")]
public static GameObject GetPlayer()
{
    return GameObject.Find("Player");  // 每次都查找
}
```

应该缓存引用：

```csharp
// ✅ 正确示例
private static GameObject _cachedPlayer;

[ShizukuFunction("获取玩家", "游戏")]
public static GameObject GetPlayer()
{
    if (_cachedPlayer == null)
        _cachedPlayer = GameObject.Find("Player");
    return _cachedPlayer;
}
```

### 3. 使用合适的数据类型

选择最合适的参数类型以避免不必要的转换：

```csharp
✅ public static void SetPosition(Transform t, Vector3 pos) { }
❌ public static void SetPosition(GameObject obj, Vector3 pos)
   {
       obj.transform.position = pos;  // 额外的属性访问
   }
```

---

## 🎓 进阶技巧

### 1. 组合使用多个函数

创建可复用的小函数，在蓝图中组合使用：

```csharp
[ShizukuFunction("获取距离", "工具", Pure = true)]
public static float GetDistance(Vector3 a, Vector3 b) { ... }

[ShizukuFunction("在范围内", "工具", Pure = true)]
public static bool IsInRange(float distance, float maxRange) { ... }
```

蓝图中：
```
[Pos A] ──┐
          ├─→ [获取距离] ──→ [在范围内] ──→ [Result]
[Pos B] ──┘                     ↑
                                │
[Float: maxRange] ──────────────┘
```

### 2. 返回多个值（未来版本）

当前版本只支持单个返回值。如果需要返回多个值，可以：

**方案 A**：使用自定义类型
```csharp
[Serializable]
[ShizukuClass("计算结果", "工具")]
public class CalculationResult
{
    public float Sum;
    public float Average;
    public float Max;
    public float Min;
}

[ShizukuFunction("统计", "数学")]
public static CalculationResult CalculateStats(float[] values) { ... }
```

**方案 B**：拆分为多个函数
```csharp
[ShizukuFunction("求和", "数学")]
public static float Sum(float[] values) { ... }

[ShizukuFunction("求平均", "数学")]
public static float Average(float[] values) { ... }
```

---

## 📚 示例项目

完整的示例代码见：`Assets/Scripts/ShizukuAttributeExamples.cs`

包含以下示例：
1. 数学工具类（MathUtils）
2. 字符串工具类（StringUtils）
3. GameObject 工具类（GameObjectUtils）
4. 自定义数据类型（EnemyConfig, SkillData）

---

## 🔗 相关文档

- [实现文档](Implementation_ShizukuAttributes.md) - 技术细节和架构
- [开发路线图](ROADMAP.md) - 后续功能计划
- [贡献指南](CONTRIBUTING.md) - 如何贡献代码

---

**最后更新**：2026-03-05  
**适用版本**：v0.2.0+

