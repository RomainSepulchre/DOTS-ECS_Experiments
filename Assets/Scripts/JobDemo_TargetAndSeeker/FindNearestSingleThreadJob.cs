using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Jobs.TargetAndSeekerDemo
{
    [BurstCompile]
    public struct FindNearestSingleThreadJob : IJob
    {
        [ReadOnly] public NativeArray<float3> SeekersPos;
        [ReadOnly] public NativeArray<float3> TargetsPos;
        public NativeArray<float3> NearestPos;

        public void Execute()
        {
            for (int i = 0; i < SeekersPos.Length; i++)
            {
                float distWithNearestSq = float.MaxValue;

                foreach (float3 targetPos in TargetsPos)
                {
                    // Use squared distance instead of distance because it's cheaper (no need to compute a square root)
                    // Performance gain for this job with 1000 seekers and 1000 targets: 1.89-2ms -> 1.22-1.30ms
                    float distWithTargetSq = math.distancesq(SeekersPos[i], targetPos);

                    if (distWithTargetSq < distWithNearestSq)
                    {
                        NearestPos[i] = targetPos;
                        distWithNearestSq = distWithTargetSq;
                    }
                }
            }
        }
    } 
}
