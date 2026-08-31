# 节点创建菜单实现说明

## 当前结构

- `NodeMenuItemAttribute`：保存完整 `MenuPath`、说明和排序；路径末段提供 `DisplayName`。
- `NodeSearchWindowProvider`：反射扫描显式标注的节点，按完整路径动态构建任意层级搜索树。
- `ShizukuNodeBase`：静态节点默认从 `NodeMenuItem.DisplayName` 获取标题。
- `ShizukuGraphView`：普通节点统一使用搜索树；上下文菜单只维护根节点、蓝图事件和分组等动态或受控入口。
- `UnifiedShizukuGeneratorTab`：生成函数节点和自定义变量节点时写入完整 `MenuPath`，不生成重复的静态标题。

## 唯一来源

```text
NodeMenuItem.MenuPath，例如 数学/Add (Float)
        ├── 前置路径段：中文搜索树分组
        └── 最后一段：英文菜单名 + 静态节点默认标题
```

系统不再包含 `NodeCategory`，也不会把类名、声明类型名或文件夹结构自动暴露为菜单文本。未标注节点不会进入搜索树。

## 动态入口边界

以下内容不能仅靠静态特性表达，因此仍由编辑器按当前图状态构建：

- 根节点：主图没有根节点时才允许创建；
- 蓝图事件：来自可用事件定义；
- 函数调用：来自当前图的函数列表；
- 分组和调试操作：不是节点类型。

## 自动化约束

EditMode 测试覆盖：

- `DisplayName` 来自完整路径的最后一段；
- 静态节点标题与菜单名称一致；
- 路径至少有两层，分组为中文、末段为英文且完整路径不重复；
- 搜索树按路径生成中文分组，并隐藏未标注的内部节点。

## 扩展约定

新增静态节点时只需维护一处：

```csharp
[NodeMenuItem("逻辑/Branch", Description = "根据条件选择执行分支")]
public class ShizukuIfNode : ShizukuRunnableNode
{
}
```

生成器或动态节点如果需要根据实例状态改变标题，可以继续覆写 `Title`；这不会改变其创建菜单路径。生成器元数据中的显示名也必须使用英文，分类使用中文，以便生成的 `NodeMenuItem.MenuPath` 遵守同一规范。
