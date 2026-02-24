# 节点开发指南

本文档详细说明如何创建和使用 Shizuku 节点。

---

## 📚 节点类型概述

Shizuku 蓝图系统支持两种节点模式：

### 1. 可执行节点（Execution Nodes）
- **特点**：参与控制流，按顺序执行
- **控制流**：有 Previous 输入和 Next 输出（菱形端口）
- **执行时机**：通过控制流链调用 `Execute()`
- **计算输出**：在 `OnExecute()` 中计算并设置输出值
- **用途**：逻辑流程、副作用操作（日志、伤害、触发事件等）

**示例**：
```csharp
public class ShizukuLogNode : ShizukuNodeBase
{
    public override bool SupportControlInput => true;
    public override bool SupportControlOutput => true;
    
    [SerializeField]
    private ChainPort _nextPort = new() {Name = "next" };
    
    protected override void OnExecute()
    {
        // 在Execute时计算输出
        Debug.Log(Message.Value);
    }
    
    protected override bool OnSelectNextNode(out string nextNodeGUID)
    {
        nextNodeGUID = _nextPort.NextNodeGuid;
        return !string.IsNullOrEmpty(nextNodeGUID);
    }
}
```

### 2. 值节点（Value Nodes）
- **特点**：不参与控制流，按需计算
- **控制流**：无 Previous/Next 端口
- **执行时机**：当其他节点需要其输出值时，通过 `GetOutputValues()` 触发计算
- **计算输出**：通常在 `GetOutputValues()` 中实现计算逻辑
- **缓存机制**：可选实现，同一帧内避免重复计算
- **用途**：数学运算、数据转换、属性读取等纯计算

**注意**：当前版本值节点缓存机制未完全实现，计划在 v0.2.0 中加入。

---

## 🔄 执行流程

### 可执行节点的执行流程
```
Root.Execute()
  ↓
GetInputValues()  → 触发依赖的值节点计算
  ↓                 (调用 node.GetOutputValues())
OnExecute()       → 执行逻辑，计算输出
  ↓
OnSelectNextNode() → 选择下一个节点
  ↓
NextNode.Execute() → 递归执行
```

### 值节点的计算流程
```
可执行节点需要输入值
  ↓
GetInputValues()
  ↓
遍历 DependentNodes
  ↓
调用 值节点.GetOutputValues()
  ↓
值节点计算输出
  ↓
返回结果给可执行节点
```

---

## 🎯 典型使用场景

### 场景1：MonoBehavior 的 Update 逻辑
```
Root (每帧执行)
  ↓
If (health < 50)
  ├─ true → PlayHurtAnimation
  └─ false → IdleAnimation
```

### 场景2：事件驱动逻辑
```
BlueprintEventNode (OnTakeDamage)
  ↓
GetProperty (defense)
  ↓
Subtract (damage - defense)
  ↓
SetProperty (health = result)
  ↓
If (health <= 0)
  ├─ true → Die
  └─ false → PlayHurtEffect
```

---

## 🛠️ 创建自定义节点

### 基础节点模板

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

### 分支节点模板

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

### 入口节点模板

```csharp
public class MyEntryNode : ShizukuNodeBase
{
    public override string Title => "入口节点";
    public override Color TitleBarColor => new Color(0.8f, 0.2f, 0.2f); // 红色
    
    // 入口节点不接受控制流输入
    public override bool SupportControlInput => false;
    public override bool SupportControlOutput => true;
    
    [SerializeField]
    private ChainPort _nextPort = new() { Name = "next" };
    
    protected override void OnExecute()
    {
        // 入口逻辑
    }
    
    protected override bool OnSelectNextNode(out string nextNodeGUID)
    {
        nextNodeGUID = _nextPort.NextNodeGuid;
        return !string.IsNullOrEmpty(nextNodeGUID);
    }
}
```

---

## ⚡ 性能优化要点

### 1. 避免在 OnExecute 中频繁分配
```csharp
// ❌ 不好：每次都创建新对象
protected override void OnExecute()
{
    var list = new List<int>();  // 每帧分配
    // ...
}

// ✅ 好：复用对象
private List<int> _cachedList = new List<int>();

protected override void OnExecute()
{
    _cachedList.Clear();
    // ...
}
```

### 2. 减少依赖链长度
- 过长的依赖链会增加计算开销
- 考虑在 C# 中预计算复杂值

### 3. 避免循环依赖
- 系统会在编辑器中阻止创建
- 但设计时仍需注意

---

## 🎨 节点颜色约定

| 节点类型 | 推荐颜色 | 用途 |
|---------|---------|------|
| 入口节点 | 红色 `new Color(0.8f, 0.2f, 0.2f)` | Root, Event |
| 控制流节点 | 青色 `Color.cyan` | If, Loop |
| 值节点 | 绿色 `new Color(0.3f, 0.7f, 0.3f)` | Add, Multiply |
| 工具节点 | 灰色 `Color.gray` | Log, Debug |
| 属性节点 | 蓝色 `new Color(0.2f, 0.6f, 0.8f)` | Get/Set Property |

---

## 🐛 常见问题

### Q: 端口值总是默认值？

**A:** 检查：
1. 端口是否正确连接
2. 输出节点是否在依赖链中
3. 类型转换是否成功（查看 Console 警告）

### Q: 如何创建多输出分支？

**A:** 添加多个 ChainPort：
```csharp
[SerializeField]
private ChainPort _option1 = new() { Name = "Option1" };
[SerializeField]
private ChainPort _option2 = new() { Name = "Option2" };
[SerializeField]
private ChainPort _option3 = new() { Name = "Option3" };
```

### Q: 可以创建没有控制流的可执行节点吗？

**A:** 可以，将 `SupportControlInput` 或 `SupportControlOutput` 设为 false 即可。

---

## 📖 相关文档

- [架构设计](ARCHITECTURE.md) - 深入了解系统设计
- [快速参考](QUICK_REFERENCE.md) - 常用代码片段
- [API 参考](../README.md#api-参考) - 完整 API 文档

---

**文档版本**：1.0
**最后更新**：2026-01-25
