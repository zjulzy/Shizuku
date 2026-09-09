# Shizuku

Shizuku 是面向 Unity Gameplay 编程的可视化 Graph / Blueprint 系统，完整包包含 Core、Graph、DebugKit、SkillEditor、SkillEditorGraph 和 Tag 模块。

## 环境要求

- Unity 6000.2 或更高版本
- Odin Inspector（请在安装 Shizuku 前先安装）

## 安装

在 Unity Package Manager 中选择 `Install package from git URL...`，输入：

```text
https://github.com/zjulzy/Shizuku.git?path=/Packages/com.shizuku#v0.4.0
```

若需要 Tag 模块，在 `Edit > Project Settings > Shizuku` 中勾选 `Enable Tag Module`。该选项会为当前 Build Target 管理 `SHIZUKU_TAG` 宏；关闭后 Tag 程序集不会参与编译。

## 基本入口

- 在 Project 窗口右键选择 `Create > Shizuku > Graph` 创建普通图
- 双击图资产进入编辑器
- 使用 `GraphRunner` 运行普通图
- 继承 `BlueprintBehavior` 与 `ShizukuBluePrint<T>` 创建可覆写的组件蓝图

更完整的使用方式、节点扩展和架构说明见 [项目文档](https://github.com/zjulzy/Shizuku#readme)。

## 许可证

[MIT](LICENSE)
