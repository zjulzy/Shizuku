# 节点创建菜单使用说明

## 查找并创建节点

1. 在图编辑器空白处点击右键；
2. 打开“创建节点”；
3. 从 `基础`、`数学`、`逻辑`、`变量`、`蓝图` 等中文分组浏览，或输入英文节点名搜索；
4. 选择菜单项后在鼠标位置创建节点。

例如输入 `add` 可以找到各类型加法，输入 `float` 可以找到名称中包含 Float 的节点。从端口拖线到空白处时，也会打开同一份搜索树。

## 暴露自定义节点

```csharp
using System;

[Serializable]
[NodeMenuItem("自定义/Double Value", Description = "将输入整数乘以二")]
public class DoubleValueNode : ShizukuValueNode
{
    // 节点实现
}
```

`NodeMenuItem` 的构造函数只接收完整菜单路径：

```csharp
public NodeMenuItemAttribute(string menuPath)
```

可选属性：

```csharp
[NodeMenuItem("向量/Dot Product", Description = "计算两个向量的点积", Order = 10)]
```

## 路径规范

推荐：

```csharp
[NodeMenuItem("场景/Get World Position")]
[NodeMenuItem("类型转换/Int To String")]
```

避免：

```csharp
[NodeMenuItem("Utility/GetTransformPositionNode")] // 分组不是中文
[NodeMenuItem("场景/获取世界坐标")]               // 节点名不是英文
[NodeMenuItem("场景/对象/变换/位置/Get Position")] // 层级过长
```

菜单路径应直接描述用户要做的事，不依赖类名、文件夹或内部类型名。通常使用“中文功能域/英文操作名”两层即可。

## 标题规则

默认标题是 `MenuPath` 的最后一段。例如 `数学/Add (Float)` 的节点标题为 `Add (Float)`。只有标题需要显示所选变量、属性或事件等动态内容时才覆写 `Title`。

## 常见问题

### 节点为什么没有出现在菜单中？

确认节点是非抽象 `ShizukuNodeBase` 子类、标注了 `NodeMenuItem`、路径非空，并等待 Unity 编译完成。未标注节点会被有意隐藏，不会再使用类名生成默认菜单项。

### 如何隐藏内部节点？

不要添加 `NodeMenuItem`。根节点、事件入口、返回节点等结构节点由编辑器的专用流程管理。

### 修改路径会影响序列化吗？

不会。`MenuPath` 只负责编辑器显示和默认标题，不更改节点类名或已有图中的序列化类型。
