using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

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
            float3 nearestPos = math.INFINITY;

            foreach (float3 targetPos in TargetsPos)
            {
                float distWithTarget = math.distance(SeekersPos[i], targetPos);
                float distWithNearest = math.distance(SeekersPos[i], nearestPos);

                if(distWithTarget < distWithNearest)
                {
                    nearestPos = targetPos;
                }
            }
            NearestPos[i] = nearestPos;
        }
    }
}
