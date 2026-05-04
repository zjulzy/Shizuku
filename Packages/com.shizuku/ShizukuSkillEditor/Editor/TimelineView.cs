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
    /// 时间轴视图。渲染轨道头、时间刻度尺、Clip 色块，处理拖拽交互�?
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
        private const float ScrollbarHeight = 16f;
        private const float ScrollbarWidth = 16f;

        // ---- 数据 ----
        private ShizukuSkillConfig _config;
        private float _zoom = 1f;
        private float _scrollX;
        private float _scrollY;

        // ---- 选中状�?----
        private SkillClip _selectedClip;
        private SkillTrack _selectedTrack;
        public event Action<SkillClip, SkillTrack> OnClipSelected;
        public event Action<SkillTrack> OnTrackSelected;
        public event Action OnSelectionCleared;
        /// <summary>双击 Clip 时触发（用于跳转到子编辑器，例如 GraphClip → SkillGraph 窗口）。</summary>
        public event Action<SkillClip, SkillTrack> OnClipDoubleClicked;

        // ---- Clip 拖拽 ----
        private SkillClip _dragClip;
        private SkillTrack _dragTrack;
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

        // ---- Clip→Track 缓存 (编辑器用，反射扫�?[ClipForTrack]) ----
        private static Dictionary<Type, List<(Type clipType, string displayName)>> _trackClipMap;

        private Label _hintLabel;
        private IMGUIContainer _imguiOverlay;
        private IMGUIContainer _scrollbarContainer;
        private IMGUIContainer _vScrollbarContainer;

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

            // IMGUI overlay for text + scrollbar
            _imguiOverlay = new IMGUIContainer(DrawIMGUIOverlay)
            {
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

            // 底部水平滚动条
            _scrollbarContainer = new IMGUIContainer(DrawHScrollbar)
            {
                style =
                {
                    position = Position.Absolute,
                    left = TrackHeaderWidth,
                    right = ScrollbarWidth,
                    bottom = 0,
                    height = ScrollbarHeight
                }
            };
            Add(_scrollbarContainer);

            // 右侧垂直滚动条
            _vScrollbarContainer = new IMGUIContainer(DrawVScrollbar)
            {
                style =
                {
                    position = Position.Absolute,
                    right = 0,
                    top = RulerHeight,
                    bottom = ScrollbarHeight,
                    width = ScrollbarWidth
                }
            };
            Add(_vScrollbarContainer);
        }


        public void SetConfig(ShizukuSkillConfig config)
        {
            _config = config;
            _selectedClip = null;
            _selectedTrack = null;
            _scrollX = 0;
            _scrollY = 0;
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

        public new void MarkDirtyRepaint()
        {
            base.MarkDirtyRepaint();
        }

        // ============================================================
        // 滚动条 (IMGUI)
        // ============================================================
        private float GetTotalContentHeight()
        {
            if (_config == null) return 0;
            return _config.Tracks.Count * TrackHeight;
        }

        private float GetMaxScrollY()
        {
            float viewportHeight = contentRect.height - RulerHeight - ScrollbarHeight;
            return Mathf.Max(0, GetTotalContentHeight() - viewportHeight);
        }

        private void DrawHScrollbar()
        {
            if (_config == null) return;
            float pps = PixelsPerSecond * _zoom;
            float viewportWidth = contentRect.width - TrackHeaderWidth - ScrollbarWidth;
            if (viewportWidth <= 0) return;

            float timelineContentWidth = GetVisibleEndTime(pps) * pps;
            if (timelineContentWidth <= viewportWidth) return;

            var rect = new Rect(0, 0, _scrollbarContainer.contentRect.width, ScrollbarHeight);
            float newScrollX = GUI.HorizontalScrollbar(rect, _scrollX, viewportWidth, 0, timelineContentWidth);
            if (!Mathf.Approximately(newScrollX, _scrollX))
            {
                _scrollX = newScrollX;
                base.MarkDirtyRepaint();
            }
        }

        private void DrawVScrollbar()
        {
            if (_config == null) return;
            float viewportHeight = contentRect.height - RulerHeight - ScrollbarHeight;
            float totalHeight = GetTotalContentHeight();
            if (viewportHeight <= 0 || totalHeight <= viewportHeight) return;

            var rect = new Rect(0, 0, ScrollbarWidth, _vScrollbarContainer.contentRect.height);
            float newScrollY = GUI.VerticalScrollbar(rect, _scrollY, viewportHeight, 0, totalHeight);
            if (!Mathf.Approximately(newScrollY, _scrollY))
            {
                _scrollY = newScrollY;
                base.MarkDirtyRepaint();
            }
        }

        // ============================================================
        // IMGUI 文字叠加层（轨道�?+ 刻度数字�?
        // ============================================================
        private void DrawIMGUIOverlay()
        {
            if (_config == null) return;
            float pps = PixelsPerSecond * _zoom;

            // ---- 左上角标�?----
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                fontSize = 12
            };
            GUI.Label(new Rect(0, 0, TrackHeaderWidth, RulerHeight), "Tracks", titleStyle);

            // ---- 刻度数字 (�?.5秒显示时间标�? ----
            float visibleEnd = GetVisibleEndTime(pps);
            float ppf = pps / FrameRate; // pixels per frame
            int startFrame = Mathf.Max(0, Mathf.FloorToInt(_scrollX / ppf));
            int endFrame = Mathf.CeilToInt((contentRect.width - TrackHeaderWidth + _scrollX) / ppf) + 1;            int maxFrame = Mathf.CeilToInt(visibleEnd * FrameRate);
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

                // 每半�?15�?显示时间标签
                if (f % halfSecFrames == 0)
                {
                    float t = f / FrameRate;
                    string label = t < 1f ? $"{t:F1}s" : $"{t:F1}s";
                    GUI.Label(new Rect(x - 24, 1, 48, 12), label, rulerStyle);
                }
            }

            // ---- 轨道�?----
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
                float y = RulerHeight + i * TrackHeight - _scrollY;
                if (y + TrackHeight < RulerHeight || y > contentRect.height - ScrollbarHeight) continue;
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

            // 空状态提�?
            if (_config.Tracks.Count == 0)
            {
                // 简单画一个提示背景区域（文字无法通过 Painter2D 画，�?Label 叠加�?
            }
        }

        private void DrawRuler(Painter2D painter, Rect rect, float pps)
        {
            // 左上角标题背�?
            painter.fillColor = new Color(0.2f, 0.2f, 0.2f);
            DrawRect(painter, 0, 0, TrackHeaderWidth, RulerHeight);

            // 刻度区背�?
            painter.fillColor = new Color(0.16f, 0.16f, 0.16f);
            painter.BeginPath();
            painter.MoveTo(new Vector2(TrackHeaderWidth, 0));
            painter.LineTo(new Vector2(rect.width, 0));
            painter.LineTo(new Vector2(rect.width, RulerHeight));
            painter.LineTo(new Vector2(TrackHeaderWidth, RulerHeight));
            painter.ClosePath();
            painter.Fill();

            // 帧刻�?
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
                    // 半秒大刻�?
                    painter.strokeColor = new Color(0.55f, 0.55f, 0.55f);
                    painter.lineWidth = 1f;
                    tickHeight = RulerHeight;
                }
                else if (f % fiveFrames == 0)
                {
                    // �?帧中刻度
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
                float y = RulerHeight + i * TrackHeight - _scrollY;

                // 跳过不可见的轨道
                if (y + TrackHeight < RulerHeight || y > rect.height - ScrollbarHeight) continue;

                // 轨道头背�?
                painter.fillColor = new Color(0.2f, 0.2f, 0.2f);
                DrawRect(painter, 0, y, TrackHeaderWidth, TrackHeight);

                // 轨道分隔�?
                painter.strokeColor = new Color(0.12f, 0.12f, 0.12f);
                painter.lineWidth = 1f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(0, y + TrackHeight));
                painter.LineTo(new Vector2(rect.width, y + TrackHeight));
                painter.Stroke();

                // Clip 色块 — 半透明 + 边框 + 重叠区域高亮
                var clips = track.Clips;
                for (int ci = 0; ci < clips.Count; ci++)
                {
                    var clip = clips[ci];
                    float cx = TrackHeaderWidth + (clip.StartTime * pps) - _scrollX;
                    float cw = Mathf.Max(clip.Duration * pps, MinClipWidth);
                    if (cx + cw < TrackHeaderWidth || cx > rect.width - ScrollbarWidth) continue;

                    float drawX = Mathf.Max(cx, TrackHeaderWidth);
                    float drawW = cw - (drawX - cx);
                    drawW = Mathf.Min(drawW, rect.width - ScrollbarWidth - drawX);
                    if (drawW <= 0) continue;

                    var baseColor = GetClipColor(clip.GetType());
                    bool selected = clip == _selectedClip;

                    // 半透明填充，便于看到重叠
                    var fill = selected ? baseColor * 1.3f : baseColor;
                    fill.a = 0.7f;
                    painter.fillColor = fill;
                    DrawRoundedRect(painter, drawX, y + 2, drawW, TrackHeight - 4, 3f);

                    // 始终绘制边框以区分相邻/重叠片段
                    painter.strokeColor = selected
                        ? Color.white
                        : new Color(baseColor.r * 0.5f, baseColor.g * 0.5f, baseColor.b * 0.5f, 1f);
                    painter.lineWidth = selected ? 1.5f : 1f;
                    DrawRoundedRectStroke(painter, drawX, y + 2, drawW, TrackHeight - 4, 3f);
                }

                // 重叠区域警示条纹
                for (int a = 0; a < clips.Count; a++)
                {
                    for (int b = a + 1; b < clips.Count; b++)
                    {
                        float aStart = clips[a].StartTime;
                        float aEnd   = aStart + clips[a].Duration;
                        float bStart = clips[b].StartTime;
                        float bEnd   = bStart + clips[b].Duration;
                        float oStart = Mathf.Max(aStart, bStart);
                        float oEnd   = Mathf.Min(aEnd, bEnd);
                        if (oEnd <= oStart) continue;

                        float ox  = TrackHeaderWidth + oStart * pps - _scrollX;
                        float ow  = (oEnd - oStart) * pps;
                        float odx = Mathf.Max(ox, TrackHeaderWidth);
                        float odw = ow - (odx - ox);
                        odw = Mathf.Min(odw, rect.width - ScrollbarWidth - odx);
                        if (odw <= 0) continue;

                        // 半透明黄色底色
                        painter.fillColor = new Color(1f, 1f, 0f, 0.18f);
                        DrawRect(painter, odx, y + 2, odw, TrackHeight - 4);

                        // 斜线条纹
                        painter.strokeColor = new Color(1f, 0.8f, 0f, 0.35f);
                        painter.lineWidth = 1f;
                        float step = 6f;
                        float h = TrackHeight - 4;
                        for (float sx = odx - h; sx < odx + odw; sx += step)
                        {
                            float x0 = Mathf.Max(sx, odx);
                            float x1 = Mathf.Min(sx + h, odx + odw);
                            if (x1 <= x0) continue;
                            painter.BeginPath();
                            painter.MoveTo(new Vector2(x0, y + 2 + (x0 - sx)));
                            painter.LineTo(new Vector2(x1, y + 2 + (x1 - sx)));
                            painter.Stroke();
                        }
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
                    _dragTrack = track;
                    _dragMode = dragMode;
                    _dragStartMouseX = evt.localMousePosition.x;
                    _dragStartTime = hit.StartTime;
                    _dragStartDuration = hit.Duration;
                    OnClipSelected?.Invoke(hit, track);

                    // 双击：派发到外部跳转处理（如 GraphClip → SkillGraph 窗口）
                    if (evt.clickCount >= 2)
                        OnClipDoubleClicked?.Invoke(hit, track);
                }
                else
                {
                    _selectedClip = null;
                    // 检查是否点击了某个轨道区域（无 clip 命中�?
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
                    float maxStart = _config.Duration - _dragClip.Duration;
                    float newMoveStart = SnapToFrame(Mathf.Clamp(_dragStartTime + dt, 0, Mathf.Max(0, maxStart)));
                    if (!WouldViolateOverlap(_dragTrack, _dragClip, newMoveStart, _dragClip.Duration))
                        _dragClip.StartTime = newMoveStart;
                    break;
                case DragMode.ResizeRight:
                    float maxDur = _config.Duration - _dragClip.StartTime;
                    float newDur = SnapToFrame(Mathf.Clamp(_dragStartDuration + dt, 1f / FrameRate, maxDur));
                    if (!WouldViolateOverlap(_dragTrack, _dragClip, _dragClip.StartTime, newDur))
                        _dragClip.Duration = newDur;
                    break;
                case DragMode.ResizeLeft:
                    float newStart = SnapToFrame(Mathf.Max(0, _dragStartTime + dt));
                    float endTime = _dragStartTime + _dragStartDuration;
                    float newLeftDur = Mathf.Max(1f / FrameRate, SnapToFrame(endTime - newStart));
                    if (!WouldViolateOverlap(_dragTrack, _dragClip, newStart, newLeftDur))
                    {
                        _dragClip.StartTime = newStart;
                        _dragClip.Duration = newLeftDur;
                    }
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
                // Ctrl+滚轮：缩放
                _zoom = Mathf.Clamp(_zoom - evt.delta.y * 0.05f, 0.2f, 5f);
            }
            else if (evt.shiftKey)
            {
                // Shift+滚轮：水平滚动
                float pps = PixelsPerSecond * _zoom;
                float viewportWidth = contentRect.width - TrackHeaderWidth - ScrollbarWidth;
                float totalWidth = GetVisibleEndTime(pps) * pps;
                float maxScroll = Mathf.Max(0, totalWidth - viewportWidth);
                _scrollX = Mathf.Clamp(_scrollX + evt.delta.y * 20f, 0, maxScroll);
            }
            else
            {
                // 普通滚轮：垂直滚动
                _scrollY = Mathf.Clamp(_scrollY + evt.delta.y * 20f, 0, GetMaxScrollY());
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
                        clip.StartTime = SnapToFrame(Mathf.Clamp(time, 0, _config.Duration - 0.5f));
                        clip.Duration = Mathf.Min(0.5f, _config.Duration - clip.StartTime);
                        if (WouldViolateOverlap(track, clip, clip.StartTime, clip.Duration))
                        {
                            Debug.LogWarning("[SkillEditor] 无法在此处添加 Clip：会超出轨道允许的最大重叠数。");
                            return;
                        }
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
            float y = RulerHeight + trackIndex * TrackHeight - _scrollY;

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
            return (int)((y - RulerHeight + _scrollY) / TrackHeight);
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
        // 重叠验证
        // ============================================================
        private static readonly Dictionary<Type, int> _overlapCache = new();

        /// <summary>
        /// 获取轨道允许的最大重叠数。未标记 AllowClipOverlap 时返回 1（不允许重叠）。
        /// </summary>
        private static int GetMaxOverlap(Type trackType)
        {
            if (!_overlapCache.TryGetValue(trackType, out int max))
            {
                var attr = trackType.GetCustomAttribute<AllowClipOverlapAttribute>();
                max = attr?.MaxOverlap ?? 1;
                _overlapCache[trackType] = max;
            }
            return max;
        }

        /// <summary>
        /// 检查在轨道上放置/移动一个 clip 后是否会违反重叠规则。
        /// exclude 为当前正在操作的 clip（已在列表中时需排除后重新计算）。
        /// </summary>
        private static bool WouldViolateOverlap(SkillTrack track, SkillClip target, float startTime, float duration, SkillClip exclude = null)
        {
            int maxOverlap = GetMaxOverlap(track.GetType());
            float targetEnd = startTime + duration;

            // 收集所有其他 clip 的区间
            var others = new List<(float s, float e)>();
            foreach (var c in track.Clips)
            {
                if (c == exclude || c == target) continue;
                others.Add((c.StartTime, c.StartTime + c.Duration));
            }

            // 对目标区间内的每个采样点，检查与目标重叠的 clip 数 + 1(目标自身) 是否超过 maxOverlap
            // 优化：只需检查每个 clip 的边界点处的重叠数
            var checkPoints = new List<float> { startTime, targetEnd };
            foreach (var (s, e) in others)
            {
                if (s > startTime && s < targetEnd) checkPoints.Add(s);
                if (e > startTime && e < targetEnd) checkPoints.Add(e);
            }

            foreach (float t in checkPoints)
            {
                // 在 t 时刻（取开区间内的微小偏移），统计重叠数
                float probe = Mathf.Min(t + 0.0001f, targetEnd - 0.0001f);
                if (probe <= startTime || probe >= targetEnd) continue;

                int count = 1; // 目标自身
                foreach (var (s, e) in others)
                {
                    if (probe > s && probe < e) count++;
                }
                if (count > maxOverlap) return true;
            }
            return false;
        }

        // ============================================================
        // 绘制工具
        // ============================================================

        /// <summary>
        /// 将时间吸附到最近的帧边界�?
        /// </summary>
        private static float SnapToFrame(float time)
        {
            return Mathf.Round(time * FrameRate) / FrameRate;
        }

        /// <summary>
        /// 计算可见时间轴终点：�?config.Duration 和视口可见范围的较大值，再额外留余量�?
        /// </summary>
        private float GetVisibleEndTime(float pps)
        {
            return _config.Duration;
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

