#if UNITY_EDITOR
using System.Collections.Generic;

/// <summary>
/// ShizukuBluePrint 的调试功能部分
/// 负责：重写快照拍摄（额外捕获 Behavior 字段）、重写快照还原
/// </summary>
public abstract partial class ShizukuBluePrint<T>
{
    /// <summary>
    /// 重写快照：额外捕获 Behavior 上所有 public/protected 字段的当前值
    /// </summary>
    public override DebugSnapshot CaptureSnapshot(string pausedAtNodeGuid)
    {
        var snapshot = base.CaptureSnapshot(pausedAtNodeGuid);
        
        if (_behavior != null && _cachedGetters != null)
        {
            var props = new Dictionary<string, object>();
            foreach (var kvp in _cachedGetters)
            {
                try
                {
                    props[kvp.Key] = kvp.Value(_behavior);
                }
                catch
                {
                    props[kvp.Key] = "<error>";
                }
            }
            snapshot.BehaviorFields = props;
        }
        
        return snapshot;
    }

    /// <summary>
    /// 重写还原：额外把快照中的 Behavior 字段写回。
    /// 只涉及 public/protected 字段，无属性 setter 的副作用风险。
    /// </summary>
    public override void RestoreVariablesFromSnapshot()
    {
        base.RestoreVariablesFromSnapshot();
        
        var behaviorFields = ShizukuDebugger.SnapshotBehaviorFields;
        if (behaviorFields == null || _behavior == null || _cachedSetters == null)
            return;
        
        foreach (var kvp in behaviorFields)
        {
            if (_cachedSetters.TryGetValue(kvp.Key, out var setter))
            {
                try
                {
                    setter(_behavior, kvp.Value);
                }
                catch
                {
                    // readonly 字段或类型不匹配，静默跳过
                }
            }
        }
    }
}
#endif

