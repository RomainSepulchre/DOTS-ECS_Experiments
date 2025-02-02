using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct FindNearestParrallelJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> SeekersPos;
    [ReadOnly] public NativeArray<float3> TargetsPos;
    public NativeArray<float3> NearestPos;

    public void Execute(int index)
    {
        float distWithNearestSq = float.MaxValue;

        foreach (float3 targetPos in TargetsPos)
        {
            // Use squared distance instead of distance because it's cheaper (no need to compute a square root)
            // Performance gain for this job with 1000 seekers and 1000 targets: 0.33-0.42ms -> 0.28-0.33ms
            float distWithTargetSq = math.distancesq(SeekersPos[index], targetPos);

            if (distWithTargetSq < distWithNearestSq)
            {
                NearestPos[index] = targetPos;
                distWithNearestSq = distWithTargetSq;
            }
        }
    }
}
