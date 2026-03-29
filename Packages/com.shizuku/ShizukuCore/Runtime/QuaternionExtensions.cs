using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// quaternion 扩展方法
/// </summary>
namespace Shizuku.Core
{
    public static class QuaternionExtensions
    {
        /// <summary> 转 Unity Quaternion </summary>
        public static Quaternion ToQuaternion(this quaternion q) => new Quaternion(q.value.x, q.value.y, q.value.z, q.value.w);

        /// <summary> 从 Unity Quaternion 转 mathematics quaternion </summary>
        public static quaternion ToMathQuaternion(this Quaternion q) => new quaternion(q.x, q.y, q.z, q.w);

        /// <summary> 获取前方向 (local Z+) </summary>
        public static float3 Forward(this quaternion q) => math.forward(q);

        /// <summary> 获取右方向 (local X+) </summary>
        public static float3 Right(this quaternion q) => math.mul(q, math.right());

        /// <summary> 获取上方向 (local Y+) </summary>
        public static float3 Up(this quaternion q) => math.mul(q, math.up());

        /// <summary> 绕 Y 轴旋转指定角度（弧度） </summary>
        public static quaternion RotateY(this quaternion q, float radians)
        {
            return math.mul(q, quaternion.RotateY(radians));
        }

        /// <summary> 绕任意轴旋转指定角度（弧度） </summary>
        public static quaternion RotateAround(this quaternion q, float3 axis, float radians)
        {
            return math.mul(q, quaternion.AxisAngle(axis, radians));
        }

        /// <summary> 球面插值 </summary>
        public static quaternion SlerpTo(this quaternion from, quaternion to, float t) => math.slerp(from, to, t);

        /// <summary> 求从 from 到 to 方向的旋转（XZ 平面，忽略 Y） </summary>
        public static quaternion LookRotationFlat(this quaternion _, float3 from, float3 to)
        {
            var dir = (to - from).Flat();
            if (math.lengthsq(dir) < 1e-8f)
                return quaternion.identity;
            return quaternion.LookRotationSafe(dir, math.up());
        }

        /// <summary> 获取欧拉角（度） </summary>
        public static float3 ToEulerDegrees(this quaternion q)
        {
            return math.degrees(ToEulerRadians(q));
        }

        /// <summary> 获取欧拉角（弧度） </summary>
        public static float3 ToEulerRadians(this quaternion q)
        {
            // ZXY 顺序，与 Unity 默认一致
            var m = new float3x3(q);
            float x = math.asin(math.clamp(-m.c2.y, -1f, 1f));

            float y, z;
            if (math.abs(m.c2.y) < 0.9999f)
            {
                y = math.atan2(m.c2.x, m.c2.z);
                z = math.atan2(m.c0.y, m.c1.y);
            }
            else
            {
                y = math.atan2(-m.c0.z, m.c0.x);
                z = 0f;
            }

            return new float3(x, y, z);
        }
    }


}
