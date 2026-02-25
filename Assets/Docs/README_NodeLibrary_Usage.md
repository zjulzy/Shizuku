# 节点库自动发现 - 使用示例

## 快速开始

### 1. 打开蓝图编辑器

在 Unity 编辑器中：
1. 选择任意 `ShizukuGraphBase` 资源（如 `.asset` 文件）
2. 双击打开蓝图编辑器窗口

### 2. 使用 SearchWindow 创建节点

**方法一：空白处右键**
1. 在图编辑器空白处点击**右键**
2. 系统会自动打开 SearchWindow
3. 浏览节点分类或输入关键字搜索
4. 点击选择节点即可创建

**方法二：拖拽端口**
1. 从已有节点的输出端口拖出连线
2. 在空白处释放鼠标
3. 系统会打开 SearchWindow
4. 选择要连接的节点

### 3. 节点分类说明

当前系统包含以下节点分类：

#### 📦 基础节点
- **加一** - 将整数加一
- **打印** - 输出日志消息

#### 🧠 逻辑节点
- **条件分支** - 根据条件执行不同分支
- **循环** - 循环执行（未完全实现）

#### ➕ 数学节点

**加法**
- 整数加法 (`AddNode_Int`)
- 浮点数加法 (`AddNode_Float`)
- 二维向量加法 (`AddNode_Vector2`)
- 三维向量加法 (`AddNode_Vector3`)

**减法**
- 整数减法 (`SubtractNode_Int`)
- 浮点数减法 (`SubtractNode_Float`)
- 二维向量减法 (`SubtractNode_Vector2`)
- 三维向量减法 (`SubtractNode_Vector3`)

**乘法**
- 整数乘法 (`MultiplyNode_Int`)
- 浮点数乘法 (`MultiplyNode_Float`)
- 二维向量标量乘法 (`MultiplyNode_Vector2`)
- 三维向量标量乘法 (`MultiplyNode_Vector3`)

**除法**
- 整数除法 (`DivideNode_Int`)
- 浮点数除法 (`DivideNode_Float`)
- 二维向量标量除法 (`DivideNode_Vector2`)
- 三维向量标量除法 (`DivideNode_Vector3`)

#### 📘 蓝图节点

**获取属性**
- 浮点数 (`GetPropertyNode_Float`)
- 整数 (`GetPropertyNode_Int`)
- 布尔 (`GetPropertyNode_Bool`)
- 字符串 (`GetPropertyNode_String`)
- 通用对象 (`GetPropertyNode`)

**设置属性**
- 浮点数 (`SetPropertyNode_Float`)
- 整数 (`SetPropertyNode_Int`)
- 布尔 (`SetPropertyNode_Bool`)
- 字符串 (`SetPropertyNode_String`)
- 通用对象 (`SetPropertyNode`)

## 创建自定义节点

### 步骤 1: 定义节点类

```csharp
using UnityEngine;

[NodeMenuItem("自定义/我的节点", NodeCategory.Basic, Description = "这是一个自定义节点")]
public class MyCustomNode : ShizukuValueNode
{
    public override string Title => "我的节点";
    
    [SerializeReference]
    private IntParameterEdgePort _input = new() { IsOut = false, Name = "输入" };
    
    [SerializeReference]
    private IntParameterEdgePort _output = new() { IsOut = true, Name = "输出" };
    
    public override void GetOutputValues()
    {
        GetInputValues();
        // 在这里实现你的逻辑
        _output.Value = _input.Value * 2;
    }
}
```

### 步骤 2: 保存并编译

保存文件后，Unity 会自动编译。编译完成后，你的节点就会出现在 SearchWindow 中。

### 步骤 3: 在编辑器中使用

1. 打开蓝图编辑器
2. 右键打开 SearchWindow
3. 在 "基础节点" → "自定义" 分类下找到 "我的节点"
4. 点击创建

## NodeMenuItem 特性详解

### 构造函数

```csharp
public NodeMenuItemAttribute(string menuPath, NodeCategory category = NodeCategory.Basic)
```

**参数：**
- `menuPath` - 菜单路径，使用 "/" 分隔层级
  - 示例：`"数学/加法/整数"`
  - 第一层会自动归入分类下
- `category` - 节点分类（默认 `Basic`）

### 可选属性

```csharp
[NodeMenuItem("路径", NodeCategory.Math, 
    Description = "节点描述文本",
    Order = 10)]
```

- `Description` - 节点描述，会显示为工具提示
- `Order` - 排序顺序，数字越小越靠前（默认 0）

## 最佳实践

### 1. 合理使用分类

```csharp
// ✅ 好的做法：使用合适的分类
[NodeMenuItem("条件判断", NodeCategory.Logic)]
public class MyIfNode : ShizukuRunnableNode { }

// ❌ 不好的做法：分类不当
[NodeMenuItem("条件判断", NodeCategory.Math)]  // 逻辑节点不应放在数学分类
public class MyIfNode : ShizukuRunnableNode { }
```

### 2. 清晰的菜单路径

```csharp
// ✅ 好的做法：层级清晰
[NodeMenuItem("变换/位置/获取", NodeCategory.Basic)]
public class GetPositionNode : ShizukuValueNode { }

// ❌ 不好的做法：路径过长或混乱
[NodeMenuItem("变换/位置相关/获取位置信息/获取", NodeCategory.Basic)]
public class GetPositionNode : ShizukuValueNode { }
```

### 3. 添加描述信息

```csharp
// ✅ 好的做法：提供清晰的描述
[NodeMenuItem("向量/点积", NodeCategory.Math, 
    Description = "计算两个向量的点积，返回标量值")]
public class DotProductNode : ShizukuValueNode { }

// ❌ 不好的做法：没有描述
[NodeMenuItem("向量/点积", NodeCategory.Math)]
public class DotProductNode : ShizukuValueNode { }
```

### 4. 合理使用排序

```csharp
// 常用节点排在前面
[NodeMenuItem("获取位置", NodeCategory.Basic, Order = 0)]
public class GetPositionNode : ShizukuValueNode { }

// 不常用节点排在后面
[NodeMenuItem("获取世界矩阵", NodeCategory.Basic, Order = 100)]
public class GetWorldMatrixNode : ShizukuValueNode { }
```

## 搜索技巧

1. **关键字搜索**
   - 输入 "add" 可以找到所有加法节点
   - 输入 "float" 可以找到所有浮点数相关节点

2. **分类浏览**
   - 展开 "数学节点" 查看所有数学运算
   - 展开 "蓝图节点" 查看属性操作节点

3. **快速定位**
   - 使用方向键快速导航
   - 按 Enter 直接创建选中的节点

## 常见问题

### Q: 为什么我的自定义节点没有出现在 SearchWindow 中？

**A:** 检查以下几点：
1. 节点类是否添加了 `[NodeMenuItem]` 特性
2. 节点类是否继承自 `ShizukuNodeBase`
3. 节点类是否为抽象类（抽象类不会被扫描）
4. Unity 是否已完成编译

### Q: 可以不使用 NodeMenuItem 特性吗？

**A:** 可以。没有标注的节点会使用默认配置：
- 菜单路径：节点类名（去掉 "Node" 后缀）
- 分类：Basic
- 描述：自动生成
- 但**强烈建议**添加特性以提供更好的用户体验

### Q: 如何隐藏某些节点不让其出现在 SearchWindow？

**A:** 当前版本会自动过滤以下节点：
- `ShizukuRootNode`
- `BlueprintEventNode`
- `TypeConverterNode` 及其子类

如需隐藏其他节点，暂时需要修改 `NodeSearchWindowProvider.ScanAllNodeTypes()` 方法。

### Q: 可以动态修改节点菜单吗？

**A:** 当前不支持运行时修改。但可以：
1. 修改节点的 `NodeMenuItem` 特性
2. 重新编译
3. 重新打开蓝图编辑器

## 性能说明

节点扫描只在以下情况执行：
- 打开 SearchWindow 时
- 首次调用 `CreateSearchTree()`

扫描结果会被缓存，不会影响运行时性能。

## 更新日志

### v0.2.0
- ✅ 实现 SearchWindow 集成
- ✅ 添加 NodeCategory 枚举
- ✅ 添加 NodeMenuItem 特性
- ✅ 所有现有节点已标注
- ✅ 支持搜索和分类浏览

