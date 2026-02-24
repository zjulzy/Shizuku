# 类型转换节点系统

## 📋 概述

类型转换节点系统实现了**显式的类型转换**，将转换过程可视化地展现在蓝图中，类似 UE 蓝图的转换节点。

## 🏗️ 架构

### 核心类

1. **TypeConverterNode** - 转换节点基类
   - 路径：`Assets/Scripts/Node/ConverterNodes/TypeConverterNode.cs`
   - 所有转换节点继承此类
   - 提供统一的转换接口

2. **ConverterNodeRegistry** - 转换节点注册中心
   - 路径：`Assets/Scripts/Node/ConverterNodes/ConverterNodeRegistry.cs`
   - 自动扫描和注册所有转换节点
   - 提供查询和创建转换节点的功能

3. **BasicTypeConverters** - 基础类型转换节点
   - 路径：`Assets/Scripts/Node/ConverterNodes/BasicTypeConverters.cs`
   - 包含 8 个常用转换节点：
     - Float ↔ Int
     - Int ↔ Bool
     - Float/Int → String
     - String → Float/Int

## ✅ 已完成功能

- [x] TypeConverterNode 基类实现
- [x] 8 个基础类型转换节点
- [x] ConverterNodeRegistry 自动注册中心
- [x] 类型查询和节点创建功能
- [x] 测试脚本

## 🎯 使用方法

### 1. 创建自定义转换节点

```csharp
using System;
using UnityEngine;

[Serializable]
public class Vector3ToVector2Converter : TypeConverterNode
{
    [SerializeReference]
    private Vector3ParameterEdgePort _input = new() { IsOut = false, Name = "Vector3" };
    
    [SerializeReference]
    private Vector2ParameterEdgePort _output = new() { IsOut = true, Name = "Vector2" };
    
    public override string Title => "Vector3 → Vector2";
    
    protected override ParameterEdgePort GetInputPort() => _input;
    
    protected override ParameterEdgePort GetOutputPort() => _output;
    
    protected override void ConvertValue()
    {
        _output.Value = new Vector2(_input.Value.x, _input.Value.y);
    }
}
```

### 2. 查询可用转换

```csharp
// 初始化注册中心
ConverterNodeRegistry.Initialize();

// 检查是否可以转换
if (ConverterNodeRegistry.CanConvert(typeof(float), typeof(int)))
{
    Debug.Log("Float 可以转换为 Int");
}

// 获取所有可转换的目标类型
var convertibleTo = ConverterNodeRegistry.GetConvertibleTo(typeof(float));
```

### 3. 创建转换节点

```csharp
// 创建 Float → Int 转换节点
var converter = ConverterNodeRegistry.CreateConverterNode(typeof(float), typeof(int));

if (converter != null)
{
    Debug.Log($"创建成功: {converter.Title}");
}
```

### 4. 测试转换功能

在 Unity 中：
1. 创建一个空 GameObject
2. 添加 `ConverterNodeTest` 组件
3. 在 Inspector 中右键点击组件
4. 选择以下菜单项之一：
   - "测试注册中心" - 测试所有转换的注册
   - "列出所有转换" - 查看所有可用转换
   - "测试转换逻辑" - 测试实际转换结果

## 📊 已实现的转换

| 源类型 | 目标类型 | 转换规则 |
|--------|---------|---------|
| Float | Int | 截断（向零取整） |
| Int | Float | 直接转换 |
| Int | Bool | 0→false, 非零→true |
| Bool | Int | false→0, true→1 |
| Float | String | 保留 2 位小数 |
| Int | String | ToString() |
| String | Int | TryParse，失败返回 0 |
| String | Float | TryParse，失败返回 0.0f |

## 🔜 下一步

接下来需要实现：

1. **编辑器集成**
   - 修改 `ShizukuGraphView.GetCompatiblePorts()` 检查类型兼容性
   - 实现自动插入转换节点的逻辑
   - 添加视觉反馈（蓝色端口表示需要转换）

2. **Unity 类型支持**
   - Vector3 ↔ Vector2
   - GameObject/Transform 类型端口
   - Color/Quaternion 等

3. **高级功能**
   - 转换参数（如缩放系数）
   - 转换链（自动寻找最短转换路径）
   - 转换节点折叠显示

## 🎨 视觉效果

转换节点使用**蓝色标题栏**（RGB: 0.5, 0.7, 1.0），与普通节点区分。

期望的蓝图效果：
```
[FloatNode: 5.7] ──→ [Float→Int] ──→ [IntNode: 5]
                        ↑ 蓝色节点
```

## 📝 注意事项

1. **自动注册**：所有 `TypeConverterNode` 的子类会被自动扫描和注册
2. **类型安全**：通过泛型端口确保类型安全
3. **性能优化**：转换节点继承 `ShizukuValueNode`，自动享受值节点缓存机制
4. **扩展性**：用户可以轻松创建自定义转换节点

## 🐛 已知问题

- 命名空间警告（不影响功能）
- 需要 Unity 刷新才能识别新的转换节点

## 📚 相关文档

- [TYPE_CONVERSION_OPTIMIZATION.md](../../Docs/TYPE_CONVERSION_OPTIMIZATION.md) - 完整的设计方案
- [ROADMAP.md](../../Docs/ROADMAP.md) - 开发路线图

---

**创建日期**: 2026-02-24  
**状态**: 基础实现完成 ✅

