# 更新日志

本文档记录 Shizuku 项目的所有重要变更。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)。

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
