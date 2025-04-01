using Unity.Burst;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;


namespace Burst.Experiments
{
    [BurstCompile]
    public static class BurstCompiledUtilities
    {
        public static bool IsBurstedV2 => Unity.Burst.CompilerServices.Constant.IsConstantExpression(1);

        [BurstDiscard]
        static void SetFalseIfUnBursted(ref bool val)
        {
            val = false;
        }

        public static bool IsBursted()
        {
            bool ret = true;
            SetFalseIfUnBursted(ref ret);
            return ret;
        }

        // TODO: Compare speed between burst-compiled and non-burstcompiled
        // => Pretty similar with simple operation, try a bigger operation (ex: loop with high number of iteration)

        // Code is burst compiled when checking IsBursted and Burst Inspector, however method has not the green burst compiled color when profiling
        [BurstCompile]
        public static void BurstCompiled_MultiplyAdd(in float3 mula, in float3 mulb, in float3 add, out float3 result)
        {
            ProfilerMarker multAddMarker = new ProfilerMarker("Marker_BurstCompiled_MultiplyAdd");
            multAddMarker.Begin();
            result = mula * mulb + add;
            multAddMarker.End();
            //Debug.Log($"BurstCompiled_MultiplyAdd IsBursted: {IsBursted()} (v2={IsBurstedV2})");
        }

        public static void NonBurstCompiled_MultiplyAdd(in float3 mula, in float3 mulb, in float3 add, out float3 result)
        {
            ProfilerMarker multAddMarker = new ProfilerMarker("Marker_NonBurstCompiled_MultiplyAdd");
            multAddMarker.Begin();
            result = mula * mulb + add;
            multAddMarker.End();
            //Debug.Log($"BurstCompiled_MultiplyAdd IsBursted: {IsBursted()} (v2={IsBurstedV2})");
        }

        [BurstCompile]
        public static void BurstCompiled_MultiplySub(in float3 mula, in float3 mulb, in float3 sub, out float3 result)
        {
            ProfilerMarker multSubMarker = new ProfilerMarker("Marker_BurstCompiled_MultiplySub");
            multSubMarker.Begin();
            result = mula * mulb - sub;
            multSubMarker.End();
            //Debug.Log($"BurstCompiled_MultiplySub IsBursted: {IsBursted()} (v2={IsBurstedV2})");
        }

        // Test a non burst-compiled method
        public static void NonBurstCompiled_MultiplySub(in float3 mula, in float3 mulb, in float3 sub, out float3 result)
        {
            ProfilerMarker multSubMarker = new ProfilerMarker("Marker_NonBurstCompiled_MultiplySub");
            multSubMarker.Begin();
            result = mula * mulb - sub;
            multSubMarker.End();
            //Debug.Log($"BurstCompiled_MultiplySub IsBursted: {IsBursted()} (v2={IsBurstedV2})");
        }
    } 
}

