# 属性节点使用指南

## 📖 概述

属性节点允许蓝图读取和修改 `BlueprintBehavior` 中的字段和属性值。

## 🎯 节点类型

### 获取属性节点（Get Property）

**用途**：从 Behavior 中读取属性值

**节点类型**：
- `GetPropertyNode_Float` - 获取 float 类型属性
- `GetPropertyNode_Int` - 获取 int 类型属性
- `GetPropertyNode_Bool` - 获取 bool 类型属性
- `GetPropertyNode_String` - 获取 string 类型属性
- `GetPropertyNode` - 获取任意类型属性（通用版本）

**特点**：
- ✅ 无控制流端口（纯数据节点）
- ✅ 一个输出端口（Value）
- ✅ 配置 `PropertyName` 指定要读取的属性
- ✅ 支持自动类型转换

### 设置属性节点（Set Property）

**用途**：向 Behavior 中写入属性值

**节点类型**：
- `SetPropertyNode_Float` - 设置 float 类型属性
- `SetPropertyNode_Int` - 设置 int 类型属性
- `SetPropertyNode_Bool` - 设置 bool 类型属性
- `SetPropertyNode_String` - 设置 string 类型属性
- `SetPropertyNode` - 设置任意类型属性（通用版本）

**特点**：
- ✅ 有控制流端口（Previous/Next）
- ✅ 一个输入端口（Value）
- ✅ 配置 `PropertyName` 指定要写入的属性
- ✅ 支持自动类型转换

---

## 💡 使用示例

### 示例 1：读取生命值并判断

```
[Event: TakeDamage] → [Get health (Float)] → [比较 < 20] → [是?]
                                                               ├─ 是 → [播放低血警告]
                                                               └─ 否 → [播放普通受击]
```

**步骤**：
1. 创建 `BlueprintEventNode`，设置 `EventName = "TakeDamage"`
2. 创建 `GetPropertyNode_Float`，设置 `PropertyName = "health"`
3. 连接到比较节点和条件节点
4. 根据结果触发不同的逻辑

### 示例 2：修改速度

```
[Event: OnSpeedBoost] → [Get speed (Float)] → [乘以 2] → [Set speed (Float)] → [Log "Speed boosted!"]
```

**步骤**：
1. 创建事件节点
2. 获取当前速度
3. 计算新速度
4. 写回速度属性
5. 输出日志

### 示例 3：检查存活状态

```
[Root Node] → [Get health (Float)] → [> 0?] → [是?]
                                               ├─ 是 → [继续 AI 逻辑]
                                               └─ 否 → [播放死亡动画]
```

---

## 🔧 配置说明

### 在 Behavior 中定义属性

```csharp
public class EnemyBehavior : BlueprintBehavior<EnemyBehavior>
{
    // ✅ [SerializeField] 字段会自动注册
    [SerializeField] protected float health = 100f;
    [SerializeField] protected float speed = 5f;
    [SerializeField] protected bool isAlive = true;
    
    // ✅ public 属性也会自动注册
    public int Level { get; set; } = 1;
    
    // ❌ private 字段默认不会注册
    private int secretValue = 42;
}
```

### 在编辑器中使用

1. **创建节点**：
   - 右键 → `蓝图节点` → `获取属性` / `设置属性`
   - 选择对应的类型（Float/Int/Bool/String）

2. **配置属性名**：
   - 在节点的 Inspector 中设置 `PropertyName`
   - 输入 Behavior 中定义的字段或属性名（区分大小写）
   - 例如：`"health"`, `"speed"`, `"Level"`

3. **连接端口**：
   - Get 节点：连接输出端口到其他节点的输入
   - Set 节点：连接输入端口接收值，连接控制流执行写入

---

## ⚠️ 注意事项

### 1. 属性必须已注册

```csharp
// ✅ 这些会自动注册（通过反射）
[SerializeField] protected float health;
public int Level { get; set; }

// ❌ private 字段需要手动注册
private int secretValue;

// 手动注册方式（在 ShizukuBluePrint 子类中）：
public override void InitializeBehavior(EnemyBehavior behavior)
{
    base.InitializeBehavior(behavior);
    behavior.RegisterPropertyGetter("secretValue", () => secretValue);
}
```

### 2. 属性名大小写敏感

```csharp
[SerializeField] protected float health;  // ✅

// 节点中配置：
PropertyName = "health"   // ✅ 正确
PropertyName = "Health"   // ❌ 错误（大小写不匹配）
```

### 3. 类型转换

- 强类型节点会自动转换类型
- 如果转换失败，会输出警告并使用默认值
- 建议使用与 Behavior 中定义的类型匹配的节点

```csharp
[SerializeField] protected float health = 100f;

// ✅ 推荐：使用 GetPropertyNode_Float
// ⚠️ 可行：使用 GetPropertyNode_Int（会转换为 100）
// ⚠️ 可行：使用 GetPropertyNode（返回 object，需要手动转换）
```

### 4. 运行时检查

如果属性获取失败，会输出警告日志：
```
GetPropertyNode: Property 'health' not found or Behavior not initialized
```

可能的原因：
- 属性名拼写错误
- 属性未注册
- Behavior 未初始化

---

## 🎨 扩展自定义类型

得益于泛型设计，扩展新类型非常简单！只需 3 步：

### 步骤 1：定义端口类型（如果还没有）

```csharp
[Serializable]
public class Vector3ParameterEdgePort : ParameterEdgePort<Vector3>
{
}
```

### 步骤 2：创建 Get 节点

```csharp
[Serializable]
public class GetPropertyNode_Vector3 : PropertyGetNode<Vector3ParameterEdgePort>
{
    protected override string TypeName => "Vector3";
    
    protected override object ConvertValue(object value)
    {
        if (value is Vector3 v3)
            return v3;
        throw new InvalidCastException($"Cannot convert {value?.GetType()} to Vector3");
    }
    
    protected override object GetDefaultValue() => Vector3.zero;
}
```

### 步骤 3：创建 Set 节点

```csharp
[Serializable]
public class SetPropertyNode_Vector3 : PropertySetNode<Vector3ParameterEdgePort>
{
    protected override string TypeName => "Vector3";
}
```

就这么简单！泛型基类 `PropertyGetNode<TPort>` 和 `PropertySetNode<TPort>` 已经处理了所有通用逻辑，你只需要提供类型名称和转换逻辑即可。

### 📦 代码结构

所有属性节点都在 **单个文件** `PropertyNodes.cs` 中定义：
- **泛型基类**：`PropertyGetNode<TPort>`, `PropertySetNode<TPort>`
- **强类型节点**：Float/Int/Bool/String 版本
- **通用节点**：`GetPropertyNode`, `SetPropertyNode`（支持 object 类型）

这种单文件设计让所有相关代码集中管理，便于维护和扩展。

---

## 📚 相关文档

- [快速参考](../../../Docs/QUICK_REFERENCE.md)
- [架构设计](../../../Docs/ARCHITECTURE.md)
- [节点开发指南](../../../Docs/NODE_DEVELOPMENT.md)

---

**版本**：v0.2.0  
**最后更新**：2026-01-25
