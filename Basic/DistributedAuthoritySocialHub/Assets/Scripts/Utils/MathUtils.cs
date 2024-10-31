using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Unity.Multiplayer.Samples.SocialHub.Utils
{
    static class MathUtils
    {
        const float k_DefaultThreshold = 0.0025f;

        /// <summary>
        ///
        /// </summary>
        /// <param name="vector3"></param>
        /// <param name="highPrecisionDecimals"> Enable this to get 6 decimal precision when logging Vector3 values </param>
        /// <returns></returns>
        internal static string GetVector3Values(ref Vector3 vector3, bool highPrecisionDecimals = false)
        {
            if (highPrecisionDecimals)
            {
                return $"({vector3.x:F6},{vector3.y:F6},{vector3.z:F6})";
            }
            else
            {
                return $"({vector3.x:F2},{vector3.y:F2},{vector3.z:F2})";
            }
        }

        internal static string GetVector3Values(Vector3 vector3, bool highPrecisionDecimals = false)
        {
            return GetVector3Values(ref vector3, highPrecisionDecimals);
        }

        internal static Vector3 GetRandomVector3(float min, float max, Vector3 baseLine, bool randomlyApplySign = false)
        {
            var retValue = new Vector3(baseLine.x * Random.Range(min, max), baseLine.y * Random.Range(min, max), baseLine.z * Random.Range(min, max));
            if (!randomlyApplySign)
            {
                return retValue;
            }

            retValue.x *= Random.Range(1, 100) >= 50 ? -1 : 1;
            retValue.y *= Random.Range(1, 100) >= 50 ? -1 : 1;
            retValue.z *= Random.Range(1, 100) >= 50 ? -1 : 1;
            return retValue;
        }

        internal static Vector3 GetRandomVector3(MinMaxVector2Physics minMax, Vector3 baseLine, bool randomlyApplySign = false)
        {
            return GetRandomVector3(minMax.Min, minMax.Max, baseLine, randomlyApplySign);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool Approximately(float a, float b, float threshold = k_DefaultThreshold)
        {
            return Math.Round(Mathf.Abs(a - b), 4) <= threshold;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool Approximately(Vector3 a, Vector3 b, float threshold = k_DefaultThreshold)
        {
            return Approximately(a.x, b.x) && Approximately(a.y, b.y) && Approximately(a.z, b.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool Approximately(Quaternion a, Quaternion b, float threshold = k_DefaultThreshold)
        {
            return Approximately(a.x, b.x) && Approximately(a.y, b.y) && Approximately(a.z, b.z) && Approximately(a.w, b.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float EulerDelta(float a, float b)
        {
            return Mathf.DeltaAngle(a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ApproximatelyEuler(float a, float b, float threshold = k_DefaultThreshold)
        {
            return Mathf.Abs(EulerDelta(a, b)) <= threshold;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ApproximatelyEuler(Vector3 a, Vector3 b, float threshold = k_DefaultThreshold)
        {
            return ApproximatelyEuler(a.x, b.x, threshold) && ApproximatelyEuler(a.y, b.y, threshold) && ApproximatelyEuler(a.z, b.z, threshold);
        }

        [Serializable]
        internal class MinMaxVector2Physics
        {
            [Range(1.0f, 200.0f)]
            public float Min;
            [Range(1.0f, 200.0f)]
            public float Max;

            MinMaxVector2Physics(float min, float max)
            {
                Min = min;
                Max = max;
            }
        }

    }
}
