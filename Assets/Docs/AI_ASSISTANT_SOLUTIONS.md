# Shizuku MCP 集成

## 定位

Shizuku 不在 Graph Editor 内绑定某一家模型或聊天 UI。AI 能力由外部 Agent 提供，Shizuku 负责提供稳定、可验证的 MCP 工具。

```text
Codex / Claude Code / Cursor
           │ stdio MCP
           ▼
  Shizuku.Mcp.Server
           │ 127.0.0.1 + token
           ▼
   Unity Editor Bridge
           │
           ▼
   Shizuku Graph Service
```

独立 Server 使用 Model Context Protocol 项目维护的官方 C# SDK。Unity Editor 不承载 MCP SDK 或 ASP.NET Core，只运行轻量 TCP/JSON 桥；Player 构建不包含 Editor 端实现。

## 生命周期与安全边界

- Agent 使用 stdio 按需启动和停止 Server，无需用户常驻一个后台窗口。
- Unity 桥随 Editor 加载，并可在 `Project Settings > Shizuku > MCP` 中关闭。
- 桥仅监听 IPv4 loopback，不接受局域网连接。
- 发现文件和随机令牌位于项目 `Library/ShizukuMcp/bridge.json`；Domain Reload 后端口与令牌会更新。
- Server 每次调用前重新读取发现文件，因此可以跨 Domain Reload 恢复。
- 多项目使用项目路径哈希生成独立 MCP 服务名，避免配置互相覆盖。

## Agent 配置

首版自动支持：

- Codex：调用 `codex mcp add` 管理项目哈希命名的条目。
- Claude Code：调用 `claude mcp add --scope local` 写入该项目的本地配置。
- Cursor：结构化合并项目 `.cursor/mcp.json` 的 `mcpServers`，保留其他键和服务器，并为已有文件留备份。

不同 Agent 的配置存储并没有统一标准，因此页面不会宣称对未知客户端已经自动生效。其他客户端可以复制通用 stdio JSON，再按其配置格式套壳。

## 工具

- `shizuku_graph_list`：列出当前项目的 Graph / Blueprint 资产及 revision。
- `shizuku_node_catalog`：列出可创建节点的稳定类型 ID、菜单路径和能力。
- `shizuku_graph_read`：读取主图或指定 Method 子图的语义 JSON。
- `shizuku_graph_validate`：检查 GUID、根节点、端口、边和有向环。
- `shizuku_graph_apply`：事务式应用创建/删除节点、字段修改、控制流连接和参数连接。

`shizuku_graph_apply` 默认 `dryRun=true`。正式提交前应先读取图并保存 revision，再 dry-run；提交时传入同一 revision。若资产已经变化，工具返回冲突而不是覆盖新内容。

## 构建

首次使用需要电脑安装 .NET 8 SDK 或更高版本。在 `Project Settings > Shizuku > MCP` 点击 `Build MCP Server Executable (One-time Setup)` 后，源码会复制到项目 `Library` 并发布为当前 Editor 平台的 self-contained 单文件程序。该操作只生成可执行文件，不启动常驻服务；生成物不写入包目录，也不进入 Player。
