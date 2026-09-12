# Shizuku

Shizuku 是面向 Unity Gameplay 编程的可视化 Graph / Blueprint 系统，完整包包含 Core、Graph、DebugKit、SkillEditor、SkillEditorGraph 和 Tag 模块。

## 环境要求

- Unity 6000.2 或更高版本
- Odin Inspector（请在安装 Shizuku 前先安装）

## 安装

在 Unity Package Manager 中选择 `Install package from git URL...`，输入：

```text
https://github.com/zjulzy/Shizuku.git?path=/Packages/com.shizuku#v0.6.0
```

若需要 Tag 模块，在 `Edit > Project Settings > Shizuku` 中勾选 `Enable Tag Module`。该选项会为当前 Build Target 管理 `SHIZUKU_TAG` 宏；关闭后 Tag 程序集不会参与编译。

## 基本入口

- 在 Project 窗口右键选择 `Create > Shizuku > Graph` 创建普通图
- 双击图资产进入编辑器
- 使用 `GraphRunner` 运行普通图
- 继承 `BlueprintBehavior` 与 `ShizukuBluePrint<T>` 创建可覆写的组件蓝图
- 在 `Shizuku > Generator Window` 中配置 Blueprint、函数节点、变量节点和自定义端口的项目级输出路径
- 使用 `ShizukuLatentNode` 编写跨帧节点；普通 Graph 与无返回值 Blueprint Event 支持延迟恢复控制流

## 外部节点的动态参数端口

自定义节点可实现 `IDynamicParameterPortProvider`，通过 `DynamicParameterPorts` 提供保存在集合中的动态输入/输出端口，并在 `SynchronizeDynamicParameterPorts` 中根据资产字段同步端口。Graph Editor 会在载入、刷新以及运行时初始化时调用该同步入口；端口稳定身份、重命名后的边迁移和删除失效边仍由节点实现负责。

若端口结构依赖 Inspector 中的序列化字段，再实现 `INodeSerializedFieldChangeHandler`。字段写回后会收到字段名和当前 `INodeContext`；返回 `true` 即可请求安全地重建当前图视图与节点 Inspector。该机制同样适用于通过 Addressables 原生 PropertyDrawer 编辑的 `AssetReference` 派生字段，框架不依赖具体资产类型。

更完整的使用方式、节点扩展和架构说明见 [项目文档](https://github.com/zjulzy/Shizuku#readme)。

## 许可证

[MIT](LICENSE)
