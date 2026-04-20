using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shizuku.SkillEditor.Editor
{
    /// <summary>
    /// 时间轴视图。渲染轨道头、时间刻度尺、Clip 色块，处理拖拽交互。
    /// </summary>
    public class TimelineView : VisualElement
    {
        // ---- 常量 ----
        private const float TrackHeight = 32f;
        private const float TrackHeaderWidth = 140f;
        private const float RulerHeight = 24f;
        private const float FrameRate = 30f;
        private const float PixelsPerSecond = 120f;
        private const float MinClipWidth = 6f;

        // ---- 数据 ----
        private ShizukuSkillConfig _config;
        private float _zoom = 1f;
        private float _scrollX;

        // ---- 选中状态 ----
        private SkillClip _selectedClip;
        private SkillTrack _selectedTrack;
        public event Action<SkillClip, SkillTrack> OnClipSelected;
        public event Action<SkillTrack> OnTrackSelected;
        public event Action OnSelectionCleared;

        // ---- Clip 拖拽 ----
        private SkillClip _dragClip;
        private enum DragMode { None, Move, ResizeLeft, ResizeRight }
        private DragMode _dragMode;
        private float _dragStartMouseX;
        private float _dragStartTime;
        private float _dragStartDuration;

        // ---- Clip 颜色映射 ----
        private static readonly Dictionary<Type, Color> ClipColors = new()
        {
            { typeof(AnimationClipData), new Color(0.3f, 0.6f, 0.9f) },
            { typeof(VfxClipData),       new Color(0.4f, 0.8f, 0.4f) },
            { typeof(SfxClipData),       new Color(0.9f, 0.7f, 0.2f) },
            { typeof(LogicClipData),     new Color(0.9f, 0.4f, 0.3f) },
        };

        // ---- Clip→Track 缓存 (编辑器用，反射扫描 [ClipForTrack]) ----
        private static Dictionary<Type, List<(Type clipType, string displayName)>> _trackClipMap;

        private Label _hintLabel;
        private IMGUIContainer _imguiOverlay;

        public TimelineView()
        {
            style.flexGrow = 1;
            style.backgroundColor = new Color(0.18f, 0.18f, 0.18f, 1f);
            focusable = true;
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<WheelEvent>(OnWheel);
            this.AddManipulator(new ContextualMenuManipulator(OnContextMenu));

            // IMGUI overlay for text (Painter2D cannot draw text)
            _imguiOverlay = new IMGUIContainer(DrawIMGUIOverlay)
            {
                pickingMode = PickingMode.Ignore,
                style = { position = Position.Absolute, left = 0, top = 0, right = 0, bottom = 0 }
            };
            Add(_imguiOverlay);

            _hintLabel = new Label("右键点击此处添加轨道")
            {
                style =
                {
                    position = Position.Absolute,
                    left = 0, right = 0, top = 60,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    color = new Color(0.5f, 0.5f, 0.5f),
                    fontSize = 14,
                    display = DisplayStyle.None
                }
            };
            Add(_hintLabel);
        }

        public void SetConfig(ShizukuSkillConfig config)
        {
            _config = config;
            _selectedClip = null;
            _selectedTrack = null;
            UpdateHintVisibility();
            MarkDirtyRepaint();
        }

        private void UpdateHintVisibility()
        {
            if (_hintLabel != null)
                _hintLabel.style.display = (_config != null && _config.Tracks.Count == 0)
                    ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public ShizukuSkillConfig Config => _config;

        // ============================================================
        // IMGUI 文字叠加层（轨道名 + 刻度数字）
        // ============================================================
        private void DrawIMGUIOverlay()
        {
            if (_config == null) return;
            float pps = PixelsPerSecond * _zoom;

            // ---- 左上角标题 ----
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                fontSize = 12
            };
            GUI.Label(new Rect(0, 0, TrackHeaderWidth, RulerHeight), "Tracks", titleStyle);

            // ---- 刻度数字 (每0.5秒显示时间标签) ----
            float visibleEnd = GetVisibleEndTime(pps);
            float ppf = pps / FrameRate; // pixels per frame
            int startFrame = Mathf.Max(0, Mathf.FloorToInt(_scrollX / ppf));
            int endFrame = Mathf.CeilToInt((contentRect.width - TrackHeaderWidth + _scrollX) / ppf) + 1;
            int maxFrame = Mathf.CeilToInt(visibleEnd * FrameRate);
            endFrame = Mathf.Min(endFrame, maxFrame);

            var rulerStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
                fontSize = 9
            };

            // 计算帧标签步进：缩放很小时不是每帧都显示帧号
            int frameLabelStep = 1;
            if (ppf < 4f) frameLabelStep = 0; // 太密就不显示帧号
            else if (ppf < 8f) frameLabelStep = 10;
            else if (ppf < 16f) frameLabelStep = 5;

            int halfSecFrames = Mathf.RoundToInt(FrameRate / 2f); // 15

            for (int f = startFrame; f <= endFrame; f++)
            {
                float x = TrackHeaderWidth + (f * ppf) - _scrollX;
                if (x < TrackHeaderWidth - 30 || x > contentRect.width + 30) continue;

                // 每半秒(15帧)显示时间标签
                if (f % halfSecFrames == 0)
                {
                    float t = f / FrameRate;
                    string label = t < 1f ? $"{t:F1}s" : $"{t:F1}s";
                    GUI.Label(new Rect(x - 24, 1, 48, 12), label, rulerStyle);
                }
            }

            // ---- 轨道名 ----
            var trackStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.75f, 0.75f, 0.75f) },
                fontSize = 11,
                padding = new RectOffset(6, 0, 0, 0)
            };
            for (int i = 0; i < _config.Tracks.Count; i++)
            {
                var track = _config.Tracks[i];
                float y = RulerHeight + i * TrackHeight;
                string name = string.IsNullOrEmpty(track.TrackName) ? track.GetType().Name : track.TrackName;
                GUI.Label(new Rect(0, y, TrackHeaderWidth, TrackHeight), name, trackStyle);
            }
        }

        // ============================================================
        // 渲染
        // ============================================================
        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (_config == null) return;
            var rect = contentRect;
            if (rect.width < 1f || rect.height < 1f) return;
            var painter = mgc.painter2D;
            float pps = PixelsPerSecond * _zoom;

            DrawRuler(painter, rect, pps);
            DrawTracks(painter, rect, pps);

            // 空状态提示
            if (_config.Tracks.Count == 0)
            {
                // 简单画一个提示背景区域（文字无法通过 Painter2D 画，用 Label 叠加）
            }
        }

        private void DrawRuler(Painter2D painter, Rect rect, float pps)
        {
            // 左上角标题背景
            painter.fillColor = new Color(0.2f, 0.2f, 0.2f);
            DrawRect(painter, 0, 0, TrackHeaderWidth, RulerHeight);

            // 刻度区背景
            painter.fillColor = new Color(0.16f, 0.16f, 0.16f);
            painter.BeginPath();
            painter.MoveTo(new Vector2(TrackHeaderWidth, 0));
            painter.LineTo(new Vector2(rect.width, 0));
            painter.LineTo(new Vector2(rect.width, RulerHeight));
            painter.LineTo(new Vector2(TrackHeaderWidth, RulerHeight));
            painter.ClosePath();
            painter.Fill();

            // 帧刻度
            float ppf = pps / FrameRate;
            float visibleEnd = GetVisibleEndTime(pps);
            int startFrame = Mathf.Max(0, Mathf.FloorToInt(_scrollX / ppf));
            int endFrame = Mathf.CeilToInt((rect.width - TrackHeaderWidth + _scrollX) / ppf) + 1;
            int maxFrame = Mathf.CeilToInt(visibleEnd * FrameRate);
            endFrame = Mathf.Min(endFrame, maxFrame);

            int halfSecFrames = Mathf.RoundToInt(FrameRate / 2f); // 15
            int fiveFrames = 5;

            for (int f = startFrame; f <= endFrame; f++)
            {
                float x = TrackHeaderWidth + (f * ppf) - _scrollX;
                if (x < TrackHeaderWidth || x > rect.width) continue;

                float tickHeight;
                if (f % halfSecFrames == 0)
                {
                    // 半秒大刻度
                    painter.strokeColor = new Color(0.55f, 0.55f, 0.55f);
                    painter.lineWidth = 1f;
                    tickHeight = RulerHeight;
                }
                else if (f % fiveFrames == 0)
                {
                    // 每5帧中刻度
                    painter.strokeColor = new Color(0.4f, 0.4f, 0.4f);
                    painter.lineWidth = 1f;
                    tickHeight = RulerHeight * 0.55f;
                }
                else
                {
                    // 每帧小刻度（太密时跳过）
                    if (ppf < 4f) continue;
                    painter.strokeColor = new Color(0.3f, 0.3f, 0.3f);
                    painter.lineWidth = 1f;
                    tickHeight = RulerHeight * 0.3f;
                }

                painter.BeginPath();
                painter.MoveTo(new Vector2(x, RulerHeight - tickHeight));
                painter.LineTo(new Vector2(x, RulerHeight));
                painter.Stroke();
            }

            // 刻度尺底部分隔线
            painter.strokeColor = new Color(0.1f, 0.1f, 0.1f);
            painter.lineWidth = 2.5f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0, RulerHeight));
            painter.LineTo(new Vector2(rect.width, RulerHeight));
            painter.Stroke();
        }

        private void DrawTracks(Painter2D painter, Rect rect, float pps)
        {
            if (_config.Tracks.Count == 0) return;

            for (int i = 0; i < _config.Tracks.Count; i++)
            {
                var track = _config.Tracks[i];
                float y = RulerHeight + i * TrackHeight;

                // 轨道头背景
                painter.fillColor = new Color(0.2f, 0.2f, 0.2f);
                DrawRect(painter, 0, y, TrackHeaderWidth, TrackHeight);

                // 轨道分隔线
                painter.strokeColor = new Color(0.12f, 0.12f, 0.12f);
                painter.lineWidth = 1f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(0, y + TrackHeight));
                painter.LineTo(new Vector2(rect.width, y + TrackHeight));
                painter.Stroke();

                // Clip 色块
                foreach (var clip in track.Clips)
                {
                    float cx = TrackHeaderWidth + (clip.StartTime * pps) - _scrollX;
                    float cw = Mathf.Max(clip.Duration * pps, MinClipWidth);
                    if (cx + cw < TrackHeaderWidth || cx > rect.width) continue;

                    var color = GetClipColor(clip.GetType());
                    bool selected = clip == _selectedClip;
                    painter.fillColor = selected ? color * 1.3f : color;
                    DrawRoundedRect(painter, cx, y + 2, cw, TrackHeight - 4, 3f);

                    if (selected)
                    {
                        painter.strokeColor = Color.white;
                        painter.lineWidth = 1.5f;
                        DrawRoundedRectStroke(painter, cx, y + 2, cw, TrackHeight - 4, 3f);
                    }
                }
            }
        }

        // ============================================================
        // 交互
        // ============================================================
        private void OnMouseDown(MouseDownEvent evt)
        {
            if (_config == null) return;
            float pps = PixelsPerSecond * _zoom;

            if (evt.button == 0)
            {
                var hit = HitTestClip(evt.localMousePosition, pps, out var track, out var dragMode);
                if (hit != null)
                {
                    _selectedClip = hit;
                    _selectedTrack = track;
                    _dragClip = hit;
                    _dragMode = dragMode;
                    _dragStartMouseX = evt.localMousePosition.x;
                    _dragStartTime = hit.StartTime;
                    _dragStartDuration = hit.Duration;
                    OnClipSelected?.Invoke(hit, track);
                }
                else
                {
                    _selectedClip = null;
                    // 检查是否点击了某个轨道区域（无 clip 命中）
                    int trackIdx = GetTrackIndexAtY(evt.localMousePosition.y);
                    if (trackIdx >= 0 && trackIdx < _config.Tracks.Count)
                    {
                        _selectedTrack = _config.Tracks[trackIdx];
                        OnTrackSelected?.Invoke(_selectedTrack);
                    }
                    else
                    {
                        _selectedTrack = null;
                        OnSelectionCleared?.Invoke();
                    }
                }
                MarkDirtyRepaint();
            }
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (_dragClip == null || _config == null) return;
            float pps = PixelsPerSecond * _zoom;
            float dx = evt.localMousePosition.x - _dragStartMouseX;
            float dt = dx / pps;

            switch (_dragMode)
            {
                case DragMode.Move:
                    _dragClip.StartTime = SnapToFrame(Mathf.Max(0, _dragStartTime + dt));
                    break;
                case DragMode.ResizeRight:
                    _dragClip.Duration = SnapToFrame(Mathf.Max(1f / FrameRate, _dragStartDuration + dt));
                    break;
                case DragMode.ResizeLeft:
                    float newStart = SnapToFrame(Mathf.Max(0, _dragStartTime + dt));
                    float endTime = _dragStartTime + _dragStartDuration;
                    _dragClip.StartTime = newStart;
                    _dragClip.Duration = Mathf.Max(1f / FrameRate, SnapToFrame(endTime - newStart));
                    break;
            }
            MarkDirtyRepaint();
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (_dragClip != null)
            {
                _dragClip = null;
                _dragMode = DragMode.None;
                if (_config != null) EditorUtility.SetDirty(_config);
            }
        }

        private void OnWheel(WheelEvent evt)
        {
            if (evt.ctrlKey)
            {
                _zoom = Mathf.Clamp(_zoom - evt.delta.y * 0.05f, 0.2f, 5f);
            }
            else
            {
                _scrollX = Mathf.Max(0, _scrollX + evt.delta.y * 20f);
            }
            MarkDirtyRepaint();
            evt.StopPropagation();
        }

        // ============================================================
        // 右键菜单
        // ============================================================
        private void OnContextMenu(ContextualMenuPopulateEvent evt)
        {
            if (_config == null) return;
            float pps = PixelsPerSecond * _zoom;
            var pos = evt.triggerEvent != null
                ? this.WorldToLocal(evt.triggerEvent.originalMousePosition)
                : evt.localMousePosition;

            int trackIndex = GetTrackIndexAtY(pos.y);

            if (trackIndex >= 0 && trackIndex < _config.Tracks.Count)
            {
                var track = _config.Tracks[trackIndex];
                var trackType = track.GetType();
                float time = (pos.x - TrackHeaderWidth + _scrollX) / pps;

                var clipTypes = GetClipTypesForTrack(trackType);
                foreach (var (clipType, displayName) in clipTypes)
                {
                    evt.menu.AppendAction($"添加 Clip/{displayName}", _ =>
                    {
                        var clip = (SkillClip)Activator.CreateInstance(clipType);
                        clip.StartTime = SnapToFrame(Mathf.Max(0, time));
                        clip.Duration = 0.5f;
                        track.Clips.Add(clip);
                        EditorUtility.SetDirty(_config);
                        MarkDirtyRepaint();
                    });
                }

                if (_selectedClip != null && track.Clips.Contains(_selectedClip))
                {
                    evt.menu.AppendSeparator("");
                    evt.menu.AppendAction("删除选中 Clip", _ =>
                    {
                        track.Clips.Remove(_selectedClip);
                        _selectedClip = null;
                        OnSelectionCleared?.Invoke();
                        EditorUtility.SetDirty(_config);
                        MarkDirtyRepaint();
                    });
                }

                evt.menu.AppendSeparator("");
                evt.menu.AppendAction("删除轨道", _ =>
                {
                    _config.Tracks.Remove(track);
                    _selectedClip = null;
                    _selectedTrack = null;
                    OnSelectionCleared?.Invoke();
                    EditorUtility.SetDirty(_config);
                    MarkDirtyRepaint();
                });
                evt.menu.AppendSeparator("");
            }

            // 始终显示添加轨道选项
            evt.menu.AppendAction("添加轨道/动画轨道", _ => AddTrack<AnimationTrack>("Animation"));
            evt.menu.AppendAction("添加轨道/逻辑轨道", _ => AddTrack<LogicTrack>("Logic"));
            evt.menu.AppendAction("添加轨道/特效轨道", _ => AddTrack<EffectTrack>("Effect"));
        }

        private void AddTrack<T>(string name) where T : SkillTrack, new()
        {
            if (_config == null) return;
            var track = new T { TrackName = name };
            _config.Tracks.Add(track);
            EditorUtility.SetDirty(_config);
            UpdateHintVisibility();
            MarkDirtyRepaint();
        }

        // ============================================================
        // Hit Test
        // ============================================================
        private SkillClip HitTestClip(Vector2 pos, float pps, out SkillTrack hitTrack, out DragMode mode)
        {
            hitTrack = null;
            mode = DragMode.None;
            int trackIndex = GetTrackIndexAtY(pos.y);
            if (trackIndex < 0 || trackIndex >= _config.Tracks.Count) return null;

            var track = _config.Tracks[trackIndex];
            float y = RulerHeight + trackIndex * TrackHeight;

            foreach (var clip in track.Clips)
            {
                float cx = TrackHeaderWidth + (clip.StartTime * pps) - _scrollX;
                float cw = Mathf.Max(clip.Duration * pps, MinClipWidth);

                if (pos.x >= cx && pos.x <= cx + cw && pos.y >= y && pos.y <= y + TrackHeight)
                {
                    hitTrack = track;
                    const float edgeThreshold = 6f;
                    if (pos.x - cx < edgeThreshold) mode = DragMode.ResizeLeft;
                    else if (cx + cw - pos.x < edgeThreshold) mode = DragMode.ResizeRight;
                    else mode = DragMode.Move;
                    return clip;
                }
            }
            return null;
        }

        private int GetTrackIndexAtY(float y)
        {
            if (y < RulerHeight) return -1;
            return (int)((y - RulerHeight) / TrackHeight);
        }

        // ============================================================
        // Clip→Track 映射 (反射扫描 [ClipForTrack])
        // ============================================================
        private static List<(Type, string)> GetClipTypesForTrack(Type trackType)
        {
            if (_trackClipMap == null)
            {
                _trackClipMap = new Dictionary<Type, List<(Type, string)>>();
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type[] types;
                    try { types = asm.GetTypes(); } catch { continue; }
                    foreach (var type in types)
                    {
                        if (type.IsAbstract || !type.IsSubclassOf(typeof(SkillClip))) continue;
                        var attrs = type.GetCustomAttributes<ClipForTrackAttribute>();
                        foreach (var attr in attrs)
                        {
                            if (!_trackClipMap.ContainsKey(attr.TrackType))
                                _trackClipMap[attr.TrackType] = new List<(Type, string)>();
                            _trackClipMap[attr.TrackType].Add((type, attr.DisplayName ?? type.Name));
                        }
                    }
                }
            }
            return _trackClipMap.TryGetValue(trackType, out var list) ? list : new List<(Type, string)>();
        }

        // ============================================================
        // 绘制工具
        // ============================================================

        /// <summary>
        /// 将时间吸附到最近的帧边界。
        /// </summary>
        private static float SnapToFrame(float time)
        {
            return Mathf.Round(time * FrameRate) / FrameRate;
        }

        /// <summary>
        /// 计算可见时间轴终点：取 config.Duration 和视口可见范围的较大值，再额外留余量。
        /// </summary>
        private float GetVisibleEndTime(float pps)
        {
            float viewportSeconds = (contentRect.width - TrackHeaderWidth + _scrollX) / pps;
            return Mathf.Max(_config.Duration, viewportSeconds) + 2f;
        }

        private static Color GetClipColor(Type clipType)
        {
            return ClipColors.TryGetValue(clipType, out var c) ? c : new Color(0.5f, 0.5f, 0.5f);
        }

        private static void DrawRect(Painter2D p, float x, float y, float w, float h)
        {
            p.BeginPath();
            p.MoveTo(new Vector2(x, y));
            p.LineTo(new Vector2(x + w, y));
            p.LineTo(new Vector2(x + w, y + h));
            p.LineTo(new Vector2(x, y + h));
            p.ClosePath();
            p.Fill();
        }

        private static void DrawRoundedRect(Painter2D p, float x, float y, float w, float h, float r)
        {
            p.BeginPath();
            p.MoveTo(new Vector2(x + r, y));
            p.LineTo(new Vector2(x + w - r, y));
            p.ArcTo(new Vector2(x + w, y), new Vector2(x + w, y + r), r);
            p.LineTo(new Vector2(x + w, y + h - r));
            p.ArcTo(new Vector2(x + w, y + h), new Vector2(x + w - r, y + h), r);
            p.LineTo(new Vector2(x + r, y + h));
            p.ArcTo(new Vector2(x, y + h), new Vector2(x, y + h - r), r);
            p.LineTo(new Vector2(x, y + r));
            p.ArcTo(new Vector2(x, y), new Vector2(x + r, y), r);
            p.ClosePath();
            p.Fill();
        }

        private static void DrawRoundedRectStroke(Painter2D p, float x, float y, float w, float h, float r)
        {
            p.BeginPath();
            p.MoveTo(new Vector2(x + r, y));
            p.LineTo(new Vector2(x + w - r, y));
            p.ArcTo(new Vector2(x + w, y), new Vector2(x + w, y + r), r);
            p.LineTo(new Vector2(x + w, y + h - r));
            p.ArcTo(new Vector2(x + w, y + h), new Vector2(x + w - r, y + h), r);
            p.LineTo(new Vector2(x + r, y + h));
            p.ArcTo(new Vector2(x, y + h), new Vector2(x, y + h - r), r);
            p.LineTo(new Vector2(x, y + r));
            p.ArcTo(new Vector2(x, y), new Vector2(x + r, y), r);
            p.ClosePath();
            p.Stroke();
        }
    }
}

