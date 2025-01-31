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
        float3 nearestPos = math.INFINITY;

        foreach (float3 targetPos in TargetsPos)
        {
            float distWithTarget = math.distance(SeekersPos[index], targetPos);
            float distWithNearest = math.distance(SeekersPos[index], nearestPos);

            if (distWithTarget < distWithNearest)
            {
                nearestPos = targetPos;
            }
        }
        NearestPos[index] = nearestPos;
    }
}
