using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct FindNearestOptimizedParrallelJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> SeekersPos;
    [ReadOnly] public NativeArray<float3> TargetsPos;
    public NativeArray<float3> NearestPos;

    public void Execute(int index)
    {
        float3 seekerPos = SeekersPos[index];

        // Use Binary search to get closest X coordinate
        int startIndex = TargetsPos.BinarySearch(seekerPos, new XAxisComparer { });

        // When no precise match is found, BinarySearch returns the bitwise negation of the last-searched index.
        // So when startIndex is negative, we flip the bits back to getthe last-searched index, then must ensure the index is within bounds.
        if (startIndex < 0) startIndex = ~startIndex;
        // If startIndex is bigger or equal to the length of the array it means there is no bigger value than our search target so we can start at the last index.
        if (startIndex >= TargetsPos.Length) startIndex = TargetsPos.Length - 1;

        float3 nearestTargetPos = TargetsPos[startIndex];
        float distWithNearest = math.distance(seekerPos, nearestTargetPos);

        // Search upward to find a closer target
        for (int i = startIndex+1; i < TargetsPos.Length; i++)
        {
            float3 targetPos = TargetsPos[i];
            float xDist = seekerPos.x - targetPos.x;
            if (xDist < 0) xDist *= -1;  // If xDist is negative make it positive

            // If distance between seeker.x and target.x is longer than our nearest distance we can stop
            // we're only getting away from the nearest at this point (x distance is bigger than nearest distance and x distance will only get bigger) 
            if (xDist > distWithNearest) break;

            float distWithTarget = math.distance(seekerPos, targetPos);
            if (distWithTarget < distWithNearest)
            {
                distWithNearest = distWithTarget;
                nearestTargetPos = targetPos;
            }
        }

        // Search downward to find a closer target
        for (int i = startIndex-1; i >= 0; i--)
        {
            float3 targetPos = TargetsPos[i];
            float xDist = seekerPos.x - targetPos.x;
            if (xDist < 0) xDist *= -1;  // If xDist is negative make it positive

            // If distance between seeker.x and target.x is longer than our nearest distance we can stop
            // we're only getting away from the nearest at this point (x distance is bigger than nearest distance and x distance will only get bigger) 
            if (xDist > distWithNearest) break;

            float distWithTarget = math.distance(seekerPos, targetPos);
            if(distWithTarget < distWithNearest)
            {
                distWithNearest = distWithTarget;
                nearestTargetPos = targetPos;
            }
        }

        NearestPos[index] = nearestTargetPos;
    }
}
