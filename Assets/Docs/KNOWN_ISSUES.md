# Shizuku 框架已知问题 - 性能与错误处理

本文档记录当前框架在**性能优化**和**运行时错误处理**方面的已知问题和改进计划。

---

## ⚡ 性能问题

### 1. BlueprintBehavior 每帧无条件执行 Root Node

#### 问题描述

**当前实现**：
```csharp
// BlueprintBehavior.cs
private void Update()
{
    if (_blueprint != null)
    {
        _blueprint.Update();
    }
}
```

**问题**：
- 即使蓝图中没有任何逻辑，也会每帧执行一次图遍历
- 大量实例时性能开销显著
- 100 个 BlueprintBehavior 实例 = 每帧 100 次图执行

#### 改进方案

**方案 A：UpdateMode 枚举**（推荐）

```csharp
public enum BlueprintUpdateMode
{
    Disabled,      // 完全不执行（默认）
    Manual,        // 手动调用 ExecuteBlueprint()
    Always,        // 每帧执行（兼容当前行为）
    OnDemand       // 智能检测：有 Root 逻辑时才执行
}
```

**方案 B：智能检测** — 在 `InitializeBehavior` 时检测 Root Node 是否有连接。

**方案 C：事件驱动** — 完全移除 Update 中的自动执行，通过事件系统触发。

#### 实施计划

- **版本**：v0.2.0
- **优先级**：⭐⭐⭐ 高
- **推荐方案**：方案 A（灵活性最高，向后兼容）

---

### 2. 静态缓存的生命周期问题

#### 问题描述

```csharp
// ShizukuBluePrint.cs
private static Dictionary<string, Func<T, object>> _cachedGetters;
private static Dictionary<string, Action<T, object>> _cachedSetters;
private static bool _accessorsCached = false;
```

- 静态缓存在整个应用生命周期内存在
- 即使蓝图资源被卸载，缓存仍然占用内存
- 编辑器模式下频繁切换蓝图会累积缓存

#### 改进方案

**推荐：手动清理接口**
```csharp
public static void ClearStaticCache()
{
    _cachedGetters?.Clear();
    _cachedSetters?.Clear();
    _accessorsCached = false;
}
```

#### 实施计划

- **版本**：v0.3.0 或更晚
- **优先级**：⭐ 低（目前影响有限）

---

### 3. 图初始化时的反射性能开销

#### 问题描述

运行时有 **3 处**反射/低效调用在每次图初始化时触发：

**① `ShizukuNodeBase.Init()` — 反射发现 ParameterEdgePort 字段**

```csharp
public virtual void Init(INodeContext context)
{
    _context = context;
    // ❌ 每个节点初始化时都要反射所有字段，无缓存
    var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    
    foreach (var field in fields)
    {
        if (typeof(ParameterEdgePort).IsAssignableFrom(field.FieldType))
        {
            var port = field.GetValue(this) as ParameterEdgePort;  // ❌ 反射获取值
            // ... 分类到 SelfOutputPorts / SelfInputPorts
        }
    }
}
```

**② `ShizukuNormalNode.Init()` — 反射发现 ChainPort 字段**

```csharp
public override void Init(INodeContext context)
{
    base.Init(context);  // 已经反射了一次 ParameterEdgePort
    ChainPorts.Clear();
    var type = GetType();
    while (type != null && type != typeof(object))
    {
        // ❌ 又一次反射，遍历继承链查找 ChainPort
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public
            | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        foreach (var field in fields)
        {
            if (typeof(ChainPort).IsAssignableFrom(field.FieldType))
            {
                var port = field.GetValue(this) as ChainPort;
                if (port != null) ChainPorts[port.Name] = port;
            }
        }
        type = type.BaseType;
    }
}
```

**③ `ParameterEdge.ConnectPorts()` — `List.Find()` 线性查找节点**

```csharp
public void ConnectPorts(ShizukuGraphBase graph)
{
    // ❌ List.Find() O(n) 查找，而 Guid2NodeMap 字典已存在却未使用
    var outputNode = graph.Nodes.Find(n => n.GUID == OutputNodeGuid);
    var inputNode = graph.Nodes.Find(n => n.GUID == InputNodeGuid);
    // ...
}
```

#### 性能数据

| 操作 | 当前实现 | 影响 |
|------|---------|------|
| 单节点 `Init()`（含两次反射） | ~50-100μs | 反射字段 |
| 100 节点 `Init()` | ~5-10ms | 累积反射开销 |
| 单条边 `ConnectPorts()` | ~20-50μs | List.Find 查找 |
| 200 条边连接 | ~4-10ms | 累积查找开销 |
| **总初始化时间** | **~10-20ms** | 每次加载都要执行 |

#### 改进方案

**方案 A：静态字段缓存（推荐）**

为 `ShizukuNodeBase` 和 `ShizukuNormalNode` 添加 `static Dictionary<Type, FieldInfo[]>` 缓存，同类型节点只反射一次。与编辑器端 `ShizukuNodeView.GetCachedFields` 完全相同的模式。

```csharp
private static readonly Dictionary<Type, List<FieldInfo>> _portFieldCache = new();

public virtual void Init(INodeContext context)
{
    _context = context;
    var nodeType = GetType();
    if (!_portFieldCache.TryGetValue(nodeType, out var portFields))
    {
        portFields = new List<FieldInfo>();
        var type = nodeType;
        while (type != null && type != typeof(object))
        {
            foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.Public
                | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (typeof(ParameterEdgePort).IsAssignableFrom(f.FieldType))
                    portFields.Add(f);
            }
            type = type.BaseType;
        }
        _portFieldCache[nodeType] = portFields;
    }
    // 使用缓存的 portFields ...
}
```

**方案 B：`ConnectPorts` 使用已有的 `Guid2NodeMap` 字典**

```csharp
public void ConnectPorts(INodeContext context)
{
    // ✅ O(1) 字典查找替代 O(n) List.Find
    if (!context.Guid2NodeMap.TryGetValue(OutputNodeGuid, out var outputNode) ||
        !context.Guid2NodeMap.TryGetValue(InputNodeGuid, out var inputNode))
        return;
    // ...
}
```

**方案 C：端口预注册（最优，需重构所有节点类）** — 构造函数中 `RegisterPort()`，完全消除反射。

#### 实施计划

- **版本**：v0.3.0
- **优先级**：⭐⭐ 中高
- **推荐方案**：方案 A + 方案 B（2-3 天工作量）
- **预期收益**：图初始化 10-20ms → 2-4ms（3-5x）

---

### 4. BlueprintEventNode 验证方法中的反射未加编辑器保护

#### 问题描述

```csharp
// BlueprintEventNode.cs
public override Color TitleBarColor => IsValid()
    ? new Color(1f, 0.3f, 0.3f, 1f)
    : new Color(0.8f, 0.4f, 0f, 1f);

private MethodInfo FindMatchingMethod()
{
    var behaviorType = GetBehaviorType();
    // ❌ 运行时也可能触发的重量级反射
    var methods = behaviorType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    foreach (var method in methods)
    {
        var attr = method.GetCustomAttribute<BlueprintOverridableAttribute>();
        // ...
    }
    return null;
}
```

**问题**：
- `IsValid()`、`GetValidationMessage()`、`FindMatchingMethod()` 仅用于编辑器 UI 显示
- 但代码位于 Runtime 程序集，没有 `#if UNITY_EDITOR` 保护
- 运行时访问 `TitleBarColor` 会触发 `GetMethods` + `GetCustomAttribute` 重量级反射

#### 改进方案

**推荐：`#if UNITY_EDITOR` 保护**

```csharp
public override Color TitleBarColor =>
#if UNITY_EDITOR
    IsValid() ? new Color(1f, 0.3f, 0.3f, 1f) : new Color(0.8f, 0.4f, 0f, 1f);
#else
    new Color(1f, 0.3f, 0.3f, 1f);
#endif
```

#### 实施计划

- **版本**：v0.2.0
- **优先级**：⭐⭐ 中
- **工作量**：0.5 天

---
## 🐛 运行时错误处理问题

### 1. 节点执行错误信息不友好

#### 问题描述

节点执行出错时，用户只能看到原始异常堆栈，缺少关键上下文信息：
- ❌ 哪个 GameObject / Behavior / 蓝图资源
- ❌ 哪个节点（类型、GUID）
- ❌ 执行路径

#### 改进方案

**推荐：结构化错误上下文 + 错误码系统**

```csharp
public class ShizukuExecutionContext
{
    public GameObject GameObject { get; set; }
    public string BehaviorType { get; set; }
    public string BlueprintAssetPath { get; set; }
    public string CurrentNodeGUID { get; set; }
    public string CurrentNodeType { get; set; }
    public Stack<string> ExecutionPath { get; set; }
}
```

#### 实施计划

- **版本**：v0.2.0
- **优先级**：⭐⭐⭐ 高

---

### 2. 缺少错误恢复机制

#### 问题描述

- 节点执行出错时，整个执行链中断
- 没有"遇错继续"的选项
- 无法记录错误节点用于后续分析

#### 改进方案

```csharp
public enum ErrorHandlingMode
{
    StopOnError,      // 遇错停止（默认）
    ContinueOnError,  // 遇错继续
    UseDefaultValue   // 使用默认值继续
}
```

#### 实施计划

- **版本**：v0.2.0 或 v0.3.0
- **优先级**：⭐⭐ 中高

---

### 3. 缺少循环依赖的运行时保护

#### 问题描述

- 编辑器中有循环检测（`HasCycle`），但运行时没有保护
- 序列化数据损坏可能导致无限递归 → Stack Overflow

#### 改进方案

```csharp
private static HashSet<string> _executingNodes = new HashSet<string>();

public void Execute()
{
    if (_executingNodes.Contains(this.GUID))
    {
        Debug.LogError($"检测到循环依赖: {this.Title} ({this.GUID})");
        return;
    }
    _executingNodes.Add(this.GUID);
    try { GetInputValues(); OnExecute(); }
    finally { _executingNodes.Remove(this.GUID); }
}
```

#### 实施计划

- **版本**：v0.2.0
- **优先级**：⭐⭐ 中
- **开销**：HashSet ~O(1)，可忽略
