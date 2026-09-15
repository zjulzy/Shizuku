# Shizuku

Shizuku 是面向 Unity Gameplay 编程的可视化 Graph / Blueprint 系统，完整包包含 Core、Graph、DebugKit、SkillEditor、SkillEditorGraph 和 Tag 模块。

## 环境要求

- Unity 6000.2 或更高版本
- Odin Inspector（请在安装 Shizuku 前先安装）
- 可选：.NET 8 SDK 或更高版本（仅首次构建 Shizuku MCP Server 时需要）

## 安装

在 Unity Package Manager 中选择 `Install package from git URL...`，输入：

```text
https://github.com/zjulzy/Shizuku.git?path=/Packages/com.shizuku#v0.8.0
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

## 图编辑器配置辅助

- `＋ 节点` 或画布创建菜单：按名称、中文用途或别名搜索；空搜索优先显示最近使用的节点。
- 从端口拖到空白处，或右键端口选择“创建并连接节点”：筛选兼容节点。只有一个兼容端口时自动连接，多个端口时选择目标；尚未配置资源的动态端口可取消筛选后创建。
- 点击输入旁的 `← 来源节点.端口` 定位来源。未连线的输入编辑的是序列化默认值，而不是运行时值。
- 工具栏“检查配置”检查当前主图或函数子图的必填项、数值范围及缺失节点引用；节点内也显示字段警告。这是静态辅助，不代替运行测试，也不会自动添加失败分支或清理逻辑。
- 参数、连线、节点及变量/函数编辑支持 Unity Undo/Redo；修改资源触发的动态端口和边变更与字段修改一起撤销。

## 图资产版本兼容

图资产使用独立的整数 `SchemaVersion`，只在 Shizuku 图容器的序列化格式发生不兼容变化时递增，不跟随 Package 版本。打开旧版本图时，Graph Editor 会按版本顺序执行迁移，全部成功后保存并重新导入资产；高于当前支持版本的图会拒绝打开，避免旧框架覆盖新数据。

如果 Unity 无法反序列化某个节点、端口或自定义变量类型，编辑器会显示 assembly、namespace、class 和 reference ID，并停止迁移和编辑。框架不会自动清除缺失数据：请恢复依赖程序集，或在类型移动时使用 `[MovedFrom]`、字段改名时使用 `[FormerlySerializedAs]`；确定废弃的节点应由明确的版本迁移步骤替换或删除。

自定义节点继续使用 Unity 的 `[Tooltip]`、`[Min]`、`[Range]` 和 PropertyDrawer，可额外添加：

```csharp
[NodeMenuItem("流程/Wait", Description = "等待指定时长", Keywords = "延迟 暂停 等待")]
// 放在节点类型上；MenuPath 仍是静态名称的唯一来源。
```

字段配置示例：

```csharp
[SerializeField, Min(0), Tooltip("等待后继续执行"),
 NodeField("等待时长", Unit = "秒", Summary = true)]
private float seconds = 1;
```

`NodeField.Required` 用于必要的资产/对象输入；已连线输入视为已提供来源，但无法静态保证来源运行时非空。`NodeField` 只改变显示，不改变字段名或端口键。生成的 ID、端口结构缓存请标记 `[HideInInspector]`，业务配置集合仍可使用原生序列化编辑器。

## MCP Agent 集成

在 `Edit > Project Settings > Shizuku > MCP` 中生成项目专用的 stdio MCP Server 可执行文件，并可自动配置当前电脑上检测到的 Codex、Claude Code 和 Cursor。生成操作只编译文件，不会开启常驻服务。Codex 与 Claude Code 使用各自 CLI；Cursor 仅合并项目下 `.cursor/mcp.json` 中带项目哈希的 Shizuku 条目，并在覆盖已有文件前创建备份。其他兼容 stdio MCP 的 Agent 可复制页面提供的通用配置后手动接入。

MCP Server 由 Agent 按需启动，Unity Editor 内的 loopback 桥随项目自动启动。桥只监听 `127.0.0.1`，并使用保存在 `Library/ShizukuMcp` 下、每次启动都会轮换的随机令牌。首批工具支持列出图资产、查询节点目录、读取图、校验图，以及通过 dry-run 与 revision 检查事务式修改图。

更完整的使用方式、节点扩展和架构说明见 [项目文档](https://github.com/zjulzy/Shizuku#readme)。

## 许可证

[MIT](LICENSE)
