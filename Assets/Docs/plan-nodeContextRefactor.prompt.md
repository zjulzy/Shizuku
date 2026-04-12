# 方案：引入 `INodeContext` 接口重构节点执行上下文

## 背景

当前所有节点通过 `_parentGraph`（`ShizukuGraphBase`）来定义运行环境上下文。但实际上不同节点的上下文是不同的：
- 图中的节点 → 上下文是 `ShizukuGraphBase`
- 方法中的节点 → 上下文应该是 `ShizukuMethod`

目前的 workaround 是在 `ShizukuMethod.Init()` 中把方法内部的节点/边都注册到了父图的 `Guid2NodeMap` / `Guid2EdgeMap` 里，导致上下文边界不清晰。

## 核心思路

`_parentGraph` 拆成两个概念：
- **`_context`** (`INodeContext`) — 当前作用域的节点/边查找（图或方法）
- **`RootGraph`** — 通过 context 访问全局资源（变量、函数定义）

## 1. 新建 `INodeContext.cs`

路径：`ShizukuGraph/Runtime/INodeContext.cs`

```csharp
using System.Collections.Generic;

namespace Shizuku.Graph
{
    /// <summary>
    /// 节点执行上下文接口。
    /// 图中的节点 → 上下文是 ShizukuGraphBase；
    /// 函数中的节点 → 上下文是 ShizukuMethod。
    /// </summary>
    public interface INodeContext
    {
        Dictionary<string, ShizukuNodeBase> Guid2NodeMap { get; }
        Dictionary<string, ParameterEdge> Guid2EdgeMap { get; }

        /// <summary>
        /// 根图（用于访问变量、函数等全局资源）。
        /// 图自身返回 this，方法返回所属的父图。
        /// </summary>
        ShizukuGraphBase RootGraph { get; }
    }
}
```

## 2. `ShizukuGraphBase` 实现 `INodeContext`

```csharp
// 类声明改为：
public partial class ShizukuGraphBase : ScriptableObject, INodeContext
{
    // ...existing code...

    // 实现 INodeContext.RootGraph — 图自身就是根
    public ShizukuGraphBase RootGraph => this;
}
```

## 3. `ShizukuMethod` 实现 `INodeContext`

```csharp
[Serializable]
public class ShizukuMethod : INodeContext
{
    // ...existing fields...

    /// <summary>
    /// 运行时父图引用（由 Init 设置，不序列化）
    /// </summary>
    [NonSerialized]
    private ShizukuGraphBase _rootGraph;
    public ShizukuGraphBase RootGraph => _rootGraph;

    // Init 签名不变，但多存一份引用
    public void Init(ShizukuGraphBase parentGraph)
    {
        _rootGraph = parentGraph;
        // ...existing init code...

        // ★ 关键改动：节点 Init 传 this 而不是 parentGraph
        foreach (var node in _nodes)
        {
            _guid2NodeMap[node.GUID] = node;
            node.Init(this);  // ← 传方法自身作为上下文

            // 不再注册到 parentGraph.Guid2NodeMap
        }

        foreach (var edge in _edges)
        {
            _guid2EdgeMap[edge.GUID] = edge;
            ConnectEdge(edge);
            // 不再注册到 parentGraph.Guid2EdgeMap
        }
    }
}
```

## 4. `ShizukuNodeBase` — 把 `_parentGraph` 改为 `_context`

```csharp
[NonSerialized]
protected INodeContext _context;

/// <summary>
/// 便捷访问根图（变量、函数等全局资源）
/// </summary>
protected ShizukuGraphBase RootGraph => _context?.RootGraph;

public virtual void Init(INodeContext context)  // ← 参数类型改为 INodeContext
{
    _context = context;
    // ...rest of Init unchanged...
}
```

## 5. 所有子类适配

### `ShizukuRunnableNode.Execute()` — `_parentGraph.Guid2NodeMap` → `_context.Guid2NodeMap`

```csharp
if (_context.Guid2NodeMap.TryGetValue(guid, out var nextNode))
```

### `ShizukuRootNode.StartExcute()` — 同理

```csharp
if (_context.Guid2NodeMap.TryGetValue(nextNodeGUID, out var nextNode))
```

### 变量节点 — `_parentGraph.SetVariable*` / `TryGetVariable*` → `RootGraph.*`

```csharp
// SetVariableNodes.cs
protected override void SetVariable(string guid, int value) => RootGraph.SetVariableInt(guid, value);

// GetVariableNodes.cs  
protected override bool TryGetVariable(string guid, out int value) => RootGraph.TryGetVariableInt(guid, out value);

// SetVariableNodeBase.cs
var variable = RootGraph?.GetVariableByGUID(VariableGUID);
```

### `InvokeMethodNode.OnExecute()` — 函数查找用 `RootGraph`

```csharp
var method = RootGraph.GetMethodByGUID(TargetMethodGUID);
```

### `ShizukuNormalNode.Init` — 签名跟着改

```csharp
public override void Init(INodeContext context)
{
    base.Init(context);
    // ...existing code...
}
```

## 6. `ShizukuGraphBase.Init()` 中主图节点传 `this`

```csharp
// 主图初始化节点时：
node.Init(this);  // ShizukuGraphBase 自身就是 INodeContext
```

## 改动影响矩阵

| 文件 | 改动点 |
|---|---|
| **新建** `INodeContext.cs` | 接口定义 |
| `ShizukuGraphBase.cs` | 实现 `INodeContext`，`RootGraph => this` |
| `ShizukuMethod.cs` | 实现 `INodeContext`，存 `_rootGraph`，`node.Init(this)` |
| `ShizukuNodeBase.cs` | `_parentGraph` → `_context: INodeContext` + `RootGraph` 属性 |
| `ShizukuNormalNode/ShizukuRootNode.cs` | `Init` 签名 + `_context.Guid2NodeMap` |
| `ShizukuRunnableNode.cs` | `_context.Guid2NodeMap` |
| `SetVariableNodes.cs` × 9 | `_parentGraph` → `RootGraph` |
| `GetVariableNodes.cs` × 9 | `_parentGraph` → `RootGraph` |
| `SetVariableNodeBase.cs` | `_parentGraph` → `RootGraph` |
| `InvokeMethodNode.cs` | `_parentGraph` → `RootGraph` |
| `MethodEntryNode.cs` / `MethodReturnNode.cs` | `Init` 签名 |

## 核心收益

- 方法中的节点不再注册到主图的 `Guid2NodeMap`，上下文隔离干净
- 变量等全局资源通过 `RootGraph` 访问，语义清晰
- 为未来嵌套函数、子图等扩展打好基础

