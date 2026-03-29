using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// float3 扩展方法
/// </summary>
namespace Shizuku.Core
{
    public static class Float3Extensions
    {
        /// <summary> 保留 XZ 平面，Y 置零 </summary>
        public static float3 Flat(this float3 v) => new float3(v.x, 0f, v.z);

        /// <summary> XZ 平面距离 </summary>
        public static float FlatDistance(this float3 a, float3 b)
        {
            var d = a.Flat() - b.Flat();
            return math.length(d);
        }

        /// <summary> XZ 平面方向（归一化） </summary>
        public static float3 FlatDirection(this float3 from, float3 to)
        {
            var d = (to - from).Flat();
            return math.normalizesafe(d);
        }

        /// <summary> 转 Vector3 </summary>
        public static Vector3 ToVector3(this float3 v) => new Vector3(v.x, v.y, v.z);

        /// <summary> 从 Vector3 转 float3 </summary>
        public static float3 ToFloat3(this Vector3 v) => new float3(v.x, v.y, v.z);

        /// <summary> 各分量取绝对值 </summary>
        public static float3 Abs(this float3 v) => math.abs(v);

        /// <summary> 各分量 Clamp </summary>
        public static float3 Clamp(this float3 v, float min, float max) => math.clamp(v, min, max);

        /// <summary> 带最大长度的截断 </summary>
        public static float3 ClampMagnitude(this float3 v, float maxLength)
        {
            var len = math.length(v);
            return len > maxLength ? v * (maxLength / len) : v;
        }

        /// <summary> 是否近似为零 </summary>
        public static bool IsNearlyZero(this float3 v, float epsilon = 1e-5f) => math.lengthsq(v) < epsilon * epsilon;

        /// <summary> 各分量线性插值 </summary>
        public static float3 LerpTo(this float3 from, float3 to, float t) => math.lerp(from, to, t);
    }


}
