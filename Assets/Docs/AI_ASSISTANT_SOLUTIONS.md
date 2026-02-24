# AI 助手实现方案

## 📋 当前状态

目前 AI 助手只是一个装模做样的界面，位于 `ShizukuGraphView.AIAssistant.cs`，功能包括：
- 📱 右下角浮动窗口
- 💬 消息气泡显示（蓝色 AI，绿色用户）
- ⌨️ 文本输入框
- 🔄 点击发送后显示"✅ 已完成"

需要实现真正的 AI 能力。

---

## 🎯 可行实现方案

### 方案 1：本地规则引擎（推荐作为第一步）⭐⭐⭐

**优点**：
- ✅ 无需外部依赖，完全离线
- ✅ 响应速度快（毫秒级）
- ✅ 无成本
- ✅ 可控性强，结果可预测

**缺点**：
- ❌ 功能有限，只能处理预定义的命令
- ❌ 无法理解自然语言
- ❌ 需要手动维护规则库

**实现方式**：

```csharp
public class ShizukuAIAssistant
{
    private Dictionary<string, System.Action<string>> _commands;
    
    public void Initialize()
    {
        _commands = new Dictionary<string, System.Action<string>>
        {
            // 创建节点
            { "创建", HandleCreateNode },
            { "添加", HandleCreateNode },
            
            // 连接节点
            { "连接", HandleConnectNodes },
            
            // 查找节点
            { "查找", HandleFindNode },
            { "搜索", HandleFindNode },
            
            // 帮助
            { "帮助", HandleHelp },
            { "help", HandleHelp },
        };
    }
    
    public string ProcessMessage(string userInput)
    {
        // 简单关键词匹配
        foreach (var cmd in _commands)
        {
            if (userInput.Contains(cmd.Key))
            {
                cmd.Value(userInput);
                return "✅ 已执行";
            }
        }
        
        return "❓ 不明白，请输入"帮助"查看可用命令";
    }
}
```

**支持的命令示例**：
```
用户：创建加法节点
AI：已创建 Add 节点

用户：连接节点1和节点2
AI：已连接

用户：查找 OnUpdate 事件
AI：已找到并聚焦

用户：帮助
AI：可用命令列表...
```

---

### 方案 2：调用云端 LLM API（GPT/Claude）⭐⭐⭐

**优点**：
- ✅ 强大的自然语言理解
- ✅ 可以处理复杂的对话
- ✅ 上下文记忆
- ✅ 代码生成能力

**缺点**：
- ❌ 需要 API Key 和网络连接
- ❌ 有调用成本（按 token 计费）
- ❌ 响应较慢（1-3 秒）
- ❌ 隐私问题（需要发送数据到云端）

**推荐 API**：
1. **OpenAI GPT-4** - 最强大，适合复杂任务
2. **Anthropic Claude** - 安全性好，上下文长
3. **Azure OpenAI** - 企业级，国内可用
4. **国内大模型** - 通义千问、文心一言、讯飞星火

**实现方式**：

```csharp
using System.Net.Http;
using Newtonsoft.Json;

public class CloudAIAssistant
{
    private const string API_KEY = "your-api-key";
    private const string API_URL = "https://api.openai.com/v1/chat/completions";
    
    private List<ChatMessage> _conversationHistory = new();
    
    public async Task<string> ProcessMessageAsync(string userInput)
    {
        // 添加系统提示
        if (_conversationHistory.Count == 0)
        {
            _conversationHistory.Add(new ChatMessage
            {
                role = "system",
                content = @"你是 Shizuku 蓝图系统的 AI 助手。
                           用户可以让你创建节点、连接节点、查找节点。
                           你可以调用以下函数：
                           - CreateNode(nodeType, position)
                           - ConnectNodes(fromNode, toNode)
                           - FindNode(name)"
            });
        }
        
        // 添加用户消息
        _conversationHistory.Add(new ChatMessage
        {
            role = "user",
            content = userInput
        });
        
        // 调用 API
        var response = await CallOpenAI(_conversationHistory);
        
        // 解析响应并执行函数调用
        if (response.function_call != null)
        {
            ExecuteFunction(response.function_call);
        }
        
        return response.content;
    }
    
    private async Task<ChatResponse> CallOpenAI(List<ChatMessage> messages)
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {API_KEY}");
            
            var request = new
            {
                model = "gpt-4",
                messages = messages,
                functions = GetAvailableFunctions(),
                temperature = 0.7
            };
            
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var result = await client.PostAsync(API_URL, content);
            var responseJson = await result.Content.ReadAsStringAsync();
            
            return JsonConvert.DeserializeObject<ChatResponse>(responseJson);
        }
    }
}
```

**成本估算**：
- GPT-4: $0.03/1K tokens (输入), $0.06/1K tokens (输出)
- 每次对话约 500-1000 tokens
- 成本约 $0.02-0.05/次

---

### 方案 3：本地小模型（ONNX Runtime）⭐⭐

**优点**：
- ✅ 完全离线，无网络依赖
- ✅ 无调用成本
- ✅ 响应较快（100-500ms）
- ✅ 隐私安全

**缺点**：
- ❌ 需要下载模型文件（100MB-1GB）
- ❌ 能力不如云端大模型
- ❌ 需要一定的硬件性能
- ❌ 集成复杂度较高

**推荐模型**：
1. **Phi-3 Mini** (3.8B) - 微软出品，适合代码任务
2. **TinyLlama** (1.1B) - 轻量级，速度快
3. **Gemma 2B** - Google 出品，质量较好

**实现方式**：

```csharp
using Microsoft.ML.OnnxRuntime;

public class LocalAIAssistant
{
    private InferenceSession _session;
    private Tokenizer _tokenizer;
    
    public void Initialize()
    {
        // 加载 ONNX 模型
        _session = new InferenceSession("Assets/AI/phi3-mini.onnx");
        _tokenizer = new Tokenizer("Assets/AI/tokenizer.json");
    }
    
    public string ProcessMessage(string userInput)
    {
        // 构建提示
        string prompt = $@"<|system|>
你是 Shizuku 蓝图助手。
<|user|>
{userInput}
<|assistant|>";
        
        // Tokenize
        var inputIds = _tokenizer.Encode(prompt);
        
        // 推理
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputIds)
        };
        
        using (var results = _session.Run(inputs))
        {
            var output = results[0].AsTensor<long>();
            var responseText = _tokenizer.Decode(output);
            return responseText;
        }
    }
}
```

---

### 方案 4：混合方案（规则引擎 + 云端 AI）⭐⭐⭐⭐⭐

**最佳实践！**

**策略**：
1. 先用规则引擎处理简单命令（90% 场景）
2. 复杂请求才调用云端 AI（10% 场景）
3. 用户可选是否启用云端 AI

**优点**：
- ✅ 快速响应（大部分情况）
- ✅ 成本可控
- ✅ 灵活性高
- ✅ 用户体验最佳

**实现方式**：

```csharp
public class HybridAIAssistant
{
    private LocalRuleEngine _ruleEngine;
    private CloudAIAssistant _cloudAI;
    private bool _cloudAIEnabled = false;
    
    public async Task<string> ProcessMessageAsync(string userInput)
    {
        // 1. 先尝试规则引擎（快速路径）
        var ruleResult = _ruleEngine.TryProcess(userInput);
        if (ruleResult.Success)
        {
            return ruleResult.Response;
        }
        
        // 2. 规则引擎无法处理，检查是否启用云端 AI
        if (!_cloudAIEnabled)
        {
            return "❓ 无法理解，是否启用云端 AI？输入 'enable ai' 启用。";
        }
        
        // 3. 调用云端 AI
        try
        {
            return await _cloudAI.ProcessMessageAsync(userInput);
        }
        catch (Exception ex)
        {
            return $"❌ AI 服务暂时不可用：{ex.Message}";
        }
    }
}
```

---

## 🎯 功能优先级

### 阶段 1：基础规则引擎（v0.5.0）⭐⭐⭐

**必须实现**：
- [x] UI 框架（已完成）
- [ ] 命令解析器
- [ ] 节点创建命令
- [ ] 节点查找命令
- [ ] 帮助系统

**时间估计**：2-3 天

---

### 阶段 2：智能提示（v0.6.0）⭐⭐

**功能**：
- [ ] 输入时的自动补全
- [ ] 命令历史记录
- [ ] 常用命令快捷按钮
- [ ] 错误提示和建议

**时间估计**：1-2 天

---

### 阶段 3：云端 AI 集成（v0.7.0）⭐⭐⭐

**功能**：
- [ ] OpenAI API 集成
- [ ] 配置界面（API Key 设置）
- [ ] 会话管理
- [ ] 成本监控

**时间估计**：3-5 天

---

### 阶段 4：高级功能（v0.8.0+）⭐

**功能**：
- [ ] 语音输入（Unity Microphone）
- [ ] 代码生成（生成自定义节点）
- [ ] 蓝图优化建议
- [ ] 学习用户习惯

**时间估计**：5-10 天

---

## 💡 实现建议

### 推荐路径

**短期目标（1-2 周）**：
```
方案 1（规则引擎） → 快速实现基础功能
```

**中期目标（1-2 月）**：
```
方案 4（混合方案） → 提供可选的 AI 能力
```

**长期目标（3-6 月）**：
```
探索方案 3（本地模型） → 提供离线 AI
```

### 设计原则

1. **渐进增强**：从简单到复杂
2. **用户可选**：AI 功能作为可选项
3. **优雅降级**：网络故障时回退到规则引擎
4. **隐私优先**：明确告知用户数据使用

---

## 📦 依赖库

### 规则引擎（方案 1）
- ✅ 无需额外依赖

### 云端 API（方案 2）
```json
// Packages/manifest.json
{
  "dependencies": {
    "com.unity.nuget.newtonsoft-json": "3.0.2"
  }
}
```

### 本地模型（方案 3）
```
- Microsoft.ML.OnnxRuntime (NuGet)
- Unity Sentis（Unity 官方 AI 推理引擎，推荐）
```

---

## 🎯 第一步实现：简单命令解析

### 最小可用版本（1 小时实现）

```csharp
private string ProcessSimpleCommand(string userInput)
{
    var input = userInput.ToLower().Trim();
    
    // 创建节点
    if (input.Contains("创建") || input.Contains("添加"))
    {
        if (input.Contains("根节点")) return CreateNodeCommand<ShizukuRootNode>();
        if (input.Contains("打印")) return CreateNodeCommand<ShizukuLogNode>();
        if (input.Contains("+1")) return CreateNodeCommand<ShizikuAddOneNode>();
        return "请指定节点类型，如：创建打印节点";
    }
    
    // 查找节点
    if (input.Contains("查找") || input.Contains("搜索"))
    {
        // 提取节点名称
        return "查找功能开发中...";
    }
    
    // 帮助
    if (input.Contains("帮助") || input == "help" || input == "?")
    {
        return @"可用命令：
        • 创建根节点
        • 创建打印节点
        • 创建+1节点
        • 查找 [节点名]
        • 帮助";
    }
    
    return "❓ 不明白，输入"帮助"查看命令列表";
}
```

---

## 📊 方案对比

| 方案 | 开发成本 | 运行成本 | 响应速度 | 能力 | 推荐度 |
|------|---------|---------|---------|------|--------|
| 规则引擎 | ⭐ | 免费 | 极快 | 基础 | ⭐⭐⭐⭐ |
| 云端 API | ⭐⭐ | $$ | 慢 | 强大 | ⭐⭐⭐⭐ |
| 本地模型 | ⭐⭐⭐ | 免费 | 中等 | 中等 | ⭐⭐ |
| 混合方案 | ⭐⭐⭐ | $ | 快 | 强大 | ⭐⭐⭐⭐⭐ |

---

## 🚀 下一步

1. **立即可做**：实现规则引擎基础命令
2. **短期规划**：完善命令库，添加智能提示
3. **中期规划**：集成云端 AI（可选功能）
4. **长期探索**：本地模型、语音输入等高级功能

建议从最简单的规则引擎开始，快速给用户提供可用的功能，再逐步增强！

