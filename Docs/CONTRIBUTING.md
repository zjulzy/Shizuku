# 贡献指南

感谢你对 Shizuku 项目的关注！我们欢迎所有形式的贡献。

---

## 🎯 贡献方式

### 1. 报告 Bug

发现问题？请通过 [GitHub Issues](链接) 报告。

**Bug 报告应包含**：
- 清晰的标题（描述问题）
- Unity 版本
- 详细的复现步骤
- 预期行为 vs 实际行为
- 截图或日志（如果适用）
- 环境信息（操作系统、显卡等）

**模板**：
```markdown
### 问题描述
简要描述遇到的问题

### 环境信息
- Unity 版本：6000.2.14.f1
- 操作系统：Windows 10
- Shizuku 版本：0.1.0

### 复现步骤
1. 创建一个新图
2. 添加 Root 节点
3. 点击保存
4. 观察到错误

### 预期行为
应该成功保存

### 实际行为
提示 NullReferenceException

### 截图/日志
[附上截图或日志]
```

### 2. 提出功能请求

有好想法？请通过 GitHub Issues 提出。

**功能请求应包含**：
- 清晰的标题
- 问题/需求描述（为什么需要这个功能？）
- 建议的解决方案
- 替代方案（可选）
- 相关资料（可选）

### 3. 提交代码

#### 准备工作

1. **Fork 仓库**
   ```bash
   # 在 GitHub 上点击 Fork 按钮
   ```

2. **克隆到本地**
   ```bash
   git clone https://github.com/your-username/Shizuku.git
   cd Shizuku
   ```

3. **创建分支**
   ```bash
   git checkout -b feature/my-new-feature
   # 或
   git checkout -b bugfix/fix-some-bug
   ```

#### 编码规范

**命名约定**：
```csharp
// 类名：PascalCase
public class MyNodeClass { }

// 公共属性/方法：PascalCase
public string NodeTitle { get; }
public void ExecuteNode() { }

// 私有字段：_camelCase
private string _nodeName;
private int _executionCount;

// 参数/局部变量：camelCase
public void Process(int inputValue)
{
    int localVariable = inputValue * 2;
}

// 常量：UPPER_SNAKE_CASE
private const int MAX_NODE_COUNT = 1000;
```

**代码风格**：
```csharp
// 1. 使用大括号（即使单行）
if (condition)
{
    DoSomething();
}

// 2. 空格规范
if (a == b)  // 运算符两边有空格
{
    Method(a, b);  // 逗号后有空格
}

// 3. 注释规范
/// <summary>
/// XML 文档注释（公共 API 必须）
/// </summary>
public void PublicMethod() { }

// 行内注释（解释复杂逻辑）
private void ComplexLogic()
{
    // 这里需要特殊处理循环依赖
    if (HasCycle())
    {
        return;
    }
}
```

**文件组织**：
```
Assets/
├── Scripts/              # 运行时代码
│   ├── Graph/            # 图相关
│   ├── Node/             # 节点相关
│   │   ├── DerivedNodes/ # 具体节点实现
│   │   └── ...
│   └── ...
└── Editor/               # 编辑器代码
    ├── Resources/        # USS/UXML 资源
    └── ...
```

#### 提交规范

**提交信息格式**：
```
<类型>(<范围>): <简短描述>

<详细描述>

<相关 Issue>
```

**类型**：
- `feat`: 新功能
- `fix`: Bug 修复
- `docs`: 文档更新
- `style`: 代码格式（不影响功能）
- `refactor`: 重构
- `test`: 测试相关
- `chore`: 构建/工具相关

**示例**：
```
feat(node): 添加延迟执行节点

- 实现 DelayNode 类
- 添加协程支持
- 更新节点菜单

Closes #123
```

#### 测试

**运行测试**：
```bash
# Unity Test Runner
Window → General → Test Runner → Run All
```

**编写测试**：
```csharp
[TestFixture]
public class MyNodeTests
{
    [Test]
    public void TestNodeExecution()
    {
        // Arrange
        var node = new MyNode();
        
        // Act
        node.Execute();
        
        // Assert
        Assert.AreEqual(expected, actual);
    }
}
```

#### 提交 Pull Request

1. **推送分支**
   ```bash
   git push origin feature/my-new-feature
   ```

2. **创建 PR**
   - 在 GitHub 上点击 "New Pull Request"
   - 填写 PR 描述（参考模板）
   - 等待 Review

**PR 描述模板**：
```markdown
## 变更类型
- [ ] Bug 修复
- [ ] 新功能
- [ ] 文档更新
- [ ] 代码重构
- [ ] 性能优化

## 变更说明
描述你做了什么改动，为什么这样做。

## 相关 Issue
Closes #123

## 测试清单
- [ ] 单元测试通过
- [ ] 手动测试通过
- [ ] 文档已更新

## 截图/演示
[如果是 UI 相关，请附上截图或 GIF]
```

### 4. 改进文档

文档同样重要！你可以：
- 修正错误
- 补充示例
- 翻译文档
- 编写教程

文档位置：
- `README.md` - 主文档
- `Docs/QUICK_REFERENCE.md` - 快速参考
- `Docs/ARCHITECTURE.md` - 架构设计
- `Assets/Scripts/Node/节点系统说明.md` - 节点系统文档

---

## 🎨 贡献节点

### 节点开发清单

创建新节点时，请确保：

- [ ] 继承自 `ShizukuNodeBase`
- [ ] 实现所有抽象方法
- [ ] 添加 XML 文档注释
- [ ] 设置合适的颜色和标题
- [ ] 测试边界情况
- [ ] 添加使用示例
- [ ] 更新菜单（如果需要）

### 节点质量标准

**必需**：
- ✅ 功能正确
- ✅ 无编译错误/警告
- ✅ 代码符合规范
- ✅ 有基本注释

**推荐**：
- 🌟 单元测试
- 🌟 使用示例
- 🌟 性能优化
- 🌟 错误处理

**加分项**：
- 💎 示例场景
- 💎 视频教程
- 💎 多语言支持

---

## 📋 开发流程

### 新功能开发

1. **讨论阶段**
   - 创建 Issue 描述想法
   - 等待维护者反馈
   - 确认设计方案

2. **开发阶段**
   - 创建功能分支
   - 编写代码
   - 编写测试
   - 更新文档

3. **审查阶段**
   - 提交 PR
   - 响应 Review 意见
   - 修改代码

4. **合并阶段**
   - 通过所有检查
   - 维护者合并
   - 删除分支

### Bug 修复流程

1. **确认 Bug**
   - 复现问题
   - 定位原因

2. **修复**
   - 创建修复分支
   - 编写修复代码
   - 添加回归测试

3. **验证**
   - 确认修复有效
   - 检查无副作用

4. **提交**
   - 提交 PR
   - 关联原 Issue

---

## 🔍 代码审查标准

我们会从以下方面审查代码：

### 功能性
- ✅ 实现了预期功能
- ✅ 无明显 Bug
- ✅ 边界情况处理

### 代码质量
- ✅ 符合编码规范
- ✅ 命名清晰易懂
- ✅ 逻辑清晰简洁
- ✅ 无重复代码

### 性能
- ✅ 无明显性能问题
- ✅ 避免不必要的分配
- ✅ 合理使用缓存

### 可维护性
- ✅ 代码结构清晰
- ✅ 注释充分
- ✅ 易于扩展

### 测试
- ✅ 有相关测试
- ✅ 测试覆盖核心逻辑
- ✅ 测试通过

---

## 🤝 社区准则

### 行为规范

我们承诺为每个人提供一个友好、安全、包容的环境。

**应该做**：
- ✅ 友好和尊重
- ✅ 包容不同观点
- ✅ 接受建设性批评
- ✅ 专注于对社区最有利的事情
- ✅ 同理心对待他人

**不应该做**：
- ❌ 使用性化语言或图像
- ❌ 人身攻击或政治攻击
- ❌ 公开或私下骚扰
- ❌ 未经许可发布他人隐私
- ❌ 其他不专业或不受欢迎的行为

### 沟通渠道

- **GitHub Issues** - Bug 报告和功能请求
- **GitHub Discussions** - 一般讨论和问答
- **QQ 群** - 即时交流（中文）
- **Discord** - 即时交流（英文）

---

## 📜 许可协议

贡献代码即表示你同意：
1. 你的代码将以 MIT 许可证发布
2. 你拥有代码的版权，或有权贡献
3. 你理解并接受《贡献者许可协议》

---

## 🙏 致谢

感谢每一位贡献者！

你的名字将出现在：
- [CONTRIBUTORS.md](CONTRIBUTORS.md)
- 项目 README
- 发布说明

---

## 📞 联系方式

有问题？随时联系我们：
- **Email**: [Your Email]
- **GitHub**: [@YourUsername](https://github.com/YourUsername)
- **QQ 群**: [QQ Group Number]

---

**最后更新**：2026-01-25
