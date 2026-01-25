# Shizuku 架构设计文档

## 📐 设计理念

### 核心原则

1. **关注点分离** - 运行时与编辑器代码完全解耦
2. **数据驱动** - 图数据以 ScriptableObject 形式存储
3. **类型安全** - 使用泛型和强类型系统
4. **可扩展性** - 开放-封闭原则，易于扩展新节点类型
5. **性能优先** - 避免运行时反射，优化执行路径

### 设计模式

- **Visitor 模式** - 节点执行遍历
- **Strategy 模式** - 节点逻辑封装
- **Command 模式** - 编辑器操作可撤销（未来）
- **Observer 模式** - 事件系统
- **Facade 模式** - BlueprintBehavior 简化接口

---

## 🏛️ 架构分层

```
┌────────────────────────────────────────────────────────────┐
│                     表现层 (Presentation)                    │
│  - ShizukuGraphWindow     编辑器窗口                         │
│  - ShizukuGraphView       可视化编辑视图                      │
│  - ShizukuNodeView        节点渲染                           │
│  - USS/UXML              样式和布局                          │
└────────────────────────────────────────────────────────────┘
                              ↓ 操作
┌────────────────────────────────────────────────────────────┐
│                     编辑层 (Editor Logic)                    │
│  - 节点创建/删除                                              │
│  - 端口连接/断开                                              │
│  - 环检测                                                    │
│  - 序列化/反序列化                                            │
└────────────────────────────────────────────────────────────┘
                              ↓ 持久化
┌────────────────────────────────────────────────────────────┐
│                    数据层 (Data Model)                       │
│  - ShizukuGraphBase       图容器                             │
│  - ShizukuNodeBase        节点数据                           │
│  - ParameterEdge          边数据                             │
│  - GroupData              分组数据                           │
└────────────────────────────────────────────────────────────┘
                              ↓ 执行
┌────────────────────────────────────────────────────────────┐
│                   运行时层 (Runtime)                         │
│  - GraphRunner            图执行器                           │
│  - Node.Execute()         节点执行                           │
│  - Port.GetSourceValue()  数据传递                           │
└────────────────────────────────────────────────────────────┘
                              ↓ 集成
┌────────────────────────────────────────────────────────────┐
│                  应用层 (Application)                        │
│  - BlueprintBehavior      组件集成                           │
│  - BlueprintEvent         事件系统                           │
│  - PropertyAccessor       属性访问                           │
└────────────────────────────────────────────────────────────┘
```

---

## 🔍 核心组件详解

### 1. ShizukuGraphBase - 图数据容器

**职责**：
- 存储节点、边、分组数据
- 初始化图（建立连接关系）
- 提供图执行入口

**关键设计决策**：

```csharp
[CreateAssetMenu(...)]
public class ShizukuGraphBase : ScriptableObject
{
    // 使用 SerializeReference 支持多态序列化
    [SerializeReference]
    private List<ShizukuNodeBase> _nodes;
    
    // 非序列化字典用于快速查找
    [NonSerialized]
    private Dictionary<string, ShizukuNodeBase> _guid2NodeMap;
    
    // 初始化：建立运行时连接
    public virtual void Init()
    {
        // 1. 构建 GUID 映射
        foreach (var node in _nodes)
            _guid2NodeMap[node.GUID] = node;
        
        // 2. 初始化每个节点（反射端口）
        foreach (var node in _nodes)
            node.Init(this);
        
        // 3. 连接边（建立依赖关系）
        foreach (var edge in _edges)
            edge.ConnectPorts(this);
    }
}
```

**为什么使用 ScriptableObject？**
- Unity 原生序列化支持
- 可在 Inspector 中编辑
- 作为资源文件独立存在
- 可被多个场景/预制体引用

### 2. ShizukuNodeBase - 节点抽象

**职责**：
- 定义节点接口
- 管理端口集合
- 实现执行流程

**执行流程设计**：

```csharp
public void Execute()
{
    // 1. 准备阶段：获取输入值
    GetInputValues();
    
    // 2. 执行阶段：调用子类逻辑
    OnExecute();
    
    // 3. 传播阶段：选择并执行下一个节点
    if (OnSelectNextNode(out var guid))
    {
        _parentGraph.Guid2NodeMap[guid].Execute();
    }
}

private void GetInputValues()
{
    // 触发依赖节点计算输出
    foreach (var node in DependentNodes)
        node.GetOutputValues();
    
    // 从连接的端口复制值
    foreach (var port in SelfInputPorts)
        port.GetSourceValue();
}
```

**为什么需要 DependentNodes？**
- 值节点可能不在控制流链上
- 需要按需触发计算（Pull 模型）
- 避免不必要的计算

### 3. ParameterEdgePort - 端口系统

**设计目标**：
- 类型安全的数据传递
- 支持同类型和异类型连接
- 简洁的 API

**泛型设计**：

```csharp
[Serializable]
public class ParameterEdgePort
{
    [SerializeField] public string Name;
    [SerializeField] public bool IsOut;
    
    // 运行时连接（非序列化）
    [NonSerialized] public ParameterEdgePort SameTypeConnectedPort;
    [NonSerialized] public ParameterEdgePort DifferentTypeConnectedPort;
    
    public virtual void GetSourceValue() { }
}

[Serializable]
public class ParameterEdgePort<T> : ParameterEdgePort
{
    public T Value = default;
    
    public override void GetSourceValue()
    {
        if (SameTypeConnectedPort != null)
        {
            // 同类型：直接赋值
            Value = ((ParameterEdgePort<T>)SameTypeConnectedPort).Value;
        }
        else if (DifferentTypeConnectedPort != null)
        {
            // 异类型：尝试转换
            Value = (T)Convert.ChangeType(
                ((dynamic)DifferentTypeConnectedPort).Value, 
                typeof(T)
            );
        }
    }
}
```

**为什么分 SameType 和 DifferentType？**
- 同类型连接：零开销赋值
- 异类型连接：标记需要转换
- 性能优化：避免每次都尝试转换

### 4. ChainPort - 控制流端口

**设计思路**：

```csharp
[Serializable]
public class ChainPort
{
    public string NextNodeGuid = null;
    public string Name;
}
```

**为什么不用 ParameterEdge？**
- 控制流不传递数据，只记录连接
- ChainPort 存储在节点内部，序列化更简单
- 不需要类型信息

### 5. ParameterEdge - 边数据

**职责**：仅记录连接关系，不参与运行时逻辑。

```csharp
[Serializable]
public class ParameterEdge
{
    [SerializeField] public string OutputNodeGuid;
    [SerializeField] public string OutputPortName;
    [SerializeField] public string InputNodeGuid;
    [SerializeField] public string InputPortName;
    
    // 在 Init 时建立实际连接
    public void ConnectPorts(ShizukuGraphBase graph)
    {
        var outputNode = graph.Nodes.Find(n => n.GUID == OutputNodeGuid);
        var inputNode = graph.Nodes.Find(n => n.GUID == InputNodeGuid);
        
        // 添加到依赖列表
        inputNode.DependentNodes.Add(outputNode);
        
        // 连接端口
        var outputPort = outputNode.SelfOutputPorts.Find(p => p.Name == OutputPortName);
        var inputPort = inputNode.SelfInputPorts.Find(p => p.Name == InputPortName);
        inputPort.SameTypeConnectedPort = outputPort;  // 或 DifferentTypeConnectedPort
    }
}
```

---

## 🎭 蓝图系统设计 (v0.2.0)

### 核心架构：CRTP 泛型模式

**设计挑战**：如何让蓝图类型安全地绑定到 Behavior 类型？

**最终方案：CRTP (Curiously Recurring Template Pattern)**

```csharp
// ✅ 使用自引用泛型实现类型安全
public abstract class BlueprintBehavior<T> : MonoBehaviour 
    where T : BlueprintBehavior<T>
{
    [SerializeField]
    private ShizukuBluePrint<T> _blueprint;  // 强类型蓝图
    
    protected virtual void Start()
    {
        if (_blueprint != null)
        {
            _blueprint.InitializeBehavior((T)this);  // 类型安全的绑定
        }
    }
}

// 具体实现
public class EnemyBehavior : BlueprintBehavior<EnemyBehavior>
{
    // 类型安全！编译时检查！
}
```

**优势**：
- ✅ **编译时类型检查** - 蓝图类型不匹配会编译报错
- ✅ **无反射初始化** - `InitializeBehavior` 直接调用，无需反射
- ✅ **IntelliSense 支持** - IDE 可以正确推断类型
- ✅ **消除循环依赖** - Behavior 和 Blueprint 可以分别定义

### Update 系统设计

**设计理念**：Root Node + 事件驱动混合模式

```csharp
private void Update()
{
    // 每帧执行 Root Node（设计特性）
    // 适用场景：持续检测、状态机更新、AI 轮询等
    if (_blueprint != null)
    {
        _blueprint.Update();  // 执行 Root Node 开始的节点链
    }
}
```

**两种执行模式**：

1. **Root Node 模式** - 每帧持续执行
   ```
   [Root Node] → [检测距离] → [是否接近玩家?] 
                                   ├─ 是 → [追击]
                                   └─ 否 → [巡逻]
   ```

2. **Event 模式** - 事件触发执行
   ```
   C# 调用 ExecuteBlueprintEvent("OnTakeDamage", 10)
          ↓
   [Event: OnTakeDamage] → [播放受击动画] → [扣血] → [检查死亡]
   ```

**使用建议**：
- Root Node：用于每帧需要执行的逻辑（AI、检测、状态机）
- Event Node：用于响应式逻辑（受伤、死亡、触发器）

### BlueprintOverridable 系统

**目标**：简化蓝图重写 C# 方法的模板代码

**使用方式**：

```csharp
public class EnemyBehavior : BlueprintBehavior<EnemyBehavior>
{
    [BlueprintOverridable]  // ← 标记可被蓝图重写
    public virtual void TakeDamage(float damage)
    {
        // 自动检查蓝图是否实现（带缓存优化）
        if (TryExecuteBlueprintOverride(nameof(TakeDamage), damage))
            return;  // 蓝图执行了，跳过默认逻辑
        
        // 默认 C# 逻辑（仅在蓝图未实现时执行）
        health -= damage;
        if (health <= 0) OnDeath();
    }
    
    // 支持自定义事件名
    [BlueprintOverridable("OnPlayerAttack")]
    public virtual void Attack(GameObject target) { ... }
}
```

**工作原理**：

```
第一次调用 TakeDamage:
  ├─ 反射获取方法信息
  ├─ 检查 [BlueprintOverridable] 标记
  ├─ 获取事件名并缓存
  └─ 查找并执行蓝图事件

后续调用（缓存命中）:
  ├─ 直接从缓存获取事件名 (字典查询 ~1μs)
  └─ 查找并执行蓝图事件
```

**性能对比**：

| 操作 | 优化前 | 优化后 |
|------|--------|--------|
| 首次调用 | ~100-200μs | ~100-200μs |
| 后续调用 | ~100-200μs（每次反射） | **~1μs**（缓存） |

### 事件系统详解

**注册流程**：

```csharp
// ShizukuBluePrint.InitializeBehavior()
protected virtual void RegisterBlueprintEvents(T behavior)
{
    foreach (var node in Nodes)
    {
        if (node is BlueprintEventNode eventNode)
        {
            // 注册事件处理器（闭包捕获 eventNode）
            behavior.RegisterBlueprintEvent(
                eventNode.EventName,
                (args) => eventNode.TriggerEvent(args)
            );
        }
    }
}
```

**执行流程**：

```
C# 代码:
ExecuteBlueprintEvent("TakeDamage", 10f)
  ↓
BlueprintBehavior._blueprintEvents["TakeDamage"]?.Invoke([10f])
  ↓
BlueprintEventNode.TriggerEvent([10f])
  ↓
设置参数输出端口值
  ↓
Execute() 开始执行节点链
```

### 属性访问系统优化

**v0.2.0 改进：静态缓存**

```csharp
// 所有同类型实例共享缓存
private static Dictionary<string, Func<T, object>> _cachedGetters;
private static Dictionary<string, Action<T, object>> _cachedSetters;
private static bool _accessorsCached = false;

protected virtual void RegisterPropertyAccessors(T behavior)
{
    // 首次调用时构建缓存
    if (!_accessorsCached)
    {
        BuildAccessorCache();  // 反射所有字段/属性
        _accessorsCached = true;
    }
    
    // 使用缓存的访问器注册（无反射）
    foreach (var kvp in _cachedGetters)
    {
        var getter = kvp.Value;
        behavior.RegisterPropertyGetter(kvp.Key, () => getter(behavior));
    }
}
```

**性能提升**：

| 场景 | 优化前 | 优化后 |
|------|--------|--------|
| 创建第 1 个实例 | ~500μs | ~500μs |
| 创建第 2+ 个实例 | ~500μs | **~5μs** |
| 100 个实例总耗时 | ~50ms | **~0.5ms** |

---

## 🎨 编辑器架构

### GraphView 组件层次

```
ShizukuGraphWindow (EditorWindow)
  └─ ShizukuGraphView (GraphView)
       ├─ GridBackground
       ├─ ShizukuNodeView (Node)
       │    ├─ ControlFlowPortContainer
       │    │    ├─ ControlFlowPort (Previous)
       │    │    └─ ControlFlowPort (Next)
       │    ├─ inputContainer
       │    │    └─ Port (参数输入)
       │    └─ outputContainer
       │         └─ Port (参数输出)
       ├─ Edge (连接线)
       └─ CustomGroup (分组)
```

### 端口连接流程

```
用户拖拽端口
  ↓
GetCompatiblePorts() - 查找兼容端口
  ├─ 检查方向（Input/Output）
  ├─ 检查类型（ControlFlow/Parameter）
  └─ 返回候选端口列表
  ↓
用户松开鼠标
  ↓
graphViewChanged 回调
  ↓
OnGraphViewChanged()
  ├─ 环检测（WouldCreateCycle）
  ├─ 创建边数据
  │    ├─ ControlFlowPort → 设置 ChainPort.NextNodeGuid
  │    └─ ParameterPort → 添加 ParameterEdge
  └─ 标记脏数据（EditorUtility.SetDirty）
```

### 序列化与反序列化

**保存流程**：

```
用户点击"保存"
  ↓
SaveToAsset()
  ├─ 更新 Group 位置数据
  ├─ 节点数据已自动同步（通过 SetPosition）
  ├─ 边数据已在 OnGraphViewChanged 中维护
  └─ EditorUtility.SetDirty + AssetDatabase.SaveAssets
```

**加载流程**：

```
双击资源
  ↓
OnOpenAsset()
  ↓
LoadFromAsset()
  ├─ 清空视图
  ├─ 初始化图（Init）
  ├─ 创建节点视图
  │    └─ 通过反射初始化端口
  ├─ 创建控制流边
  │    └─ 读取 ChainPort.NextNodeGuid
  ├─ 创建参数边
  │    └─ 读取 ParameterEdge 列表
  └─ 创建分组视图
```

---

## 🔄 执行模型详解

### 控制流执行（Push 模型）

```
Root.Execute()
  ↓
A.Execute()
  ↓
B.Execute()
  ↓
C.Execute()
```

**特点**：
- 主动推进执行
- 同步顺序执行
- 由控制流边决定顺序

### 数据流计算（Pull 模型）

```
可执行节点需要输入
  ↓
GetInputValues()
  ↓
遍历 DependentNodes
  ↓
值节点.GetOutputValues()
  ↓
返回计算结果
```

**特点**：
- 按需触发计算
- 递归获取依赖
- 值节点可能不在控制流上

### 混合执行示例

```
Root
  ↓ (控制流)
If (condition)
  ↑ (数据流 - 获取 condition 值)
GetHealth() (值节点)
  ↑ (数据流 - 递归获取依赖)
GetMaxHealth() (值节点)
```

执行顺序：
1. Root.Execute()
2. If.Execute()
   - If.GetInputValues()
   - GetHealth.GetOutputValues()
   - GetMaxHealth.GetOutputValues()（如果 GetHealth 依赖它）
3. If.OnExecute()（条件判断）
4. If.OnSelectNextNode()（选择分支）
5. TrueNode.Execute() 或 FalseNode.Execute()

---

## 🛡️ 约束与验证

### 环检测算法

```csharp
public static bool WouldCreateCycle(this ShizukuGraphView graph, Edge newEdge)
{
    var sourceNode = newEdge.output.node;
    var targetNode = newEdge.input.node;
    
    // 使用 DFS 检查从 target 是否能到达 source
    return HasPathDFS(targetNode, sourceNode, new HashSet<Node>());
}

private static bool HasPathDFS(Node start, Node target, HashSet<Node> visited)
{
    if (start == target) return true;  // 找到环
    if (visited.Contains(start)) return false;
    
    visited.Add(start);
    
    // 遍历所有输出边
    foreach (var port in start.outputContainer.Query<Port>().ToList())
    {
        foreach (var edge in port.connections)
        {
            if (HasPathDFS(edge.input.node, target, visited))
                return true;
        }
    }
    
    return false;
}
```

**时间复杂度**：O(V + E)，V 为节点数，E 为边数

### 端口兼容性检查

```csharp
public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
{
    var compatiblePorts = new List<Port>();
    
    ports.ForEach(port =>
    {
        // 1. 不能连接自己
        if (startPort == port) return;
        
        // 2. 不能连接同一节点
        if (startPort.node == port.node) return;
        
        // 3. 方向必须相反
        if (startPort.direction == port.direction) return;
        
        // 4. 类型必须匹配
        bool isStartControlFlow = startPort is ControlFlowPort;
        bool isTargetControlFlow = port is ControlFlowPort;
        if (isStartControlFlow != isTargetControlFlow) return;
        
        compatiblePorts.Add(port);
    });
    
    return compatiblePorts;
}
```

---

## 📊 性能考虑

### 热路径优化

**已优化**：
- ✅ GUID 映射字典 - O(1) 节点查找
- ✅ 端口列表缓存 - 避免重复反射
- ✅ 非序列化运行时数据 - 减少序列化开销

**待优化**：
- ⏳ 值节点缓存 - 同一帧避免重复计算
- ⏳ 端口类型转换 - 预编译转换委托
- ⏳ 节点执行池化 - 避免频繁递归

### 内存占用

**运行时数据**：
- 每个图：~1KB（小图）到 ~10KB（大图）
- 每个节点：~200B（基础开销）+ 端口数据
- 边连接：仅指针引用，无额外开销

**编辑器数据**：
- 视图对象：~1-2KB/节点
- StyleSheet：共享，仅一份

---

## 🔮 扩展点

### 1. 自定义节点类型

继承 `ShizukuNodeBase`，实现接口。

### 2. 自定义端口类型

继承 `ParameterEdgePort<T>`。

### 3. 自定义图类型

继承 `ShizukuGraphBase`，重写 `Init()` 和 `Update()`。

### 4. 自定义编辑器视图

继承 `ShizukuNodeView`，重写渲染逻辑。

### 5. 自定义验证规则

实现 `IGraphValidator` 接口（未来）。

---

## 🎯 设计权衡

| 决策 | 优点 | 缺点 | 选择理由 |
|-----|------|------|---------|
| ScriptableObject 存储 | Unity 原生，易用 | 不易版本控制 | 优先易用性 |
| 字符串 GUID | 灵活，易序列化 | 无类型检查 | 序列化友好 |
| 反射端口 | 声明式，简洁 | 初始化开销 | 仅初始化时使用 |
| 递归执行 | 实现简单 | 栈溢出风险 | 图深度通常有限 |
| 泛型蓝图 | 类型安全 | 代码生成复杂 | 值得投资 |

---

## 📚 参考资料

- [Unity GraphView API](https://docs.unity3d.com/ScriptReference/Experimental.GraphView.GraphView.html)
- [Unreal Engine Blueprint System](https://docs.unrealengine.com/en-US/ProgrammingAndScripting/Blueprints/index.html)
- [节点图系统设计模式](https://github.com/phisko/meta)

---

**文档版本**：1.0
**最后更新**：2026-01-25
