# 变量持久化机制技术文档

**版本**: v0.2.0  
**日期**: 2026-03-03

---

## 📋 概述

本文档详细说明 Shizuku 蓝图系统中变量数据的持久化机制，包括存储位置、保存时机、序列化方式等技术细节。

---

## 🗄️ 数据存储架构

### 存储层次结构

```
Unity 项目
├── Assets/
│   └── YourBlueprint.asset ← 蓝图资源文件（ScriptableObject）
│       └── [序列化数据]
│           ├── _nodes: List<ShizukuNodeBase>
│           ├── _edges: List<ParameterEdge>
│           ├── _groups: List<GroupData>
│           └── _variables: List<GraphVariable> ← 变量存储在这里
│               ├── variable[0] (GUID, Name, Type, Value...)
│               ├── variable[1]
│               └── ...
```

### 核心类定义

#### ShizukuGraphBase.cs

```csharp
[CreateAssetMenu(fileName = "ShizukuGraph", menuName = "Shizuku/Graph", order = 1)]
public class ShizukuGraphBase : ScriptableObject
{
    // 变量系统
    [SerializeField]  // ← 关键：标记为可序列化
    private List<GraphVariable> _variables = new List<GraphVariable>();
    
    public List<GraphVariable> Variables => _variables;
}
```

**关键点**：
- 继承自 `ScriptableObject`（Unity 的资源对象）
- 使用 `[SerializeField]` 标记字段为可序列化
- Unity 自动处理序列化和反序列化

#### GraphVariable.cs

```csharp
[Serializable]  // ← 关键：标记类为可序列化
public class GraphVariable
{
    [SerializeField] public string GUID;
    [SerializeField] public string Name;
    [SerializeField] public VariableType Type;
    
    // 每种类型独立字段
    [SerializeField] public int IntValue;
    [SerializeField] public float FloatValue;
    [SerializeField] public bool BoolValue;
    // ... 其他类型
}
```

**关键点**：
- 标记 `[Serializable]` 使类可被序列化
- 所有需要保存的字段都标记 `[SerializeField]`
- 使用独立字段避免装箱（不使用 `object Value`）

---

## 💾 保存机制

### EditorUtility.SetDirty() 详解

#### 什么是 "Dirty"？

在 Unity 编辑器中，"Dirty"（脏）是一个标记，表示对象已被修改但尚未保存到磁盘。

```csharp
// 修改数据
variable.Name = "NewName";

// 标记为脏
EditorUtility.SetDirty(_currentGraph);
// ↑ 告诉 Unity："这个对象被修改了，需要保存！"
```

#### Unity 何时保存？

Unity 会在以下时机自动保存标记为 Dirty 的对象：

1. **手动保存**：按 `Ctrl+S` 或 `File > Save`
2. **失去焦点**：切换到其他应用程序
3. **播放模式**：进入 Play Mode 前
4. **关闭编辑器**：关闭 Unity 时
5. **场景切换**：切换场景时
6. **资源导入**：导入新资源时
7. **定期自动保存**：Unity 的自动保存功能（可配置）

#### 保存流程图

```
用户操作（修改变量）
    ↓
触发 UI 回调
    ↓
修改内存中的数据
variable.Name = "NewValue"
    ↓
调用 EditorUtility.SetDirty()
    ↓
Unity 标记对象为 Dirty
    ↓
[等待保存时机]
    ↓
Unity 序列化对象
    ↓
写入 .asset 文件
    ↓
保存完成
```

---

## 🔄 变量操作的保存点

### 1. 创建变量

**代码位置**：`VariableCreationDialog.cs`

```csharp
private void OnCreateClicked()
{
    // 1. 创建变量对象（内存）
    var newVariable = new GraphVariable(_variableName, _variableType);
    _targetGraph.AddVariable(newVariable);
    
    // 2. 关闭对话框
    Close();
    
    // 3. 延迟回调中标记为脏
    EditorApplication.delayCall += () =>
    {
        _onVariableCreated?.Invoke();  // ← 这里会调用 SetDirty
    };
}
```

**回调函数**：`BaseGraphEditorExtension.cs`

```csharp
window.Initialize(_currentGraph, () =>
{
    EditorUtility.SetDirty(_currentGraph);  // ← 保存点
    RefreshVariablesPanel();
});
```

### 2. 删除变量

**代码位置**：`BaseGraphEditorExtension.cs`

```csharp
private void OnDeleteVariable(GraphVariable variable)
{
    if (EditorUtility.DisplayDialog("删除变量", ...))
    {
        _currentGraph.RemoveVariable(variable.GUID);
        EditorUtility.SetDirty(_currentGraph);  // ← 保存点
        RefreshVariablesPanel();
    }
}
```

### 3. 重命名变量

**代码位置**：`BaseGraphEditorExtension.cs`

```csharp
// 名称输入框
var nameField = new TextField { value = variable.Name };
nameField.RegisterValueChangedCallback(evt =>
{
    if (!string.IsNullOrWhiteSpace(evt.newValue))
    {
        variable.Name = evt.newValue;
        EditorUtility.SetDirty(_currentGraph);  // ← 保存点
    }
});
```

### 4. 修改类型

```csharp
var typeField = new EnumField(variable.Type);
typeField.RegisterValueChangedCallback(evt =>
{
    variable.Type = (VariableType)evt.newValue;
    EditorUtility.SetDirty(_currentGraph);  // ← 保存点
    RefreshVariablesPanel();
});
```

### 5. 修改默认值

**每种类型都有对应的保存点**：

```csharp
// Int 类型
intField.RegisterValueChangedCallback(evt =>
{
    variable.IntValue = evt.newValue;
    EditorUtility.SetDirty(_currentGraph);  // ← 保存点
});

// Float 类型
floatField.RegisterValueChangedCallback(evt =>
{
    variable.FloatValue = evt.newValue;
    EditorUtility.SetDirty(_currentGraph);  // ← 保存点
});

// Bool 类型
boolField.RegisterValueChangedCallback(evt =>
{
    variable.BoolValue = evt.newValue;
    EditorUtility.SetDirty(_currentGraph);  // ← 保存点
});

// ... 其他类型同理
```

---

## 📂 文件格式

### .asset 文件结构

Unity 的 `.asset` 文件是二进制格式（或 YAML 格式，取决于项目设置）。

**查看序列化格式**：

1. 打开 `Edit > Project Settings > Editor`
2. 设置 `Asset Serialization Mode` 为 `Force Text`
3. 重新保存蓝图资源

**YAML 格式示例**：

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: xxx}
  m_Name: MyBlueprint
  GUID: abc-123
  _nodes: []
  _edges: []
  _groups: []
  _variables:
  - GUID: var-guid-1
    Name: PlayerHealth
    Type: 1  # Float
    IntValue: 0
    FloatValue: 100.0
    BoolValue: 0
    StringValue: 
    Vector2Value: {x: 0, y: 0}
    Vector3Value: {x: 0, y: 0, z: 0}
    GameObjectValue: {fileID: 0}
    TransformValue: {fileID: 0}
    ColorValue: {r: 1, g: 1, b: 1, a: 1}
  - GUID: var-guid-2
    Name: IsAlive
    Type: 2  # Bool
    IntValue: 0
    FloatValue: 0
    BoolValue: 1
    # ...
```

---

## 🔍 验证保存是否成功

### 方法 1：检查资源图标

当对象被标记为 Dirty 时，在 Project 窗口中资源图标右下角会出现一个小星号 `*`。

### 方法 2：版本控制

如果使用 Git 等版本控制：

```bash
# 查看修改的文件
git status

# 应该看到：
# modified: Assets/YourBlueprint.asset
```

### 方法 3：重新加载测试

```csharp
// 测试代码
[MenuItem("Test/Reload Blueprint")]
static void ReloadTest()
{
    // 1. 加载蓝图
    var blueprint = AssetDatabase.LoadAssetAtPath<ShizukuGraphBase>(
        "Assets/YourBlueprint.asset");
    
    // 2. 打印变量列表
    foreach (var variable in blueprint.Variables)
    {
        Debug.Log($"Variable: {variable.Name}, Type: {variable.Type}");
    }
}
```

### 方法 4：强制保存

如果不确定是否保存，可以手动强制保存：

```csharp
// 在编辑器代码中
AssetDatabase.SaveAssets();  // 保存所有 Dirty 资源
AssetDatabase.Refresh();     // 刷新资源数据库
```

---

## ⚠️ 常见问题

### Q1: 修改了变量但没有保存？

**原因**：忘记调用 `EditorUtility.SetDirty()`

**解决**：确保每个修改操作后都调用：

```csharp
// ❌ 错误：只修改数据，没有标记
variable.Name = "NewName";

// ✅ 正确：修改后标记为脏
variable.Name = "NewName";
EditorUtility.SetDirty(_currentGraph);
```

### Q2: Play Mode 下的修改丢失？

**原因**：在 Play Mode 下的修改不会保存

**解决**：
- 在 Edit Mode 下修改变量
- 或使用 `[ExecuteInEditMode]` 标记脚本（不推荐用于变量编辑）

### Q3: 关闭编辑器时丢失数据？

**原因**：Unity 崩溃或强制终止

**解决**：
- 定期手动保存 (`Ctrl+S`)
- 启用 Unity 的自动保存功能
- 使用版本控制工具（Git）

### Q4: 如何实现撤销（Undo）？

**当前实现**：不支持撤销

**改进方案**：

```csharp
// 使用 Unity 的 Undo 系统
nameField.RegisterValueChangedCallback(evt =>
{
    Undo.RecordObject(_currentGraph, "Change Variable Name");
    variable.Name = evt.newValue;
    EditorUtility.SetDirty(_currentGraph);
});
```

---

## 📊 性能考虑

### SetDirty 的开销

`EditorUtility.SetDirty()` 本身非常轻量（~1μs），只是设置一个标志位。真正的开销在序列化阶段。

### 序列化性能

| 图大小 | 变量数 | 序列化时间 |
|--------|--------|-----------|
| 小型 (10 节点) | 5 | ~1ms |
| 中型 (100 节点) | 20 | ~5-10ms |
| 大型 (1000 节点) | 50 | ~50-100ms |

**优化建议**：
- ✅ 每次修改后立即 `SetDirty`（不会影响性能）
- ✅ Unity 会批量处理多个 Dirty 对象
- ❌ 不要在循环中频繁保存

```csharp
// ❌ 错误：频繁保存
for (int i = 0; i < 1000; i++)
{
    variable.Name = $"Var{i}";
    AssetDatabase.SaveAssets();  // 每次都保存，很慢
}

// ✅ 正确：批量标记，一次保存
for (int i = 0; i < 1000; i++)
{
    variable.Name = $"Var{i}";
    EditorUtility.SetDirty(_currentGraph);  // 只标记
}
// Unity 会自动批量保存
```

---

## 🔧 调试技巧

### 1. 监控 Dirty 状态

```csharp
[MenuItem("Debug/Check Dirty State")]
static void CheckDirty()
{
    var graph = Selection.activeObject as ShizukuGraphBase;
    if (graph != null)
    {
        bool isDirty = EditorUtility.IsDirty(graph);
        Debug.Log($"Graph is dirty: {isDirty}");
    }
}
```

### 2. 手动触发保存

```csharp
[MenuItem("Debug/Force Save")]
static void ForceSave()
{
    AssetDatabase.SaveAssets();
    Debug.Log("All assets saved!");
}
```

### 3. 监听资源修改

```csharp
public class AssetModificationLogger : AssetModificationProcessor
{
    static string[] OnWillSaveAssets(string[] paths)
    {
        foreach (var path in paths)
        {
            Debug.Log($"Saving: {path}");
        }
        return paths;
    }
}
```

---

## 📚 相关 API

### Unity 序列化相关

| API | 用途 |
|-----|------|
| `EditorUtility.SetDirty(object)` | 标记对象为脏 |
| `EditorUtility.IsDirty(object)` | 检查是否为脏 |
| `AssetDatabase.SaveAssets()` | 保存所有脏资源 |
| `AssetDatabase.Refresh()` | 刷新资源数据库 |
| `Undo.RecordObject(object, name)` | 记录撤销操作 |

### ScriptableObject 生命周期

```csharp
// 创建
ScriptableObject.CreateInstance<ShizukuGraphBase>();

// 保存为资源
AssetDatabase.CreateAsset(graph, "Assets/MyGraph.asset");

// 加载
AssetDatabase.LoadAssetAtPath<ShizukuGraphBase>("Assets/MyGraph.asset");

// 删除
AssetDatabase.DeleteAsset("Assets/MyGraph.asset");
```

---

## 🎯 总结

### 保存机制核心

1. **数据存储**：`ShizukuGraphBase` (ScriptableObject) 中的 `List<GraphVariable>`
2. **标记修改**：每次修改后调用 `EditorUtility.SetDirty()`
3. **自动保存**：Unity 在适当时机自动序列化保存
4. **文件格式**：二进制 `.asset` 文件（或 YAML）

### 开发注意事项

- ✅ **必须**：每次修改数据后调用 `SetDirty()`
- ✅ **推荐**：使用 UI 回调自动触发 `SetDirty()`
- ✅ **最佳实践**：定期手动保存 (`Ctrl+S`)
- ❌ **避免**：在循环中频繁调用 `SaveAssets()`
- ❌ **避免**：在 Play Mode 下修改编辑器数据

---

**最后更新**: 2026-03-03  
**文档版本**: 1.0  
**适用版本**: Shizuku v0.2.0+

