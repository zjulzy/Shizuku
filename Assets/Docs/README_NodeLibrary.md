# 节点库自动发现系统

## 概述

节点库自动发现系统基于 Unity 的 SearchWindow 实现，通过反射自动扫描所有节点类型，并提供搜索和分类功能。

## 新增文件

### 1. NodeCategory.cs
节点分类枚举，定义了以下分类：
- `Basic` - 基础节点
- `Blueprint` - 蓝图节点
- `Math` - 数学节点
- `Logic` - 逻辑节点
- `Converter` - 类型转换节点
- `Event` - 事件节点

### 2. NodeMenuItemAttribute.cs
节点菜单标注特性，用于标记节点在创建菜单中的显示信息。

**属性：**
- `MenuPath` - 节点在菜单中的路径，使用 "/" 分隔（如 "数学/加法"）
- `Category` - 节点分类
- `Description` - 节点描述
- `Order` - 排序顺序

**使用示例：**
```csharp
[NodeMenuItem("数学/加法/整数", NodeCategory.Math, Description = "整数加法")]
public class AddNode_Int : ShizukuValueNode
{
    // 节点实现...
}
```

### 3. NodeSearchWindowProvider.cs
SearchWindow 提供者，负责：
- 自动扫描所有继承自 `ShizukuNodeBase` 的节点类型
- 按分类组织节点
- 提供搜索和筛选功能
- 处理节点创建

## 使用方法

### 创建节点

1. **使用 SearchWindow（推荐）**
   - 在图编辑器空白处点击右键
   - 系统会自动打开 SearchWindow
   - 按分类浏览或直接搜索节点
   - 点击选择即可创建

2. **传统右键菜单**
   - 旧的右键菜单仍然保留
   - 可以通过菜单项创建特定节点

### 为自定义节点添加菜单项

只需在节点类上添加 `NodeMenuItem` 特性：

```csharp
[NodeMenuItem("自定义分类/自定义节点", NodeCategory.Basic, Description = "这是一个自定义节点")]
public class MyCustomNode : ShizukuValueNode
{
    public override string Title => "My Custom Node";
    
    // 节点实现...
}
```

## 已标注的节点

所有现有节点都已添加 `NodeMenuItem` 特性：

### 基础节点
- 加一节点 (`ShizikuAddOneNode`)
- 打印节点 (`ShizukuLogNode`)

### 逻辑节点
- 条件分支 (`ShizukuIfNode`)

### 数学节点
所有数学运算节点都已分类到 "数学" 分类下：
- **加法**: Int, Float, Vector2, Vector3
- **减法**: Int, Float, Vector2, Vector3
- **乘法**: Int, Float, Vector2, Vector3
- **除法**: Int, Float, Vector2, Vector3

### 蓝图节点
- **获取属性**: Float, Int, Bool, String, 通用
- **设置属性**: Float, Int, Bool, String, 通用

## 技术细节

### 节点扫描
`NodeSearchWindowProvider.ScanAllNodeTypes()` 方法会：
1. 遍历所有程序集（跳过系统程序集）
2. 查找继承自 `ShizukuNodeBase` 的类
3. 读取 `NodeMenuItem` 特性
4. 对于没有特性的节点，使用默认配置

### 特殊节点处理
以下节点不会出现在 SearchWindow 中：
- `ShizukuRootNode` - 根节点（通过右键菜单创建）
- `BlueprintEventNode` - 事件节点（通过蓝图事件菜单创建）
- `TypeConverterNode` 及其子类 - 转换节点（自动插入）

### 集成到 ShizukuGraphView
在 `ShizukuGraphView` 构造函数中注册：
```csharp
nodeCreationRequest = context =>
{
    SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), 
                      CreateNodeSearchWindowProvider(context.screenMousePosition));
};
```

## 优势

1. **自动化** - 新增节点无需修改菜单代码
2. **可搜索** - 支持节点名称搜索
3. **分类清晰** - 按功能分类组织
4. **易于扩展** - 只需添加特性即可
5. **用户友好** - 类似 Unity Shader Graph 的体验

## 后续改进建议

1. **节点预览** - 显示节点的简要说明或示例图
2. **最近使用** - 记录最近创建的节点
3. **收藏功能** - 允许用户收藏常用节点
4. **快捷键** - 支持键盘快捷键快速搜索
5. **节点图标** - 为不同类型的节点添加图标
6. **使用统计** - 统计节点使用频率并排序

## 注意事项

1. 节点类必须有无参构造函数
2. 节点类不能是抽象类
3. MenuPath 使用 "/" 分隔层级
4. 同一分类下的节点会自动合并分组

