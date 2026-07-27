# 更新日志

本文档记录 Shizuku 项目的所有重要变更。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)。

---

## [0.3.0] - 2026-07-28

### ✨ 新增

- **技能编辑器** - 基于 PlayableGraph 的技能时间轴编辑系统
  - 实现基础架构和编辑器窗口
  - Skill Player 创建独立 PlayableGraph
  - 技能编辑器与蓝图系统的桥接集成
- **调试系统** - 运行时节点执行调试
  - 单步调试和断点功能
  - 链尾自动继续执行
  - 运行时图拷贝隔离
- **ShizukuMethod / 图方法** - 支持在图中定义和调用子方法
- **ShizukuClass / ShizukuFunction** - 通过 Attribute 将 C# 类型和方法暴露给蓝图
  - 自定义类型及函数调用代码生成器
  - Get/Set 变量节点自动生成
- **Tag 插件** - 标签标记系统
- **节点搜索窗口** - 快速搜索和添加节点
- **变量系统** - 自定义蓝图变量，右侧变量面板
- **逻辑运算节点** - AND / OR / NOT 等运算
- **数学运算节点** - 基础数学运算（加减乘除等）
- **节点 Inspector** - 在 Inspector 面板查看和编辑节点属性
- **蓝图事件传参** - 完善事件节点参数传递

### 🔧 改进

- 插件结构拆分为独立子包（Core / Graph / Tag / DebugKit / SkillEditor / SkillEditorGraph）
- 节点间数据类型传递优化，类型转换显式化
- 优化端口默认值逻辑
- 扩展端口数据类型支持
- 蓝图行为属性对蓝图侧屏蔽
- 值节点计算缓存与标脏机制
- 蓝图覆写方法支持返回值
- 编辑器 UI 外观优化
- 蓝图代码生成器工具

### 🐛 修复

- 修复进入 Play 模式时编辑器内容被清空的问题

---

## [0.2.0] - 2026-01-25

### ✨ 新增

- **CRTP 泛型模式** - `BlueprintBehavior<T>` 自引用泛型，提供编译时类型安全
  ```csharp
  public class EnemyBehavior : BlueprintBehavior<EnemyBehavior>
  ```

- **BlueprintOverridable Attribute** - 简化蓝图重写模板代码
  ```csharp
  [BlueprintOverridable]
  public virtual void TakeDamage(float damage)
  {
      if (TryExecuteBlueprintOverride(nameof(TakeDamage), damage))
          return;
      // 默认逻辑
  }
  ```

### ⚡ 性能优化

- **方法缓存机制** - `TryExecuteBlueprintOverride` 首次反射后缓存，后续调用从 ~100μs 降至 ~1μs
- **静态属性访问器缓存** - 同类型实例共享缓存，100 个实例初始化从 ~50ms 降至 ~0.5ms
- **反射优化** - 移除运行时 `InitializeBehavior` 反射调用，改为直接泛型方法调用

### 🔧 改进

- **事件系统重构** - 移除不必要的 `BindToBehavior` 调用，简化事件注册流程
- **内存管理** - 添加 `OnDestroy` 清理逻辑，防止内存泄漏
- **端口可见性** - `BlueprintEventNode._nextPort` 改为 private，通过基类反射自动发现
- **序列化修复** - 移除 `_blueprint` 字段的 `readonly` 修饰符，确保 Unity 可以正确序列化

### 📚 文档

- 新增 `ARCHITECTURE.md` 中 Blueprint 系统详细设计说明
- 更新 `QUICK_REFERENCE.md` 添加 v0.2.0 新特性使用示例
- 更新 `README.md` 核心特性描述

### 🐛 修复

- 修复 `BlueprintBehavior` 字段无法序列化的问题（移除 readonly）
- 修复 `BlueprintEventNode` 端口反射找不到的问题（字段可见性）

---

## [0.1.0] - 2025-01-XX

### ✨ 初始版本

- 基于 Unity GraphView 的可视化节点编辑器
- 双端口系统（控制流 + 参数）
- ScriptableObject 数据持久化
- 基础节点类型（Root、+1、Log、If）
- 环检测系统
- 分组功能
- 蓝图组件系统（BlueprintBehavior + ShizukuBluePrint）
- 事件系统基础实现
- 属性访问系统（反射）

---

## 版本号规范

- **主版本号（Major）**：不兼容的 API 变更
- **次版本号（Minor）**：向下兼容的功能新增
- **修订号（Patch）**：向下兼容的问题修正

---

**完整更新历史**: [GitHub Releases](https://github.com/your-repo/releases)
