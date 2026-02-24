using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// ShizukuGraphView 的 AI 助手部分
/// </summary>
public partial class ShizukuGraphView
{
    #region AI 助手

    private void CreateAIAssistantWindow()
    {
        // 创建 AI 助手容器
        var aiContainer = new VisualElement();
        aiContainer.name = "ai-assistant-container";
        aiContainer.style.position = Position.Absolute;
        aiContainer.style.bottom = 20;
        aiContainer.style.right = 20;
        aiContainer.style.width = 320;
        aiContainer.style.minHeight = 400;
        aiContainer.style.maxHeight = 600;
        aiContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.95f);
        aiContainer.style.borderTopLeftRadius = 8;
        aiContainer.style.borderTopRightRadius = 8;
        aiContainer.style.borderBottomLeftRadius = 8;
        aiContainer.style.borderBottomRightRadius = 8;
        aiContainer.style.borderLeftWidth = 1;
        aiContainer.style.borderRightWidth = 1;
        aiContainer.style.borderTopWidth = 1;
        aiContainer.style.borderBottomWidth = 1;
        aiContainer.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        aiContainer.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        aiContainer.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        aiContainer.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        
        // 标题栏
        var titleBar = new VisualElement();
        titleBar.style.flexDirection = FlexDirection.Row;
        titleBar.style.justifyContent = Justify.SpaceBetween;
        titleBar.style.alignItems = Align.Center;
        titleBar.style.paddingLeft = 10;
        titleBar.style.paddingRight = 10;
        titleBar.style.paddingTop = 8;
        titleBar.style.paddingBottom = 8;
        titleBar.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
        titleBar.style.borderTopLeftRadius = 8;
        titleBar.style.borderTopRightRadius = 8;
        
        var titleLabel = new Label("🤖 AI 助手");
        titleLabel.style.color = new Color(0.7f, 0.9f, 1f, 1f);
        titleLabel.style.fontSize = 14;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        
        var minimizeButton = new Button(() => ToggleAIAssistant(aiContainer)) { text = "−" };
        minimizeButton.style.width = 24;
        minimizeButton.style.height = 24;
        minimizeButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        
        titleBar.Add(titleLabel);
        titleBar.Add(minimizeButton);
        
        // 消息区域
        var messageArea = new ScrollView();
        messageArea.name = "ai-message-area";
        messageArea.style.flexGrow = 1;
        messageArea.style.paddingLeft = 10;
        messageArea.style.paddingRight = 10;
        messageArea.style.paddingTop = 10;
        messageArea.style.paddingBottom = 10;
        
        // 欢迎消息
        var welcomeMessage = CreateMessageBubble("你好！我是 Shizuku 蓝图助手。有什么可以帮助你的吗？", true);
        messageArea.Add(welcomeMessage);
        
        // 输入区域
        var inputContainer = new VisualElement();
        inputContainer.style.flexDirection = FlexDirection.Row;
        inputContainer.style.paddingLeft = 10;
        inputContainer.style.paddingRight = 10;
        inputContainer.style.paddingTop = 8;
        inputContainer.style.paddingBottom = 10;
        inputContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
        inputContainer.style.borderBottomLeftRadius = 8;
        inputContainer.style.borderBottomRightRadius = 8;
        
        var inputField = new TextField();
        inputField.name = "ai-input-field";
        inputField.multiline = false;
        inputField.style.flexGrow = 1;
        inputField.style.marginRight = 5;
        
        var sendButton = new Button(() => OnAISendMessage(messageArea, inputField)) { text = "发送" };
        sendButton.style.width = 60;
        sendButton.style.backgroundColor = new Color(0.3f, 0.5f, 0.8f, 1f);
        sendButton.style.color = Color.white;
        
        // 支持回车发送
        inputField.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Return && !evt.shiftKey)
            {
                evt.StopPropagation();
                OnAISendMessage(messageArea, inputField);
            }
        });
        
        inputContainer.Add(inputField);
        inputContainer.Add(sendButton);
        
        // 组装 AI 助手窗口
        aiContainer.Add(titleBar);
        aiContainer.Add(messageArea);
        aiContainer.Add(inputContainer);
        
        // 添加到 GraphView
        Add(aiContainer);
    }
    
    private VisualElement CreateMessageBubble(string text, bool isAI)
    {
        var bubble = new VisualElement();
        bubble.style.marginBottom = 10;
        bubble.style.flexDirection = FlexDirection.Row;
        bubble.style.justifyContent = isAI ? Justify.FlexStart : Justify.FlexEnd;
        
        var messageLabel = new Label(text);
        messageLabel.style.backgroundColor = isAI 
            ? new Color(0.25f, 0.35f, 0.5f, 1f) 
            : new Color(0.3f, 0.5f, 0.3f, 1f);
        messageLabel.style.color = Color.white;
        messageLabel.style.paddingLeft = 10;
        messageLabel.style.paddingRight = 10;
        messageLabel.style.paddingTop = 8;
        messageLabel.style.paddingBottom = 8;
        messageLabel.style.borderTopLeftRadius = 10;
        messageLabel.style.borderTopRightRadius = 10;
        messageLabel.style.borderBottomLeftRadius = 10;
        messageLabel.style.borderBottomRightRadius = 10;
        messageLabel.style.maxWidth = 250;
        messageLabel.style.whiteSpace = WhiteSpace.Normal;
        
        bubble.Add(messageLabel);
        return bubble;
    }
    
    private void OnAISendMessage(ScrollView messageArea, TextField inputField)
    {
        var userMessage = inputField.value?.Trim();
        if (string.IsNullOrEmpty(userMessage))
            return;
        
        // 显示用户消息
        var userBubble = CreateMessageBubble(userMessage, false);
        messageArea.Add(userBubble);
        
        // 清空输入框
        inputField.value = "";
        
        // 延迟显示 AI 回复（模拟思考）
        EditorApplication.delayCall += () =>
        {
            var aiResponse = CreateMessageBubble("✅ 已完成", true);
            messageArea.Add(aiResponse);
            
            // 滚动到底部
            messageArea.ScrollTo(aiResponse);
        };
    }
    
    private void ToggleAIAssistant(VisualElement aiContainer)
    {
        var messageArea = aiContainer.Q("ai-message-area");
        var isVisible = messageArea.style.display == DisplayStyle.Flex;
        
        if (isVisible)
        {
            // 最小化
            messageArea.style.display = DisplayStyle.None;
            aiContainer.style.minHeight = 40;
        }
        else
        {
            // 展开
            messageArea.style.display = DisplayStyle.Flex;
            aiContainer.style.minHeight = 400;
        }
    }

    #endregion
}

