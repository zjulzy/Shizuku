# Shizuku 快速参考

## 🎯 5分钟上手

### 1. 创建蓝图
```
Assets 右键 → Create → Shizuku → Graph
```

### 2. 编辑蓝图
双击资源 → 右键创建节点 → 连接端口 → 保存

### 3. 运行蓝图
```csharp
public class GraphRunner : MonoBehaviour
{
    public ShizukuGraphBase GraphAsset;
    void Start() => GraphAsset.Init();
    void Update() => GraphAsset.Update();
}
```

---

## 📋 节点模板速查

### 基础节点
```csharp
public class MyNode : ShizukuNodeBase
{
    public override string Title => "节点名";
    public override Color TitleBarColor => Color.cyan;
    
    [SerializeReference]
    private IntParameterEdgePort _input = new() { IsOut = false, Name = "输入" };
    
    [SerializeReference]
    private IntParameterEdgePort _output = new() { IsOut = true, Name = "输出" };
    
    [SerializeField]
    private ChainPort _next = new() { Name = "next" };
    
    protected override void OnExecute()
    {
        _output.Value = _input.Value * 2;
    }
    
    protected override bool OnSelectNextNode(out string guid)
    {
        guid = _next.NextNodeGuid;
        return !string.IsNullOrEmpty(guid);
    }
}
```

### 分支节点
```csharp
public override bool SupportControlOutput => true;  // 多个 ChainPort

[SerializeField]
private ChainPort _truePort = new() { Name = "True" };
[SerializeField]
private ChainPort _falsePort = new() { Name = "False" };

protected override bool OnSelectNextNode(out string guid)
{
    guid = _condition.Value ? _truePort.NextNodeGuid : _falsePort.NextNodeGuid;
    return !string.IsNullOrEmpty(guid);
}
```

### 入口节点
```csharp
public override bool SupportControlInput => false;  // 无 Previous 端口
```

---

## 🔌 端口类型速查

### 内置类型
| 类型 | 类名 |
|-----|------|
| int | `IntParameterEdgePort` |
| float | `FloatParameterEdgePort` |
| bool | `BoolParameterEdgePort` |
| string | `StringParameterEdgePort` |

### 自定义类型
```csharp
[Serializable]
public class Vector3ParameterEdgePort : ParameterEdgePort<Vector3> { }
```

---

## 🎭 蓝图组件速查 (v0.2.0)

### 定义 Behavior（CRTP 模式）
```csharp
// ✅ 使用自引用泛型，类型安全！
public class EnemyBehavior : BlueprintBehavior<EnemyBehavior>
{
    [SerializeField] protected float health = 100f;
    [SerializeField] protected float defense = 10f;
    
    // 方式1：使用 [BlueprintOverridable]（推荐）
    [BlueprintOverridable]
    public virtual void TakeDamage(float damage)
    {
        // 自动检查蓝图实现（带缓存，后续调用仅 ~1μs）
        if (TryExecuteBlueprintOverride(nameof(TakeDamage), damage))
            return;
        
        // 默认 C# 逻辑（仅在蓝图未实现时执行）
        health -= Mathf.Max(0, damage - defense);
        if (health <= 0) OnDeath();
    }
    
    // 方式2：传统方式（更灵活）
    public virtual void Heal(float amount)
    {
        if (ExecuteBlueprintEvent("OnHeal", amount))
            return;
        
        health = Mathf.Min(100, health + amount);
    }
}
```

### 定义蓝图类
```csharp
[CreateAssetMenu(fileName = "EnemyBlueprint", menuName = "Shizuku/Blueprint/Enemy")]
public class EnemyBlueprint : ShizukuBluePrint<EnemyBehavior>
{
    // 通常不需要重写，基类已经处理好一切：
    // - 自动注册事件处理器
    // - 自动注册属性访问器（带静态缓存优化）
}
```

---

## 🎪 事件系统速查

### C# 触发事件
```csharp
// 推荐方式：通过 Behavior 方法（自动检查蓝图）
enemy.TakeDamage(20f);  // ← 最符合直觉

// 直接触发事件
ExecuteBlueprintEvent("TakeDamage", 20f);

// 通过蓝图对象触发
blueprint.TriggerBlueprintEvent("TakeDamage", 20f);
```

### 蓝图接收事件
1. 右键创建 `BlueprintEventNode`
2. 设置 `EventName = "TakeDamage"`（必须与 C# 中一致）
3. 配置参数：
   - `Name = "damage"`
   - `TypeName = "Single"` (float)
4. 连接后续节点（扣血、播放动画、检查死亡等）

### Root Node 执行模式
```
每帧自动执行（通过 BlueprintBehavior.Update）
适用场景：AI 巡逻、距离检测、状态机更新

[Root Node] → [获取玩家距离] → [距离 < 10?]
                                   ├─ 是 → [追击玩家]
                                   └─ 否 → [继续巡逻]
```

---

## 🔧 属性访问速查

### 自动访问
```csharp
// 所有 [SerializeField] 字段自动可访问
[SerializeField] protected float health;
```

### 手动注册
```csharp
public override void InitializeBehavior(MyBehavior behavior)
{
    base.InitializeBehavior(behavior);
    
    // 只读属性
    behavior.RegisterReadOnlyProperty("IsAlive", () => health > 0);
    
    // 读写属性
    behavior.RegisterProperty(
        "Level",
        () => level,
        (v) => level = (int)v
    );
}
```

---

## 🐛 调试技巧

### 添加日志
```csharp
protected override void OnExecute()
{
    Debug.Log($"节点 {Title} 执行: 输入={_input.Value}");
    // ...
}
```

### 查看端口值
在 Inspector 中选中节点，查看端口的 Value 字段。

### 检查连接
```csharp
// 打印依赖节点
Debug.Log($"依赖节点数量: {DependentNodes.Count}");
foreach (var node in DependentNodes)
    Debug.Log($"  - {node.Title}");
```

---

## ⚠️ 常见陷阱

### ❌ 忘记标记 SerializeReference
```csharp
// 错误
private IntParameterEdgePort _input;  // 不会序列化！

// 正确
[SerializeReference]
private IntParameterEdgePort _input = new() { ... };
```

### ❌ ChainPort 未初始化
```csharp
// 错误
private ChainPort _next;  // null！

// 正确
[SerializeField]
private ChainPort _next = new() { Name = "next" };
```

### ❌ 循环依赖
```
A → B → C → A  // 系统会阻止创建
```

### ❌ 端口名称重复
```csharp
// 错误：两个端口同名
private IntParameterEdgePort _input1 = new() { Name = "input" };
private FloatParameterEdgePort _input2 = new() { Name = "input" };
```

---

## 🎨 颜色约定

```csharp
// 入口节点（红色）
public override Color TitleBarColor => new Color(0.8f, 0.2f, 0.2f);

// 控制流节点（青色）
public override Color TitleBarColor => Color.cyan;

// 分支节点（黄色）
public override Color TitleBarColor => Color.yellow;

// 工具节点（灰色）
public override Color TitleBarColor => Color.gray;
```

---

## 📊 性能建议

| 场景 | 建议 |
|-----|------|
| 每帧执行 | 保持 Root 节点逻辑简单 |
| 频繁计算 | 缓存结果，避免重复计算 |
| 复杂逻辑 | 考虑在 C# 中实现 |
| 大量节点 | 使用分组管理，优化视图 |

---

## 🔗 重要链接

- [完整文档](../README.md)
- [架构设计](ARCHITECTURE.md)
- [开发路线图](ROADMAP.md)
- [项目结构](PROJECT_STRUCTURE.md)
