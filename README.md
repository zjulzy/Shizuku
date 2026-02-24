# Shizuku - Unity 可视化蓝图系统

![Unity Version](https://img.shields.io/badge/Unity-6000.2.14.f1-blue)
![License](https://img.shields.io/badge/License-MIT-green)

> 🎨 基于 Unity GraphView 的可视化节点编辑系统，为 MonoBehaviour 和技能系统提供蓝图式逻辑编辑能力

---

## 📚 文档导航

- 📖 [快速参考](Assets/Docs/QUICK_REFERENCE.md) - 5分钟上手，常用代码速查
- 🏗️ [架构设计](Assets/Docs/ARCHITECTURE.md) - 深入了解系统设计和实现原理
- 🗺️ [开发路线图](Assets/Docs/ROADMAP.md) - 未来规划和版本计划
- ⚠️ [已知问题](Assets/Docs/KNOWN_ISSUES.md) - 性能优化和错误处理相关问题
- 🛠️ [节点开发指南](Assets/Docs/NODE_DEVELOPMENT.md) - 创建自定义节点
- 🤝 [贡献指南](Assets/Docs/CONTRIBUTING.md) - 如何参与项目开发
- 📁 [项目结构](Assets/Docs/PROJECT_STRUCTURE.md) - 目录和文件组织说明
- 🔧 [Blueprint Generator 使用指南](Assets/Docs/BLUEPRINT_GENERATOR_GUIDE.md) - 蓝图类生成器工具

---

## 📖 项目简介

Shizuku 是一个基于 Unity GraphView 的可视化节点编辑系统，旨在为 MonoBehaviour 组件和技能系统提供蓝图式的逻辑编辑能力。项目名称"Shizuku"（雫）意为水滴，象征着从简单节点汇聚成复杂逻辑的理念。

### 核心特性

- 🎨 **可视化编辑** - 基于 Unity GraphView 的直观图形界面
- 🔗 **双端口系统** - 控制流端口（菱形）与参数端口（圆形）分离
- 🎯 **CRTP 泛型** - 自引用泛型模式 `BlueprintBehavior<T>` 提供编译时类型检查
- 🚀 **BlueprintOverridable** - Attribute 标记简化蓝图重写，带缓存优化（~1μs）
- 🔄 **双执行模式** - Root Node 持续执行 + Event 事件驱动混合
- 🛡️ **环检测** - 编辑器自动防止创建循环依赖
- ⚡ **性能优化** - 静态缓存属性访问器，100 个实例仅 ~0.5ms
- 📦 **模块化设计** - 运行时与编辑器代码完全分离

### 适用场景

1. **组件蓝图** - 为 MonoBehaviour 提供可视化的 Update/事件逻辑
2. **技能系统** - 构建复杂的技能效果链和伤害计算
3. **AI 行为树** - 设计敌人 AI 的决策逻辑
4. **对话系统** - 创建分支对话和剧情流程
5. **关卡机制** - 实现触发器、机关等交互逻辑

---

## 🏗️ 架构设计

### 系统架构图

```
┌─────────────────────────────────────────────────────────┐
│                    编辑器层 (Editor)                      │
├─────────────────────────────────────────────────────────┤
│  ShizukuGraphWindow         - 编辑器窗口入口              │
│  ShizukuGraphView           - GraphView 主视图            │
│  ShizukuNodeView            - 节点视图                    │
│  ControlFlowPort            - 控制流端口（菱形）           │
│  ControlFlowPortContainer   - 端口容器                    │
│  CustomGroup                - 分组视图                    │
└─────────────────────────────────────────────────────────┘
                            ↓ 序列化
┌─────────────────────────────────────────────────────────┐
│                    运行时层 (Runtime)                     │
├─────────────────────────────────────────────────────────┤
│  ShizukuGraphBase           - 图数据容器 (ScriptableObject)│
│    ├─ Nodes                 - 节点列表                    │
│    ├─ Edges                 - 边列表                      │
│    ├─ Groups                - 分组列表                    │
│    └─ RootNodeGUID          - 根节点引用                  │
│                                                           │
│  ShizukuNodeBase            - 节点基类                    │
│    ├─ ParameterEdgePorts    - 参数端口                   │
│    ├─ ChainPorts            - 控制流端口                  │
│    └─ DependentNodes        - 依赖节点列表                │
│                                                           │
│  ParameterEdge              - 参数边连接数据              │
│  ParameterEdgePort<T>       - 泛型参数端口                │
│  ChainPort                  - 控制流端口数据              │
└─────────────────────────────────────────────────────────┘
                            ↓ 继承
┌─────────────────────────────────────────────────────────┐
│                   蓝图系统 (Blueprint)                    │
├─────────────────────────────────────────────────────────┤
│  ShizukuBluePrint<T>        - 泛型蓝图基类                │
│    ├─ InitializeBehavior    - 初始化 Behavior            │
│    ├─ RegisterEvents        - 注册事件                   │
│    └─ RegisterProperties    - 注册属性访问器              │
│                                                           │
│  BlueprintBehavior          - MonoBehaviour 包装类        │
│    ├─ Blueprint             - 持有蓝图引用                │
│    ├─ ExecuteBlueprintEvent - 执行蓝图事件                │
│    └─ Get/SetProperty       - 属性访问接口                │
│                                                           │
│  BlueprintEventNode         - 事件节点                    │
│    ├─ EventName             - 事件名称                    │
│    ├─ EventParameters       - 事件参数                    │
│    └─ TriggerEvent          - 触发执行                    │
└─────────────────────────────────────────────────────────┘
```

### 核心概念

#### 1. 控制流 (Control Flow)

控制流决定节点的**执行顺序**：

- **表现形式**：菱形端口（橙黄色）
- **连接规则**：Previous（输入）→ Next（输出）
- **数据结构**：`ChainPort` 存储下一个节点的 GUID
- **执行方式**：同步顺序执行，从 Root 或 Event 节点开始

```csharp
// 控制流示例
Root → If (condition) 
    ├─ true  → DealDamage → PlayEffect
    └─ false → Log → Next
```

#### 2. 数据流 (Data Flow)

数据流传递节点间的**参数值**：

- **表现形式**：圆形端口（默认颜色）
- **连接规则**：输出端口 → 输入端口
- **数据结构**：`ParameterEdge` + `ParameterEdgePort<T>`
- **计算时机**：在节点执行前，通过 `GetInputValues()` 从依赖节点获取

```csharp
// 数据流示例
GetHealth (output: float) 
    → Multiply (input: A, value: 0.5) 
        → DealDamage (input: damage)
```

#### 3. 执行模型

**可执行节点执行流程**：

```
Execute()
  ↓
1. GetInputValues()           // 获取所有输入参数
  ├─ 遍历 DependentNodes     // 触发依赖节点的 GetOutputValues()
  └─ 调用 port.GetSourceValue() // 从连接的端口复制值
  ↓
2. OnExecute()                // 执行节点逻辑，计算输出
  ↓
3. OnSelectNextNode()         // 选择下一个节点（通过 ChainPort）
  ↓
4. NextNode.Execute()         // 递归执行下一个节点
```

**根节点与事件节点**：

| 节点类型 | 触发时机 | 用途 |
|---------|---------|------|
| Root Node | 每帧 Update | MonoBehaviour 的 Update 逻辑 |
| Event Node | 事件触发 | 响应外部事件（如受伤、死亡） |

---

## 🚀 快速开始

### 1. 创建蓝图资源

**方式一：直接创建通用图**

```
Assets 右键 → Create → Shizuku → Graph
```

创建 `ShizukuGraphBase` 实例，可用于 `GraphRunner` 组件。

**方式二：创建蓝图组件**

1. 定义 Behavior 类：

```csharp
public class EnemyBehavior : BlueprintBehavior
{
    [SerializeField] protected float health = 100f;
    [SerializeField] protected float defense = 10f;
    
    public virtual void TakeDamage(float damage)
    {
        if (IsBlueprintEventImplemented("OnTakeDamage"))
        {
            ExecuteBlueprintEvent("OnTakeDamage", damage);
        }
        else
        {
            // 默认 C# 逻辑
            health -= Mathf.Max(0, damage - defense);
        }
    }
}
```

2. 创建对应的蓝图类：

```csharp
[CreateAssetMenu(fileName = "EnemyBlueprint", menuName = "Shizuku/Blueprint/Enemy Blueprint")]
public class EnemyBlueprint : ShizukuBluePrint<EnemyBehavior>
{
    public override void InitializeBehavior(EnemyBehavior behavior)
    {
        base.InitializeBehavior(behavior);
        // 可选：注册自定义属性
    }
}
```

3. 在 Inspector 中将蓝图赋值给 `EnemyBehavior` 组件。

### 2. 编辑蓝图

1. **打开编辑器**：双击蓝图资源或通过菜单 `Shizuku → ShizukuGraphWindow`
2. **创建节点**：右键菜单 → 创建节点
3. **连接节点**：
   - 拖拽控制流端口（菱形）建立执行顺序
   - 拖拽参数端口（圆形）传递数据
4. **保存**：点击工具栏的"保存"按钮

### 3. 运行蓝图

**方式一：使用 GraphRunner**

```csharp
// 添加到 GameObject
public class GraphRunner : MonoBehaviour
{
    public ShizukuGraphBase GraphAsset;
    
    void Start() => GraphAsset.Init();
    void Update() => GraphAsset.Update();
}
```

**方式二：使用 BlueprintBehavior**

```csharp
// Behavior 会自动初始化和更新蓝图
public class MyEnemy : EnemyBehavior
{
    // Blueprint 字段在 Inspector 中赋值
}
```

---

## 🛠️ 开发指南

### 创建自定义节点

#### 基本节点模板

```csharp
using UnityEngine;

public class MyCustomNode : ShizukuNodeBase
{
    // 1. 基本信息
    public override string Title => "我的节点";
    public override Color TitleBarColor => Color.cyan;
    
    // 2. 控制流支持
    public override bool SupportControlInput => true;
    public override bool SupportControlOutput => true;
    
    // 3. 定义输入端口
    [SerializeReference]
    private IntParameterEdgePort _inputValue = new() 
    { 
        IsOut = false, 
        Name = "输入" 
    };
    
    // 4. 定义输出端口
    [SerializeReference]
    private IntParameterEdgePort _outputValue = new() 
    { 
        IsOut = true, 
        Name = "输出" 
    };
    
    // 5. 定义控制流端口
    [SerializeField]
    private ChainPort _nextPort = new() { Name = "next" };
    
    // 6. 实现执行逻辑
    protected override void OnExecute()
    {
        // 处理输入，计算输出
        _outputValue.Value = _inputValue.Value * 2;
        Debug.Log($"执行: {_inputValue.Value} → {_outputValue.Value}");
    }
    
    // 7. 选择下一个节点
    protected override bool OnSelectNextNode(out string nextNodeGUID)
    {
        nextNodeGUID = _nextPort.NextNodeGuid;
        return !string.IsNullOrEmpty(nextNodeGUID);
    }
}
```

#### 分支节点模板

```csharp
public class MyBranchNode : ShizukuNodeBase
{
    public override string Title => "分支节点";
    
    [SerializeReference]
    private BoolParameterEdgePort _condition = new() 
    { 
        IsOut = false, 
        Name = "条件" 
    };
    
    [SerializeField]
    private ChainPort _truePort = new() { Name = "True" };
    
    [SerializeField]
    private ChainPort _falsePort = new() { Name = "False" };
    
    protected override void OnExecute()
    {
        // 分支节点通常不需要额外逻辑
    }
    
    protected override bool OnSelectNextNode(out string nextNodeGUID)
    {
        // 根据条件选择分支
        if (_condition.Value)
        {
            nextNodeGUID = _truePort.NextNodeGuid;
        }
        else
        {
            nextNodeGUID = _falsePort.NextNodeGuid;
        }
        return !string.IsNullOrEmpty(nextNodeGUID);
    }
}
```

### 支持的参数类型

系统内置以下类型：

| 类型 | 类名 | 说明 |
|-----|------|-----|
| 整数 | `IntParameterEdgePort` | int |
| 浮点 | `FloatParameterEdgePort` | float |
| 布尔 | `BoolParameterEdgePort` | bool |
| 字符串 | `StringParameterEdgePort` | string |

#### 创建自定义类型端口

```csharp
[Serializable]
public class Vector3ParameterEdgePort : ParameterEdgePort<Vector3>
{
    // 自动继承所有功能
}

// 使用
[SerializeReference]
private Vector3ParameterEdgePort _position = new() 
{ 
    IsOut = false, 
    Name = "位置" 
};
```

#### 在编辑器中添加输入控件

修改 `ShizukuNodeView.CreateInputFieldForPort()` 方法：

```csharp
private VisualElement CreateInputFieldForPort(ParameterEdgePort port)
{
    // ... 现有类型处理 ...
    
    // 添加 Vector3 类型
    case Vector3ParameterEdgePort vector3Port:
    {
        var vector3Field = new Vector3Field()
        {
            value = vector3Port.Value
        };
        vector3Field.RegisterValueChangedCallback(evt =>
        {
            vector3Port.Value = evt.newValue;
            EditorUtility.SetDirty(_graphAsset);
        });
        return vector3Field;
    }
}
```

### 蓝图事件系统

#### 定义可重写的方法

```csharp
public class PlayerBehavior : BlueprintBehavior
{
    [SerializeField] private int level = 1;
    
    public virtual void OnLevelUp()
    {
        // 检查是否有蓝图实现
        if (IsBlueprintEventImplemented("OnLevelUp"))
        {
            ExecuteBlueprintEvent("OnLevelUp", level);
        }
        else
        {
            // 默认 C# 实现
            Debug.Log($"Level Up to {level}!");
        }
    }
}
```

#### 在蓝图中创建事件节点

1. 右键 → 创建节点 → Event Node
2. 设置 EventName 为 "OnLevelUp"
3. 配置事件参数（在 Inspector 中）
4. 连接后续逻辑节点

#### 触发事件

```csharp
// 在 C# 代码中
playerBehavior.OnLevelUp();

// 或直接触发蓝图事件
blueprint.TriggerBlueprintEvent("OnLevelUp", currentLevel);
```

### 属性访问系统

#### 自动属性访问

蓝图会自动为 Behavior 的字段和属性生成访问器：

```csharp
public class PlayerBehavior : BlueprintBehavior
{
    [SerializeField] protected float health = 100f;  // 自动可访问
    [SerializeField] protected float mana = 50f;     // 自动可访问
    
    public float MaxHealth => 100f;  // 自动可读取
}
```

#### 手动注册属性

```csharp
public override void InitializeBehavior(PlayerBehavior behavior)
{
    base.InitializeBehavior(behavior);
    
    // 注册计算属性
    behavior.RegisterReadOnlyProperty("HealthPercent", 
        () => behavior.GetBlueprintProperty("health") as float? / 100f ?? 0f
    );
    
    // 注册自定义访问器
    behavior.RegisterProperty(
        "Level",
        () => PlayerPrefs.GetInt("Level"),
        (value) => PlayerPrefs.SetInt("Level", (int)value)
    );
}
```

---

## 📚 API 参考

### ShizukuGraphBase

图数据容器，继承自 `ScriptableObject`。

```csharp
public class ShizukuGraphBase : ScriptableObject
{
    // 属性
    public List<ShizukuNodeBase> Nodes { get; }
    public List<ParameterEdge> Edges { get; }
    public List<GroupData> Groups { get; }
    public string RootNodeGUID { get; set; }
    
    // 方法
    public void Init();                    // 初始化图（建立连接）
    public void Update();                  // 更新图（执行 Root 节点）
    public void AddNode(ShizukuNodeBase node);
    public void AddParameterEdge(ShizukuNodeBase source, string outputPort, 
                                  ShizukuNodeBase target, string inputPort);
}
```

### ShizukuNodeBase

节点基类，所有自定义节点继承此类。

```csharp
public abstract class ShizukuNodeBase
{
    // 基本属性
    public string GUID { get; }
    public virtual string Title { get; }
    public virtual Color TitleBarColor { get; }
    
    // 控制流支持
    public virtual bool SupportControlInput { get; }
    public virtual bool SupportControlOutput { get; }
    
    // 运行时数据
    public List<ShizukuNodeBase> DependentNodes { get; }
    public List<ParameterEdgePort> SelfInputPorts { get; }
    public List<ParameterEdgePort> SelfOutputPorts { get; }
    public Dictionary<string, ChainPort> ChainPorts { get; }
    
    // 生命周期
    public void Init(ShizukuGraphBase graph);
    public void Execute();
    public virtual void GetOutputValues();
    
    // 需子类实现
    protected abstract void OnExecute();
    protected abstract bool OnSelectNextNode(out string nextNodeGUID);
}
```

### BlueprintBehavior

蓝图组件基类，MonoBehaviour 的包装。

```csharp
public abstract class BlueprintBehavior : MonoBehaviour
{
    // 蓝图引用
    public ShizukuGraphBase Blueprint { get; }
    
    // 事件系统
    public void RegisterBlueprintEvent(string eventName, Action<object[]> handler);
    public void UnregisterBlueprintEvent(string eventName);
    protected void ExecuteBlueprintEvent(string eventName, params object[] args);
    protected bool IsBlueprintEventImplemented(string eventName);
    
    // 属性系统
    public void RegisterPropertyGetter(string propertyName, Func<object> getter);
    public void RegisterPropertySetter(string propertyName, Action<object> setter);
    public object GetBlueprintProperty(string propertyName);
    public void SetBlueprintProperty(string propertyName, object value);
}
```

### ShizukuBluePrint<T>

泛型蓝图基类，提供类型安全。

```csharp
public abstract class ShizukuBluePrint<T> : ShizukuGraphBase 
    where T : BlueprintBehavior
{
    // 生命周期
    public override void Init();
    public virtual void InitializeBehavior(T behavior);
    
    // 注册系统
    protected virtual void RegisterBlueprintEvents(T behavior);
    protected virtual void RegisterPropertyAccessors(T behavior);
    
    // 工具方法
    public T GetBehavior();
    public void TriggerBlueprintEvent(string eventName, params object[] args);
}
```

---

## 🎓 示例项目

### 示例 1：简单的伤害计算

**场景**：敌人受到伤害，根据防御力减免，血量归零时死亡。

**节点链**：
```
Event: OnTakeDamage (参数: damage)
  ↓
GetProperty: defense
  ↓
Subtract: damage - defense
  ↓
SetProperty: health = result
  ↓
If: health <= 0
  ├─ True  → Log "Enemy Died" → Destroy GameObject
  └─ False → Log "Enemy Hurt"
```

**C# 代码**：

```csharp
public class EnemyBehavior : BlueprintBehavior
{
    [SerializeField] protected float health = 100f;
    [SerializeField] protected float defense = 10f;
    
    public void TakeDamage(float damage)
    {
        ExecuteBlueprintEvent("OnTakeDamage", damage);
    }
}
```

### 示例 2：状态机 AI

**场景**：敌人根据玩家距离切换巡逻/追击/攻击状态。

**Root 节点链**：
```
Root (每帧)
  ↓
GetDistance: player
  ↓
If: distance > 10
  ├─ True  → Patrol
  └─ False → If: distance > 3
              ├─ True  → Chase
              └─ False → Attack
```

### 示例 3：技能系统

**场景**：技能命中后，计算伤害并触发特效。

**Event: OnSkillCast**
```
Event: OnSkillCast (参数: target, skillPower)
  ↓
GetProperty: attackPower
  ↓
Multiply: skillPower × attackPower
  ↓
CallMethod: target.TakeDamage(result)
  ↓
Parallel (多分支同时执行)
  ├─ SpawnEffect "Explosion"
  ├─ PlaySound "HitSound"
  └─ CameraShake 0.3s
```

---

## 🔧 编辑器使用技巧

### 快捷操作

| 操作 | 说明 |
|-----|------|
| 右键空白 | 打开创建节点菜单 |
| 拖拽节点 | 移动节点 |
| 框选 | 多选节点（支持 Shift 追加选择） |
| Delete | 删除选中的节点/边/分组 |
| 鼠标滚轮 | 缩放画布 |
| 中键拖拽 | 平移画布 |

### 节点颜色约定

| 颜色 | 节点类型 | 示例 |
|-----|---------|------|
| 🔴 红色 | 入口节点 | Root, Event |
| 🔵 青色 | 控制流节点 | If, Loop |
| 🟢 绿色 | 值节点 | Add, Multiply |
| 🟡 黄色 | 工具节点 | Log, Debug |

### 分组管理

1. **创建分组**：右键 → 创建分组
2. **添加节点到分组**：拖拽节点到分组内
3. **调整分组大小**：拖拽分组边框
4. **重命名分组**：双击分组标题

### 调试技巧

1. **添加 Log 节点**：在关键位置插入日志输出
2. **查看端口值**：在 Inspector 中查看节点的端口默认值
3. **检查连接**：使用不同颜色标记不同类型的边

---

## ⚙️ 配置与优化

### 性能优化建议

1. **避免每帧执行复杂逻辑**
   - Root 节点的执行频率 = Update 频率
   - 使用 Event 节点代替轮询检测

2. **减少节点依赖链长度**
   - 过长的依赖链会增加计算开销
   - 考虑在 C# 中预计算复杂值

3. **使用对象池**
   - 频繁创建/销毁的对象应使用对象池
   - 避免在节点中频繁 Instantiate

### 序列化最佳实践

1. **使用 SerializeReference**
   - 所有端口字段必须标记 `[SerializeReference]`
   - 保证多态序列化正常工作

2. **避免循环引用**
   - 节点间的引用通过 GUID 字符串
   - 不要在节点中直接引用其他节点对象

3. **版本兼容性**
   - 添加新字段时提供默认值
   - 使用 `[SerializeField]` 而非 public 字段

---

## 🗺️ 未来规划

### 短期目标（已规划）

- [ ] **扩展类型系统**
  - 支持 Vector3、Quaternion、GameObject 等 Unity 类型
  - 提供类型注册接口，支持自定义类型

- [ ] **值节点缓存机制**
  - 同一帧内避免重复计算
  - 提供性能分析工具

- [ ] **节点库自动发现**
  - 通过反射自动扫描节点类型
  - 动态生成创建菜单

- [ ] **调试工具**
  - 运行时显示节点执行状态
  - 断点系统和单步执行
  - 变量监视窗口

### 中期目标

- [ ] **子图系统**
  - 将常用逻辑封装为可复用的子图
  - 支持子图参数传递

- [ ] **函数节点**
  - 纯计算节点，无副作用
  - 自动缓存和优化

- [ ] **时间轴支持**
  - 延迟执行节点
  - 序列/并行执行容器
  - 协程支持

- [ ] **技能系统专用节点**
  - 伤害计算节点
  - Buff/Debuff 系统
  - 技能冷却管理

### 长期目标

- [ ] **代码生成**
  - 将蓝图编译为 C# 代码
  - 性能优化选项

- [ ] **多人协作**
  - 蓝图 Diff 和 Merge 工具
  - 版本控制友好的序列化格式

- [ ] **AI 辅助**
  - 智能节点推荐
  - 自动优化建议

- [ ] **可视化调试器**
  - 实时节点执行可视化
  - 性能分析工具
  - 数据流追踪

---

## 🐛 常见问题

### Q: 为什么我的蓝图没有执行？

**A:** 检查以下几点：
1. 图资源是否已调用 `Init()`？
2. Root 节点是否设置正确？（检查 RootNodeGUID）
3. 节点间的控制流是否正确连接？
4. 是否有编译错误导致节点未加载？

### Q: 如何调试蓝图逻辑？

**A:** 推荐方法：
1. 在关键位置添加 `ShizukuLogNode` 输出日志
2. 在 `OnExecute()` 中添加 `Debug.Log`
3. 使用断点调试节点的 C# 代码

### Q: 参数端口的值总是默认值？

**A:** 可能原因：
1. 端口未连接到输出端口
2. 输出节点未先执行（检查 DependentNodes）
3. 类型转换失败（检查 Console 警告）

### Q: 控制流边和参数边可以互相连接吗？

**A:** 不可以。系统通过 `GetCompatiblePorts()` 检查端口类型：
- 控制流端口（ControlFlowPort）只能连接控制流端口
- 参数端口只能连接参数端口

### Q: 如何在蓝图中调用 C# 方法？

**A:** 创建一个包装节点：

```csharp
public class CallMethodNode : ShizukuNodeBase
{
    [NonSerialized]
    private BlueprintBehavior _behavior;
    
    public void BindToBehavior(BlueprintBehavior behavior)
    {
        _behavior = behavior;
    }
    
    protected override void OnExecute()
    {
        // 调用 Behavior 的方法
        _behavior.GetComponent<MyComponent>().MyMethod();
    }
}
```

### Q: 蓝图保存后，节点位置丢失？

**A:** 确保：
1. 节点的 `PositionAndSize` 字段已正确序列化
2. 调用了 `SetPosition()` 更新位置
3. 保存前调用了 `EditorUtility.SetDirty()`

---

## 📝 贡献指南

欢迎贡献代码、报告问题或提出建议！

### 开发环境要求

- Unity 6000.2.14.f1 或更高版本
- Visual Studio 2022 或 JetBrains Rider

### 代码规范

1. **命名约定**
   - 类名：PascalCase（如 `ShizukuNodeBase`）
   - 私有字段：_camelCase（如 `_runtimeGraph`）
   - 公共属性：PascalCase（如 `TitleBarColor`）

2. **注释规范**
   - 所有公共 API 必须有 XML 文档注释
   - 复杂逻辑添加行内注释说明

3. **提交规范**
   - 使用清晰的提交信息（中文或英文）
   - 一次提交只做一件事

### 报告问题

在 GitHub Issues 中报告问题时，请提供：
1. Unity 版本
2. 复现步骤
3. 预期行为 vs 实际行为
4. 相关截图或日志

---

## 📄 许可证

本项目采用 MIT 许可证。详见 [LICENSE](LICENSE) 文件。

---

## 🙏 致谢

- 灵感来源：Unreal Engine Blueprint System
- 基于：Unity GraphView API
- 特别感谢：所有贡献者和测试者

---

## 📮 联系方式

- 项目地址：[GitHub Repository URL]
- 问题反馈：[Issues URL]
- 邮箱：[Your Email]

---

**最后更新日期**：2026-01-25
