using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

// In burst compiled-code, generic job such as SortJob must be registered with [assembly: RegisterGenericJobType(typeof(MyJob<MyJobSpecialization>))]
// [assembly: RegisterGenericJobType()] must always be put after the using and before the namespace or class declaration
// In the case of sort there is 2 jobs two declare because sort job is decomposed in 2 jobs (SegmentSort and then SegmentSortMerge)
// ! Sometime weird stuff happened like the demo suddenly working without these or another error popping in a system with no generic job for no reason but now with the 2 sortjobs declared it seems fine
[assembly: RegisterGenericJobType(typeof(SortJob<float3, ECS.TargetAndSeekerDemo.XAxisComparer>.SegmentSort))]
[assembly: RegisterGenericJobType(typeof(SortJob<float3, ECS.TargetAndSeekerDemo.XAxisComparer>.SegmentSortMerge))]
namespace ECS.TargetAndSeekerDemo
{
    [UpdateAfter(typeof(MovementSystem))]
    [UpdateAfter(typeof(SpawnerSystem))]
    public partial struct SeekerSystem : ISystem
    {
        EntityQuery seekersQuery;
        EntityQuery targetsQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            seekersQuery = SystemAPI.QueryBuilder().WithAllRW<Seeker>().Build();
            targetsQuery = SystemAPI.QueryBuilder().WithAll<Target, LocalTransform>().Build();
            state.RequireForUpdate(seekersQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // TODO: find a way to keep the a sorted version of the entity arrays based on 
            NativeArray<Entity> targets = targetsQuery.ToEntityArray(Allocator.Temp);

            NativeArray<float3> targetsPos = new NativeArray<float3>(targets.Length, Allocator.TempJob); // We use [DeallocateOnJobCompletion] in the job to deallocate this
            for (int i = 0; i < targetsPos.Length; i++)
            {
                targetsPos[i] = SystemAPI.GetComponent<LocalTransform>(targets[i]).Position;
            }
            targets.Dispose(); // no longer needed here if I keep only hte position in Seeker data

            SortJob<float3, XAxisComparer> sortJob = targetsPos.SortJob(new XAxisComparer());
            JobHandle sortJobHandle = sortJob.Schedule(state.Dependency);

            FindNearestTarget findNearestTarget = new FindNearestTarget()
            {
                TargetsPos = targetsPos,
            };

            state.Dependency = findNearestTarget.ScheduleParallel(sortJobHandle);
        }
    }

    [BurstCompile]
    public partial struct FindNearestTarget : IJobEntity
    {
        // Note: There was no issue with [DeallocateOnJobCompletion], the issue was that in burst-compiled code generic job such as SortJob must be registered with [assembly: RegisterGenericJobType(typeof(SortJob<float3, XAxisComparer>))]
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<float3> TargetsPos;

        public void Execute(ref Seeker seeker, in LocalTransform transform)
        {
            float3 seekerPos = transform.Position;

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

            seeker.NearestTargetPos = nearestTargetPos;
        }

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

    public struct XAxisComparer : IComparer<float3>
    {
        public int Compare(float3 a, float3 b)
        {
            return a.x.CompareTo(b.x);
        }
    }
}