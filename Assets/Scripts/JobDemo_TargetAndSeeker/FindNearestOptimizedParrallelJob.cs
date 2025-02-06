using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Jobs.TargetAndSeekerDemo
{
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
            // So when startIndex is negative, we flip the bits back to get the last-searched index, then must ensure the index is within bounds.
            if (startIndex < 0) startIndex = ~startIndex;
            // If startIndex is bigger or equal to the length of the array it means there is no bigger value than our search target so we can start at the last index.
            if (startIndex >= TargetsPos.Length) startIndex = TargetsPos.Length - 1;

            float3 nearestTargetPos = TargetsPos[startIndex];

            // Use squared distance instead of distance because it's cheaper (no need to compute a square root)
            // Performance gain for this job with 1000 seekers and 1000 targets: 0.10-0.17ms -> 0.10-0.15ms (it is pretty much similar for this job)
            float distWithNearestSq = math.distancesq(seekerPos, nearestTargetPos);

            // Search upward to find a closer target
            Search(seekerPos, startIndex + 1, TargetsPos.Length, +1, ref nearestTargetPos, ref distWithNearestSq);

            // Search downward to find a closer target
            Search(seekerPos, startIndex - 1, -1, -1, ref nearestTargetPos, ref distWithNearestSq);

            NearestPos[index] = nearestTargetPos;
        }

        // Create a search method that use square distance
        private void Search(float3 seekerPos, int startIndex, int endIndex, int step, ref float3 nearestTargetPos, ref float distWithNearestSq)
        {
            for (int i = startIndex; i != endIndex; i += step)
            {
                float3 targetPos = TargetsPos[i];
                float xDist = seekerPos.x - targetPos.x;

                // If distance between seeker.x and target.x is longer than our nearest distance we can stop
                // we're only getting away from the nearest at this point (x distance is bigger than nearest distance and x distance will only get bigger)
                // we need to square xDist since we use square distance (also we no longer need to ensure it's positive)
                if ((xDist * xDist) > distWithNearestSq) break;

                float distWithTargetSq = math.distancesq(seekerPos, targetPos);
                if (distWithTargetSq < distWithNearestSq)
                {
                    distWithNearestSq = distWithTargetSq;
                    nearestTargetPos = targetPos;
                }
            }
        }
    } 
}
