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
    // 每帧无条件执行 Root Node
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

#### 性能数据

| 场景 | 实例数 | 帧时间（无逻辑） | 帧时间（有逻辑） |
|------|--------|------------------|------------------|
| 当前实现 | 1 | ~0.01ms | ~0.05-0.5ms |
| 当前实现 | 100 | ~1ms | ~5-50ms |
| 理想实现 | 100 | ~0.01ms | ~5-50ms |

**对比 UE 蓝图**：
- UE 的 Event Tick 是可选的
- 如果不添加 Event Tick 节点，蓝图不会每帧执行
- 只有需要的时候才启用

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

public abstract class BlueprintBehavior<T> : MonoBehaviour
{
    [SerializeField]
    protected BlueprintUpdateMode updateMode = BlueprintUpdateMode.Disabled;
    
    private void Update()
    {
        if (updateMode == BlueprintUpdateMode.Always && _blueprint != null)
        {
            _blueprint.Update();
        }
    }
    
    // 手动执行接口
    public void ExecuteBlueprint()
    {
        _blueprint?.Update();
    }
}
```

**方案 B：智能检测**

```csharp
// 在 InitializeBehavior 时检测 Root Node 是否有连接
public virtual void InitializeBehavior(T behavior)
{
    Init();
    _behavior = behavior;
    
    // 检测是否需要每帧执行
    _needsUpdate = HasRootNodeLogic();
    
    // ...
}

private bool HasRootNodeLogic()
{
    if (string.IsNullOrEmpty(RootNodeGUID))
        return false;
    
    if (Guid2NodeMap.TryGetValue(RootNodeGUID, out var rootNode))
    {
        if (rootNode is ShizukuRootNode root)
        {
            // 检查是否有连接的节点
            return !string.IsNullOrEmpty(root._nextPort?.NextNodeGuid);
        }
    }
    return false;
}
```

**方案 C：事件驱动**

```csharp
// 完全移除 Update 中的自动执行
// 通过事件系统触发蓝图执行

[BlueprintOverridable]
protected virtual void OnCustomEvent()
{
    TryExecuteBlueprintOverride(nameof(OnCustomEvent));
}
```

#### 实施计划

- **版本**：v0.2.0
- **优先级**：⭐⭐⭐ 高
- **推荐方案**：方案 A（灵活性最高，向后兼容）
- **预期收益**：
  - 无逻辑场景：性能提升 ~100x
  - 有逻辑场景：无影响
  - 用户可根据需求选择模式

---

### 2. 值节点重复计算问题

#### 问题描述

**当前实现**：
```csharp
// ShizukuValueNode.GetOutputValues()
public override void GetOutputValues()
{
    // 每次调用都重新计算，没有缓存
    OnComputeOutputValues();
}
```

**场景**：
```
      [FloatValue: 5]
         /    |    \
        /     |     \
    [Add]  [Multiply] [Compare]
```

同一个 `FloatValue` 节点在同一帧内被 3 个节点依赖：
- 每个依赖节点都调用 `GetOutputValues()`
- 同样的值计算了 3 次
- 复杂的值节点（如查询数据库）开销更大

#### 改进方案

**帧缓存机制**：

```csharp
public abstract class ShizukuValueNode : ShizukuNodeBase
{
    [NonSerialized]
    private int _cachedFrame = -1;
    
    public override void GetOutputValues()
    {
        // 检查缓存
        if (_cachedFrame == Time.frameCount)
            return;  // 使用缓存的值
        
        // 重新计算
        OnComputeOutputValues();
        _cachedFrame = Time.frameCount;
    }
    
    protected abstract void OnComputeOutputValues();
}
```

**性能提升**：

| 场景 | 依赖数 | 当前 | 优化后 | 提升 |
|------|--------|------|--------|------|
| 简单节点（常量） | 3 | 3 次 | 1 次 | 3x |
| 复杂节点（查询） | 10 | 10 次 | 1 次 | 10x |

#### 实施计划

- **版本**：v0.2.0
- **优先级**：⭐⭐⭐ 高
- **预期收益**：大幅减少重复计算，提升复杂图性能

---

### 3. 静态缓存的生命周期问题

#### 问题描述

**当前实现**：
```csharp
// ShizukuBluePrint.cs
private static Dictionary<string, Func<T, object>> _cachedGetters;
private static Dictionary<string, Action<T, object>> _cachedSetters;
private static bool _accessorsCached = false;
```

**问题**：
- 静态缓存在整个应用生命周期内存在
- 即使蓝图资源被卸载，缓存仍然占用内存
- 编辑器模式下频繁切换蓝图会累积缓存

#### 影响

| 操作 | 内存占用 |
|------|----------|
| 加载 1 个蓝图类型 | ~1-5KB |
| 加载 100 个蓝图类型 | ~100-500KB |
| 编辑器模式频繁切换 | 持续增长 |

**风险**：
- 正常使用下影响较小
- 但长时间运行或大型项目可能累积

#### 改进方案

**方案 A：弱引用缓存**
```csharp
private static ConditionalWeakTable<Type, AccessorCache> _typeCache;
```

**方案 B：手动清理接口**
```csharp
public static void ClearStaticCache()
{
    _cachedGetters?.Clear();
    _cachedSetters?.Clear();
    _accessorsCached = false;
}
```

**方案 C：实例化缓存**（重构）
```csharp
// 不使用静态缓存，改为实例缓存
private Dictionary<string, Func<T, object>> _instanceGetters;
```

#### 实施计划

- **版本**：v0.3.0 或更晚
- **优先级**：⭐ 低（目前影响有限）
- **推荐方案**：方案 B（简单，适合编辑器使用）

---

### 4. 数据类型传递的装箱开销

#### 问题描述

**当前实现**：
```csharp
// ParameterEdgePort.cs
public override void GetSourceValue()
{
    if (DifferentTypeConnectedPort != null)
    {
        // ❌ 装箱：float → object
        var differentValue = DifferentTypeConnectedPort.GetSelfValue();
        
        // ❌ 反射 + 拆箱：object → int
        Value = (T)Convert.ChangeType(differentValue, typeof(T));
    }
}

public override object GetSelfValue()
{
    return Value;  // ❌ 值类型装箱
}
```

**场景**：
```
[FloatNode: 5.0f] → [IntNode]
                      ↓
                   GetSelfValue() 装箱 (float → object)
                      ↓
       Convert.ChangeType() 拆箱 + 转换 (object → int)
```

#### 性能数据

| 操作 | 耗时 | GC 分配 |
|------|------|---------|
| 同类型赋值 | ~1ns | 0 |
| 异类型转换（当前） | ~50-100ns | 8-16 字节/次 |
| 频繁调用（1000次/帧） | ~0.05-0.1ms | ~8-16KB/帧 |

**影响**：
- 单次影响：微秒级（可忽略）
- 大量节点：GC 压力增加
- 复杂图：累积效应明显

#### Convert.ChangeType 的限制

```csharp
// ✅ 支持基础类型
int i = 5;
float f = (float)Convert.ChangeType(i, typeof(float));  // OK

// ❌ 不支持 Unity 类型
Vector3 v3 = new Vector3(1, 2, 3);
Vector2 v2 = (Vector2)Convert.ChangeType(v3, typeof(Vector2));  // 💥 异常

// ❌ 不支持自定义转换
MyCustomType custom = ...;
int value = (int)Convert.ChangeType(custom, typeof(int));  // 💥 异常
```

#### 改进方案

**🏆 推荐方案：显式类型转换节点（类似 UE 蓝图）**

将类型转换表现为可见的节点，而不是隐式的端口转换：

```csharp
// 连接行为：
用户拖动：[FloatNode] → [IntNode]
系统检测：Float ≠ Int，但可转换
自动插入：[FloatNode] → [Float→Int转换节点] → [IntNode]

// 转换节点实现
public abstract class TypeConverterNode : ShizukuValueNode
{
    protected abstract ParameterEdgePort InputPort { get; }
    protected abstract ParameterEdgePort OutputPort { get; }
    protected abstract void ConvertValue();
    
    public override void GetOutputValues()
    {
        InputPort.GetSourceValue();
        ConvertValue();
    }
}

// 具体转换节点
public class FloatToIntConverter : TypeConverterNode
{
    private FloatParameterEdgePort _input = new();
    private IntParameterEdgePort _output = new();
    
    protected override void ConvertValue()
    {
        _output.Value = (int)_input.Value;
    }
}
```

**优势**：
- ✅ **可视化**：转换逻辑在图中清晰可见
- ✅ **可调试**：可在转换节点设置断点
- ✅ **可自定义**：用户可添加转换参数（如缩放系数）
- ✅ **类型安全**：编译时确定所有转换路径
- ✅ **性能优秀**：可利用值节点缓存机制

**工作量**：7-10 天

---

**备选方案：类型转换器注册中心**（如果时间不足）

```csharp
// 1. 转换器注册中心
public static class TypeConverterRegistry
{
    private static Dictionary<(Type, Type), Func<object, object>> _converters = new();
    
    static TypeConverterRegistry()
    {
        // 基础类型
        Register<int, float>(i => (float)i);
        Register<float, int>(f => (int)f);
        
        // Unity 类型
        Register<Vector3, Vector2>(v3 => new Vector2(v3.x, v3.y));
        Register<Vector2, Vector3>(v2 => new Vector3(v2.x, v2.y, 0));
        
        // 字符串转换
        Register<int, string>(i => i.ToString());
        Register<string, int>(s => int.TryParse(s, out var v) ? v : 0);
    }
    
    public static void Register<TFrom, TTo>(Func<TFrom, TTo> converter)
    {
        _converters[(typeof(TFrom), typeof(TTo))] = 
            obj => converter((TFrom)obj);
    }
    
    public static bool CanConvert(Type from, Type to)
    {
        return _converters.ContainsKey((from, to));
    }
    
    public static object Convert(object value, Type targetType)
    {
        var sourceType = value.GetType();
        if (_converters.TryGetValue((sourceType, targetType), out var converter))
        {
            return converter(value);  // 使用注册的转换器
        }
        
        // 回退到 Convert.ChangeType
        return System.Convert.ChangeType(value, targetType);
    }
}

// 2. 修改 GetSourceValue
public override void GetSourceValue()
{
    if (SameTypeConnectedPort != null)
    {
        var port = SameTypeConnectedPort as ParameterEdgePort<T>;
        Value = port.Value;
    }
    else if (DifferentTypeConnectedPort != null)
    {
        var differentValue = DifferentTypeConnectedPort.GetSelfValue();
        if (differentValue != null)
        {
            // 使用注册中心
            Value = (T)TypeConverterRegistry.Convert(differentValue, typeof(T));
        }
    }
}
```

**辅助方案：编译时类型检查**

```csharp
// ShizukuGraphView.cs - 连接时检查兼容性
public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
{
    var compatiblePorts = new List<Port>();
    
    foreach (var port in ports)
    {
        // 基本检查
        if (port.direction == startPort.direction || port.node == startPort.node)
            continue;
        
        // 类型兼容性检查
        var startType = GetPortType(startPort);
        var endType = GetPortType(port);
        
        if (startType == endType || TypeConverterRegistry.CanConvert(startType, endType))
        {
            compatiblePorts.Add(port);
        }
        else
        {
            // 显示为不兼容（红色警告）
            port.portColor = Color.red;
        }
    }
    
    return compatiblePorts;
}
```

**辅助方案：端口值缓存**

```csharp
public class ParameterEdgePort<T> : ParameterEdgePort
{
    public T Value = default;
    private int _cachedFrame = -1;
    
    public override void GetSourceValue()
    {
        // 帧缓存：避免同一帧重复转换
        if (_cachedFrame == Time.frameCount)
            return;
        
        // ... 获取和转换值
        
        _cachedFrame = Time.frameCount;
    }
}
```

#### 实施计划

- **版本**：v0.2.0
- **优先级**：⭐⭐⭐ 高
- **推荐方案**：显式类型转换节点
- **工作量**：
  - **首选**：显式转换节点（7-10 天）
    - TypeConverterNode 基类：1 天
    - 常用转换节点实现：2-3 天
    - ConverterNodeRegistry：1 天
    - 编辑器自动插入：2-3 天
    - 测试和文档：1-2 天
  - **备选**：转换器注册中心（4-5 天，如果时间紧张）

#### 预期收益

**功能增强**：

| 方案 | 可视化 | 可调试 | 可定制 | 符合标准 |
|------|--------|--------|--------|----------|
| 显式转换节点 | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ✅ UE 风格 |
| 隐式转换优化 | ⭐ | ⭐⭐ | ⭐⭐ | - |

**性能提升**：

| 场景 | 当前 | 优化后 | 提升 |
|------|------|--------|------|
| 同类型连接 | ~1μs | ~1μs | - |
| 异类型连接（1000次） | ~100μs | ~30-50μs | 2-3x |
| 复杂图（100节点） | ~5μs | ~2μs | 2.5x |

**GC 减少**：

| 场景 | 当前 | 优化后 | 减少 |
|------|------|--------|------|
| 异类型连接（1000次/帧） | ~16KB | ~8KB | 50% |

**用户体验提升**：
- ✅ 支持 **Vector3 ↔ Vector2** 等 Unity 类型（显式节点中实现）
- ✅ **图中清晰可见**转换过程，易于理解和调试
- ✅ 编辑器**提前发现**类型错误（禁止不可转换的连接）
- ✅ 用户可**自定义转换逻辑**和参数
- ✅ 符合 **UE 蓝图标准**，降低学习成本

**示例对比**：

```
隐式转换（不推荐）：
[FloatNode: 5.0] ────────────→ [IntNode: 5]
                  (转换不可见)

显式转换（推荐）：
[FloatNode: 5.0] ──→ [Float→Int] ──→ [IntNode: 5]
                        ↓
                  可断点、可查看、可自定义
```

#### 详细文档

完整的分析和实现方案请参考：[TYPE_CONVERSION_OPTIMIZATION.md](TYPE_CONVERSION_OPTIMIZATION.md)

---

## 🐛 运行时错误处理问题

### 1. 节点执行错误信息不友好

#### 问题描述

**当前情况**：

```csharp
// 节点执行出错
protected abstract void OnExecute();  // 如果抛异常，只有原始栈

// 类型转换失败
if (_parentGraph.TryGetProperty(PropertyName, out var value))
{
    SetOutputValue(value);  // 转换失败只有一条 Debug.LogWarning
}
```

**用户看到的错误**：
```
InvalidCastException: Cannot cast from source type to destination type.
  at PropertyGetNode_Float.SetOutputValue (System.Object value)
  at PropertyGetNode_Float.GetOutputValues ()
  ...
```

**缺少的信息**：
- ❌ 哪个 GameObject 出错
- ❌ 哪个 Behavior 类型
- ❌ 哪个蓝图资源文件
- ❌ 哪个节点（节点类型、GUID）
- ❌ 执行路径（从哪个节点链过来）

#### 改进方案

**方案 A：结构化错误上下文**

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

public abstract class ShizukuRunnableNode : ShizukuNormalNode
{
    public void Execute()
    {
        try
        {
            // 记录上下文
            ExecutionContext.Push(this);
            
            GetInputValues();
            OnExecute();
            
            ExecutionContext.Pop();
        }
        catch (Exception ex)
        {
            // 友好的错误信息
            LogStructuredError(ex);
            throw;  // 或者继续执行
        }
    }
    
    private void LogStructuredError(Exception ex)
    {
        var context = ExecutionContext.Current;
        var message = $@"
蓝图执行错误:
  GameObject: {context.GameObject?.name ?? "Unknown"}
  Behavior: {context.BehaviorType ?? "Unknown"}
  Blueprint: {context.BlueprintAssetPath ?? "Unknown"}
  节点类型: {context.CurrentNodeType ?? "Unknown"}
  节点 GUID: {context.CurrentNodeGUID ?? "Unknown"}
  执行路径: {string.Join(" → ", context.ExecutionPath)}
  
异常信息:
  {ex.GetType().Name}: {ex.Message}
  {ex.StackTrace}
";
        Debug.LogError(message, _parentGraph);
    }
}
```

**用户看到的改进后错误**：
```
蓝图执行错误:
  GameObject: Enemy_01
  Behavior: EnemyBehavior
  Blueprint: Assets/Blueprints/EnemyBlueprint.asset
  节点类型: PropertyGetNode_Float
  节点 GUID: a3f7d8e2-...
  执行路径: RootNode → EventNode(OnDeath) → PropertyGetNode_Float
  
异常信息:
  InvalidCastException: Cannot cast System.String to System.Single
  Property 'health' type mismatch
```

**方案 B：错误码系统**

```csharp
public enum ShizukuErrorCode
{
    E001_TypeConversionFailed,
    E002_PropertyNotFound,
    E003_EventNotRegistered,
    E004_CircularDependency,
    // ...
}

public static class ShizukuErrors
{
    public static void LogError(ShizukuErrorCode code, string details, Object context = null)
    {
        var message = $"[Shizuku-{code:D3}] {GetErrorMessage(code)}\n{details}\n" +
                      $"详细文档: https://shizuku.docs/errors/{code}";
        Debug.LogError(message, context);
    }
}
```

#### 实施计划

- **版本**：v0.2.0
- **优先级**：⭐⭐⭐ 高
- **推荐方案**：方案 A + 方案 B 结合
- **预期收益**：
  - 大幅提升调试效率
  - 降低用户学习成本
  - 减少 Issue 提交量

---

### 2. 缺少错误恢复机制

#### 问题描述

**当前行为**：
- 节点执行出错时，整个执行链中断
- 没有"遇错继续"的选项
- 无法记录错误节点用于后续分析

#### 改进方案

**错误恢复策略**：

```csharp
public enum ErrorHandlingMode
{
    StopOnError,      // 遇错停止（默认，当前行为）
    ContinueOnError,  // 遇错继续执行后续节点
    RetryOnce,        // 重试一次
    UseDefaultValue   // 使用默认值继续
}

[SerializeField]
protected ErrorHandlingMode errorHandling = ErrorHandlingMode.StopOnError;

public void Execute()
{
    try
    {
        GetInputValues();
        OnExecute();
    }
    catch (Exception ex)
    {
        HandleError(ex);
    }
}

private void HandleError(Exception ex)
{
    // 记录错误节点
    MarkNodeAsErrored(this);
    
    switch (errorHandling)
    {
        case ErrorHandlingMode.StopOnError:
            LogStructuredError(ex);
            throw;
            
        case ErrorHandlingMode.ContinueOnError:
            LogStructuredError(ex);
            // 继续执行下一个节点
            break;
            
        case ErrorHandlingMode.UseDefaultValue:
            LogStructuredError(ex);
            SetDefaultOutputValues();
            break;
    }
}
```

**编辑器支持**：

```csharp
// 在编辑器中高亮显示错误节点
public static HashSet<string> ErroredNodeGUIDs = new HashSet<string>();

// ShizukuNodeView 中
if (ErroredNodeGUIDs.Contains(_node.GUID))
{
    // 添加红色边框或背景
    this.style.borderColor = Color.red;
    this.style.borderWidth = 2;
}
```

#### 实施计划

- **版本**：v0.2.0 或 v0.3.0
- **优先级**：⭐⭐ 中高
- **场景**：
  - 调试模式：ContinueOnError
  - 生产环境：StopOnError
  - 容错场景：UseDefaultValue

---

### 3. 缺少循环依赖的运行时保护

#### 问题描述

**当前状态**：
- 编辑器中有循环检测（`HasCycle`）
- 但运行时没有保护
- 如果序列化数据损坏，可能导致无限递归

**风险场景**：
```csharp
// 假设数据被错误修改：A → B → C → A
nodeA.Execute();
  └─ nodeB.Execute();
      └─ nodeC.Execute();
          └─ nodeA.Execute();  // 💥 Stack Overflow
```

#### 改进方案

**运行时循环检测**：

```csharp
private static HashSet<string> _executingNodes = new HashSet<string>();

public void Execute()
{
    // 检测循环
    if (_executingNodes.Contains(this.GUID))
    {
        Debug.LogError($"检测到循环依赖: 节点 {this.Title} (GUID: {this.GUID}) 正在重复执行！");
        return;
    }
    
    _executingNodes.Add(this.GUID);
    
    try
    {
        GetInputValues();
        OnExecute();
    }
    finally
    {
        _executingNodes.Remove(this.GUID);
    }
}
```

#### 实施计划

- **版本**：v0.2.0
- **优先级**：⭐⭐ 中（风险较低，但必要）
- **开销**：HashSet 操作 ~O(1)，性能影响可忽略

---

## 📋 实施优先级总结

### 立即实施（v0.2.0）

| 问题 | 优先级 | 预期工作量 | 收益 |
|------|--------|-----------|------|
| **Update 执行优化** | ⭐⭐⭐ | 2-3 天 | 性能提升 ~100x（无逻辑场景） |
| **值节点缓存** | ⭐⭐⭐ | 1-2 天 | 避免重复计算 |
| **类型转换优化** | ⭐⭐⭐ | 4-5 天 | 性能提升 2-3x，GC 减少 50% |
| **友好错误信息** | ⭐⭐⭐ | 3-4 天 | 大幅提升调试效率 |
| **循环依赖保护** | ⭐⭐ | 1 天 | 提升稳定性 |

**总工作量**：约 11-15 天

### 后续实施（v0.3.0+）

| 问题 | 优先级 | 说明 |
|------|--------|------|
| **错误恢复机制** | ⭐⭐ | 根据用户反馈决定策略 |
| **静态缓存清理** | ⭐ | 目前影响有限，不紧急 |

---

## 🔗 相关文档

- [开发路线图](ROADMAP.md) - 版本规划
- [架构设计](ARCHITECTURE.md) - 系统设计
- [Blueprint Generator 总结](BLUEPRINT_GENERATOR_SUMMARY.md) - 完整问题列表

---

**最后更新**: 2026-02-24  
**文档版本**: 1.0

