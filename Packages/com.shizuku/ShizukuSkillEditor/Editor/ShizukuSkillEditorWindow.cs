using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shizuku.SkillEditor.Editor
{
    public class ShizukuSkillEditorWindow : EditorWindow
    {
        // ---- 时间轴 ----
        private VisualElement _toolbar;
        private VisualElement _timelineContainer;
        private VisualElement _inspector;
        private VisualElement _contentContainer;

        [MenuItem("Shizuku/Skill Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<ShizukuSkillEditorWindow>();
            window.titleContent = new GUIContent("Skill Editor");
            window.minSize = new Vector2(800, 400);
        }

        private void OnEnable()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            rootVisualElement.Clear();

            // ===== 顶部工具栏 =====
            _toolbar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingLeft = 8,
                    paddingRight = 8,
                    paddingTop = 2,
                    paddingBottom = 2,
                    backgroundColor = new Color(0.22f, 0.22f, 0.22f, 1f),
                    borderBottomWidth = 1,
                    borderBottomColor = new Color(0.1f, 0.1f, 0.1f, 1f),
                    height = 30
                }
            };

            _toolbar.Add(CreateToolbarButton("新建", "创建新技能配置", OnNewSkill));
            _toolbar.Add(CreateToolbarButton("打开", "打开已有技能配置", OnOpenSkill));
            _toolbar.Add(CreateToolbarButton("保存", "保存当前技能配置", OnSaveSkill));
            _toolbar.Add(CreateToolbarSeparator());
            _toolbar.Add(CreateToolbarButton("▶", "播放预览", OnPlay));
            _toolbar.Add(CreateToolbarButton("⏸", "暂停预览", OnPause));
            _toolbar.Add(CreateToolbarButton("⏹", "停止预览", OnStop));

            rootVisualElement.Add(_toolbar);

            // ===== 主内容区（水平分割：左侧时间轴 + 右侧检查器） =====
            _contentContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexGrow = 1
                }
            };
            rootVisualElement.Add(_contentContainer);

            // ---- 左侧：时间轴区域 ----
            _timelineContainer = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    backgroundColor = new Color(0.18f, 0.18f, 0.18f, 1f)
                }
            };

            var timelinePlaceholder = new Label("时间轴区域（待实现）")
            {
                style =
                {
                    unityTextAlign = TextAnchor.MiddleCenter,
                    flexGrow = 1,
                    color = new Color(0.5f, 0.5f, 0.5f),
                    fontSize = 16
                }
            };
            _timelineContainer.Add(timelinePlaceholder);
            _contentContainer.Add(_timelineContainer);

            // ---- 右侧：检查器面板 ----
            _inspector = new VisualElement
            {
                style =
                {
                    width = 280,
                    borderLeftWidth = 1,
                    borderLeftColor = new Color(0.13f, 0.13f, 0.13f, 1f),
                    backgroundColor = new Color(0.22f, 0.22f, 0.22f, 1f),
                    paddingTop = 8,
                    paddingBottom = 8,
                    paddingLeft = 8,
                    paddingRight = 8
                }
            };

            var inspectorHeader = new Label("检查器")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 13,
                    paddingBottom = 8,
                    borderBottomWidth = 1,
                    borderBottomColor = new Color(0.13f, 0.13f, 0.13f, 1f)
                }
            };
            _inspector.Add(inspectorHeader);

            var inspectorPlaceholder = new Label("选中轨道或关键帧后显示属性")
            {
                style =
                {
                    paddingTop = 16,
                    color = new Color(0.5f, 0.5f, 0.5f),
                    unityTextAlign = TextAnchor.UpperCenter
                }
            };
            _inspector.Add(inspectorPlaceholder);

            _contentContainer.Add(_inspector);
        }

        #region Toolbar Helpers

        private Button CreateToolbarButton(string text, string tooltip, System.Action onClick)
        {
            var btn = new Button(onClick)
            {
                text = text,
                tooltip = tooltip,
                style =
                {
                    marginRight = 4,
                    paddingLeft = 8,
                    paddingRight = 8,
                    height = 22
                }
            };
            return btn;
        }

        private VisualElement CreateToolbarSeparator()
        {
            return new VisualElement
            {
                style =
                {
                    width = 1,
                    marginLeft = 4,
                    marginRight = 4,
                    backgroundColor = new Color(0.35f, 0.35f, 0.35f, 1f),
                    alignSelf = Align.Stretch
                }
            };
        }

        #endregion

        #region Toolbar Callbacks

        private void OnNewSkill()
        {
            Debug.Log("[SkillEditor] 新建技能配置（待实现）");
        }

        private void OnOpenSkill()
        {
            Debug.Log("[SkillEditor] 打开技能配置（待实现）");
        }

        private void OnSaveSkill()
        {
            Debug.Log("[SkillEditor] 保存技能配置（待实现）");
        }

        private void OnPlay()
        {
            Debug.Log("[SkillEditor] 播放预览（待实现）");
        }

        private void OnPause()
        {
            Debug.Log("[SkillEditor] 暂停预览（待实现）");
        }

        private void OnStop()
        {
            Debug.Log("[SkillEditor] 停止预览（待实现）");
        }

        #endregion
    }
}

