using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shizuku.SkillEditor.Editor
{
    public class ShizukuSkillEditorWindow : EditorWindow
    {
        private VisualElement _toolbar;
        private TimelineView _timelineView;
        private ScrollView _inspector;

        // 检查器三层容器
        private VisualElement _skillSection;
        private VisualElement _trackSection;
        private VisualElement _clipSection;

        private ShizukuSkillConfig _config;
        private SkillTrack _selectedTrack;
        private SkillClip _selectedClip;

        [MenuItem("Shizuku/Skill Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<ShizukuSkillEditorWindow>();
            window.titleContent = new GUIContent("Skill Editor");
            window.minSize = new Vector2(800, 400);
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceID);
            if (obj is ShizukuSkillConfig config)
            {
                var window = GetWindow<ShizukuSkillEditorWindow>();
                window.titleContent = new GUIContent("Skill Editor");
                window.minSize = new Vector2(800, 400);
                window.LoadConfig(config);
                return true;
            }
            return false;
        }

        public void LoadConfig(ShizukuSkillConfig config)
        {
            _config = config;
            _selectedTrack = null;
            _selectedClip = null;
            if (_timelineView != null)
                _timelineView.SetConfig(_config);
            RefreshInspector();
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
                    paddingLeft = 8, paddingRight = 8,
                    paddingTop = 2, paddingBottom = 2,
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

            // ===== 主内容区 =====
            var contentContainer = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, flexGrow = 1 }
            };
            rootVisualElement.Add(contentContainer);

            // ---- 左侧：时间轴 ----
            _timelineView = new TimelineView();
            _timelineView.OnClipSelected += OnClipSelected;
            _timelineView.OnTrackSelected += OnTrackSelected;
            _timelineView.OnSelectionCleared += OnSelectionCleared;
            contentContainer.Add(_timelineView);

            // ---- 右侧：检查器（可滚动） ----
            _inspector = new ScrollView(ScrollViewMode.Vertical)
            {
                style =
                {
                    width = 280,
                    borderLeftWidth = 1,
                    borderLeftColor = new Color(0.13f, 0.13f, 0.13f, 1f),
                    backgroundColor = new Color(0.22f, 0.22f, 0.22f, 1f),
                }
            };

            _skillSection = new VisualElement();
            _trackSection = new VisualElement();
            _clipSection = new VisualElement();
            _inspector.Add(_skillSection);
            _inspector.Add(_trackSection);
            _inspector.Add(_clipSection);
            contentContainer.Add(_inspector);

            // 如果已有 config 则刷新
            if (_config != null)
                _timelineView.SetConfig(_config);
            RefreshInspector();
        }

        // ============================================================
        // 检查器 - 分层刷新
        // ============================================================

        private static readonly Color SectionBorderColor = new(0.13f, 0.13f, 0.13f, 1f);

        private VisualElement CreateSectionHeader(string title)
        {
            return new Label(title)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 12,
                    paddingTop = 8, paddingBottom = 4,
                    paddingLeft = 8,
                    borderBottomWidth = 1,
                    borderBottomColor = SectionBorderColor,
                    color = new Color(0.8f, 0.8f, 0.8f),
                }
            };
        }

        private void RefreshInspector()
        {
            RefreshSkillSection();
            RefreshTrackSection();
            RefreshClipSection();
        }

        private void RefreshSkillSection()
        {
            _skillSection.Clear();
            if (_config == null)
            {
                _skillSection.Add(new Label("未加载技能配置")
                {
                    style = { paddingTop = 16, paddingLeft = 8, color = new Color(0.5f, 0.5f, 0.5f), unityTextAlign = TextAnchor.UpperCenter }
                });
                return;
            }

            _skillSection.Add(CreateSectionHeader("技能信息"));
            _skillSection.Add(new IMGUIContainer(() =>
            {
                if (_config == null) return;
                EditorGUI.BeginChangeCheck();
                _config.SkillName = EditorGUILayout.TextField("技能名称", _config.SkillName);
                _config.Duration = EditorGUILayout.FloatField("总时长", _config.Duration);
                EditorGUILayout.LabelField("轨道数", _config.Tracks.Count.ToString());
                if (EditorGUI.EndChangeCheck())
                    EditorUtility.SetDirty(_config);
            })
            {
                style = { paddingLeft = 8, paddingRight = 8, paddingTop = 4, paddingBottom = 8 }
            });
        }

        private void RefreshTrackSection()
        {
            _trackSection.Clear();
            if (_selectedTrack == null) return;

            _trackSection.Add(CreateSectionHeader($"轨道 - {_selectedTrack.GetType().Name}"));
            var track = _selectedTrack;
            _trackSection.Add(new IMGUIContainer(() =>
            {
                if (track == null) return;
                EditorGUI.BeginChangeCheck();
                track.TrackName = EditorGUILayout.TextField("轨道名", track.TrackName);
                track.Enabled = EditorGUILayout.Toggle("启用", track.Enabled);
                EditorGUILayout.LabelField("Clip 数", track.Clips.Count.ToString());
                if (EditorGUI.EndChangeCheck() && _config != null)
                    EditorUtility.SetDirty(_config);
            })
            {
                style = { paddingLeft = 8, paddingRight = 8, paddingTop = 4, paddingBottom = 8 }
            });
        }

        private void RefreshClipSection()
        {
            _clipSection.Clear();
            if (_selectedClip == null) return;

            _clipSection.Add(CreateSectionHeader($"Clip - {_selectedClip.GetType().Name}"));
            var clip = _selectedClip;
            _clipSection.Add(new IMGUIContainer(() =>
            {
                if (clip == null) return;
                EditorGUI.BeginChangeCheck();
                clip.StartTime = EditorGUILayout.FloatField("开始时间", clip.StartTime);
                clip.Duration = EditorGUILayout.FloatField("时长", clip.Duration);

                switch (clip)
                {
                    case LogicClipData logic:
                        logic.EventName = EditorGUILayout.TextField("事件名", logic.EventName);
                        break;
                    case AnimationClipData anim:
                        anim.Clip = (AnimationClip)EditorGUILayout.ObjectField("动画", anim.Clip, typeof(AnimationClip), false);
                        anim.BlendIn = EditorGUILayout.FloatField("混入", anim.BlendIn);
                        anim.BlendOut = EditorGUILayout.FloatField("混出", anim.BlendOut);
                        break;
                    case VfxClipData vfx:
                        vfx.Prefab = (GameObject)EditorGUILayout.ObjectField("预制体", vfx.Prefab, typeof(GameObject), false);
                        vfx.AttachBone = EditorGUILayout.TextField("挂点", vfx.AttachBone);
                        vfx.Offset = EditorGUILayout.Vector3Field("偏移", vfx.Offset);
                        break;
                    case SfxClipData sfx:
                        sfx.Clip = (AudioClip)EditorGUILayout.ObjectField("音频", sfx.Clip, typeof(AudioClip), false);
                        sfx.Volume = EditorGUILayout.Slider("音量", sfx.Volume, 0f, 1f);
                        break;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    if (_config != null) EditorUtility.SetDirty(_config);
                    _timelineView.MarkDirtyRepaint();
                }
            })
            {
                style = { paddingLeft = 8, paddingRight = 8, paddingTop = 4, paddingBottom = 8 }
            });
        }

        private void OnClipSelected(SkillClip clip, SkillTrack track)
        {
            _selectedTrack = track;
            _selectedClip = clip;
            RefreshTrackSection();
            RefreshClipSection();
        }

        private void OnTrackSelected(SkillTrack track)
        {
            _selectedTrack = track;
            _selectedClip = null;
            RefreshTrackSection();
            RefreshClipSection();
        }

        private void OnSelectionCleared()
        {
            _selectedTrack = null;
            _selectedClip = null;
            RefreshTrackSection();
            RefreshClipSection();
        }

        // ============================================================
        // 工具栏回调
        // ============================================================
        private void OnNewSkill()
        {
            var path = EditorUtility.SaveFilePanelInProject("新建技能配置", "NewSkill", "asset", "选择保存位置");
            if (string.IsNullOrEmpty(path)) return;

            _config = CreateInstance<ShizukuSkillConfig>();
            _config.SkillName = System.IO.Path.GetFileNameWithoutExtension(path);
            _config.Duration = 3f;
            AssetDatabase.CreateAsset(_config, path);
            AssetDatabase.SaveAssets();
            _timelineView.SetConfig(_config);
            _selectedTrack = null;
            _selectedClip = null;
            RefreshInspector();
        }

        private void OnOpenSkill()
        {
            var path = EditorUtility.OpenFilePanel("打开技能配置", "Assets", "asset");
            if (string.IsNullOrEmpty(path)) return;
            path = FileUtil.GetProjectRelativePath(path);
            if (string.IsNullOrEmpty(path)) return;

            var config = AssetDatabase.LoadAssetAtPath<ShizukuSkillConfig>(path);
            if (config != null)
            {
                _config = config;
                _timelineView.SetConfig(_config);
                _selectedTrack = null;
                _selectedClip = null;
                RefreshInspector();
            }
        }

        private void OnSaveSkill()
        {
            if (_config != null)
            {
                EditorUtility.SetDirty(_config);
                AssetDatabase.SaveAssets();
                Debug.Log($"[SkillEditor] 已保存: {_config.SkillName}");
            }
        }

        private void OnPlay()  { Debug.Log("[SkillEditor] 播放预览（待实现）"); }
        private void OnPause() { Debug.Log("[SkillEditor] 暂停预览（待实现）"); }
        private void OnStop()  { Debug.Log("[SkillEditor] 停止预览（待实现）"); }

        // ============================================================
        // 工具栏辅助
        // ============================================================
        private Button CreateToolbarButton(string text, string tooltip, System.Action onClick)
        {
            return new Button(onClick)
            {
                text = text, tooltip = tooltip,
                style = { marginRight = 4, paddingLeft = 8, paddingRight = 8, height = 22 }
            };
        }

        private VisualElement CreateToolbarSeparator()
        {
            return new VisualElement
            {
                style =
                {
                    width = 1, marginLeft = 4, marginRight = 4,
                    backgroundColor = new Color(0.35f, 0.35f, 0.35f, 1f),
                    alignSelf = Align.Stretch
                }
            };
        }
    }
}
