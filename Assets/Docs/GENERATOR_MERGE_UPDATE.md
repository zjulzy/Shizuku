# 生成器合并更新文档

## 📝 更新概述

将 `FunctionGeneratorTab` 和 `VariableTypeGeneratorTab` 合并为统一的 `UnifiedShizukuGeneratorTab`，并强制要求 `ShizukuFunction` 必须在 `ShizukuClass` 标记的类中使用。

**更新日期**：2026年3月8日  
**版本**：v1.0

---

## 🎯 主要变更

### 1. 新增统一生成器 Tab

**文件**：`Assets/Editor/Generator/UnifiedShizukuGeneratorTab.cs`

**功能**：
- 统一管理 ShizukuClass 和 ShizukuFunction
- 首先显示所有 ShizukuClass，按类别分组
- 在每个 ShizukuClass 下显示其包含的 ShizukuFunction
- 对于非静态类（`ShowInVariableMenu = true`），支持生成自定义变量类型

**UI 结构**：
```
📁 类别 A
  📦 ShizukuClass A1
    [Generate Variable Type] (如果是非静态类)
    ⚡ Function 1  [Generate] / [Regenerate] [Delete]
    ⚡ Function 2  [Generate] / [Regenerate] [Delete]
  📦 ShizukuClass A2
    ⚡ Function 3  [Generate] / [Regenerate] [Delete]

📁 类别 B
  📦 ShizukuClass B1
    ...
```

**按钮功能**：
- **🔄 Refresh**：刷新扫描所有 ShizukuClass 和 Function
- **Generate All Pending**：生成所有未生成的函数节点
- **Generate Variable Types**：生成所有自定义类型的变量支持代码
- **Generate Variable Type**（单个类）：为特定类生成变量类型
- **Generate / Regenerate**（单个函数）：生成或重新生成函数节点
- **Delete**（单个函数）：删除已生成的函数节点

---

### 2. 强制 ShizukuFunction 必须在 ShizukuClass 中

**文件**：`Assets/Scripts/Node/ShizukuTypeRegistry.cs`

**变更**：
```csharp
private static void RegisterFunction(Type type, MethodInfo method, ShizukuFunctionAttribute attr)
{
    // 验证：ShizukuFunction 必须在 ShizukuClass 中
    if (!_registeredClasses.ContainsKey(type))
    {
        Debug.LogWarning($"[ShizukuTypeRegistry] ShizukuFunction '{method.Name}' in type '{type.Name}' is ignored because the type is not marked with [ShizukuClass]");
        return;
    }
    
    // ...原有逻辑
}
```

**影响**：
- 未标记 `[ShizukuClass]` 的类中的 `[ShizukuFunction]` 会被忽略
- 控制台会输出警告信息
- 确保代码结构的一致性

---

### 3. 更新生成器窗口

**文件**：`Assets/Editor/Generator/ShizukuGeneratorWindow.cs`

**变更**：
- 将原来的三个 Tab（Blueprint、Function、VariableType）合并为两个
- 新的 Tab 结构：
  - **Blueprint Generator** 🎨：蓝图类生成器（保持不变）
  - **ShizukuClass & Function** 📦：统一的类和函数管理器（新）

**代码变更**：
```csharp
// 旧枚举
private enum TabType
{
    Blueprint,
    Function,
    VariableType
}

// 新枚举
private enum TabType
{
    Blueprint,
    ShizukuClassAndFunction
}

// 旧实例
private FunctionGeneratorTab _functionTab;
private VariableTypeGeneratorTab _variableTypeTab;

// 新实例
private UnifiedShizukuGeneratorTab _unifiedTab;
```

---

### 4. 更新文档

**文件**：`Assets/Docs/Guide_ShizukuAttributes.md`

**变更内容**：

#### 强调 ShizukuFunction 的限制
```markdown
**⚠️ 重要限制**：`[ShizukuFunction]` 只能在标记了 `[ShizukuClass]` 的类中使用！
```

#### 更新所有示例代码
所有包含 `[ShizukuFunction]` 的示例都添加了 `[ShizukuClass]` 标记：

```csharp
// 静态工具类示例
[ShizukuClass("数学工具", "工具", ShowInVariableMenu = false)]
public static class MyMathUtils
{
    [ShizukuFunction("计算距离", "我的工具/数学")]
    public static float CalculateDistance(Vector3 pointA, Vector3 pointB)
    {
        return Vector3.Distance(pointA, pointB);
    }
}
```

**注意**：
- 静态工具类设置 `ShowInVariableMenu = false`（不需要实例）
- 非静态类保持 `ShowInVariableMenu = true`（默认值，支持创建变量）

---

## 🔄 迁移指南

### 对于现有代码

如果你已经使用了 `[ShizukuFunction]`，需要进行以下更新：

#### 步骤 1：为类添加 ShizukuClass 标记

**旧代码**：
```csharp
public static class MathUtils
{
    [ShizukuFunction("加法", "数学")]
    public static float Add(float a, float b) => a + b;
}
```

**新代码**：
```csharp
[ShizukuClass("数学工具", "工具", ShowInVariableMenu = false)]
public static class MathUtils
{
    [ShizukuFunction("加法", "数学")]
    public static float Add(float a, float b) => a + b;
}
```

#### 步骤 2：检查控制台警告

打开 Unity 控制台，查找类似的警告：
```
[ShizukuTypeRegistry] ShizukuFunction 'Add' in type 'MathUtils' is ignored because the type is not marked with [ShizukuClass]
```

根据警告信息逐个修复。

#### 步骤 3：重新生成节点

1. 打开 **Shizuku > Generator Window**
2. 切换到 **ShizukuClass & Function** 标签
3. 点击 **🔄 Refresh**
4. 找到你的类和函数，点击 **Generate** 或 **Regenerate**

---

## 📊 功能对比

| 功能 | 旧方案 | 新方案 |
|------|--------|--------|
| **Tab 数量** | 3 个（Blueprint / Function / VariableType） | 2 个（Blueprint / ShizukuClass & Function） |
| **Function 展示** | 按类型扁平化列表 | 按 ShizukuClass 层级展示 |
| **变量类型生成** | 独立 Tab | 集成在 ShizukuClass 下 |
| **ShizukuFunction 限制** | 无限制 | 必须在 ShizukuClass 中 |
| **UI 结构** | 分散 | 统一 |
| **代码一致性** | 较低 | 高 |

---

## ✅ 优势

### 1. **更清晰的代码结构**
- 强制 ShizukuFunction 在 ShizukuClass 中，避免孤立的函数
- 类和函数的关系一目了然

### 2. **更直观的 UI**
- 一个 Tab 管理所有 ShizukuClass 相关功能
- 层级展示使得组织结构更清晰

### 3. **统一的工作流**
- 一站式管理：类定义 → 函数节点生成 → 变量类型生成
- 减少在不同 Tab 间切换的麻烦

### 4. **更好的可维护性**
- 减少重复代码
- 统一的生成逻辑
- 便于后续扩展

---

## 🔧 技术细节

### UnifiedShizukuGeneratorTab 核心数据结构

```csharp
/// <summary>
/// ShizukuClass 条目
/// </summary>
private class ShizukuClassEntry
{
    public ShizukuClassInfo ClassInfo;              // 类信息
    public List<FunctionEntry> Functions;            // 该类的所有函数
    public bool SupportsVariableType;                // 是否支持生成变量类型
}

/// <summary>
/// 函数条目
/// </summary>
private class FunctionEntry
{
    public ShizukuFunctionInfo FunctionInfo;         // 函数信息
    public string NodeClassName;                     // 生成的节点类名
    public bool IsGenerated;                         // 是否已生成
}
```

### 判断是否支持变量类型生成

```csharp
// 非静态类且 ShowInVariableMenu 为 true
entry.SupportsVariableType = classInfo.ShowInVariableMenu 
                           && !classInfo.Type.IsAbstract 
                           && !classInfo.Type.IsStatic();
```

### TypeExtensions 工具类

```csharp
public static class TypeExtensions
{
    public static bool IsStatic(this Type type)
    {
        return type.IsAbstract && type.IsSealed;
    }
}
```

---

## 📋 待办事项

- [ ] 更新更多文档中的示例代码
- [ ] 考虑是否自动为旧代码添加 ShizukuClass（通过代码生成工具）
- [ ] 优化 UI 性能（如果类和函数数量非常多）
- [ ] 添加搜索和过滤功能

---

## 🐛 已知问题

无

---

## 💡 使用建议

### 1. **静态工具类**
对于纯静态工具类，设置 `ShowInVariableMenu = false`：
```csharp
[ShizukuClass("工具类名", "类别", ShowInVariableMenu = false)]
public static class MyUtils { ... }
```

### 2. **可实例化的类**
对于需要在蓝图中创建变量的类，保持默认设置：
```csharp
[ShizukuClass("数据类名", "类别")]  // ShowInVariableMenu 默认为 true
[Serializable]
public class MyData { ... }
```

### 3. **命名规范**
- 类别使用中文或英文均可，建议统一
- 显示名称应该简洁明了
- 描述应该说明用途和功能

---

## 📚 相关文档

- [Guide_ShizukuAttributes.md](./Guide_ShizukuAttributes.md) - ShizukuClass 和 ShizukuFunction 使用指南
- [ARCHITECTURE.md](./ARCHITECTURE.md) - 蓝图系统架构文档
- [ROADMAP.md](./ROADMAP.md) - 项目开发路线图

---

**文档结束** ✨

