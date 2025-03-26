using ECS.TargetAndSeekerDemo;
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// In burst compiled-code, generic job such as SortJob must be registered with [assembly: RegisterGenericJobType(typeof(MyJob<MyJobSpecialization>))]
// [assembly: RegisterGenericJobType()] must always be put after the using and before the namespace or class declaration
// In the case of sort there is 2 jobs two declare because sort job is decomposed in 2 jobs (SegmentSort and then SegmentSortMerge)
// ! Sometime weird stuff happened like the demo suddenly working without these or another error popping in a system with no generic job for no reason but now with the 2 sortjobs declared it seems fine
[assembly: RegisterGenericJobType(typeof(SortJob<EntityWithPosition, EntityXPositionComparer>.SegmentSort))]
[assembly: RegisterGenericJobType(typeof(SortJob<EntityWithPosition, EntityXPositionComparer>.SegmentSortMerge))]
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
            NativeArray<Entity> targets = targetsQuery.ToEntityArray(Allocator.Temp);

            NativeArray<EntityWithPosition> targetsWithPos = new NativeArray<EntityWithPosition>(targets.Length, Allocator.TempJob); // We use [DeallocateOnJobCompletion] in the job to deallocate this
            for (int i = 0; i < targetsWithPos.Length; i++)
            {
                targetsWithPos[i] = new EntityWithPosition()
                {
                    Entity = targets[i],
                    Position = SystemAPI.GetComponent<LocalTransform>(targets[i]).Position
                };
            }
            targets.Dispose(); // no longer needed here if I keep only the position in Seeker data

            SortJob<EntityWithPosition, EntityXPositionComparer> sortJob = targetsWithPos.SortJob(new EntityXPositionComparer());
            JobHandle sortJobHandle = sortJob.Schedule(state.Dependency);

            FindNearestTarget findNearestTarget = new FindNearestTarget()
            {
                TargetsWithPos = targetsWithPos,
            };

            state.Dependency = findNearestTarget.ScheduleParallel(sortJobHandle);
        }
    }

    [BurstCompile]
    public partial struct FindNearestTarget : IJobEntity
    {
        // Note: There was no issue with [DeallocateOnJobCompletion], the issue was that in burst-compiled code generic job such as SortJob must be registered with [assembly: RegisterGenericJobType(typeof(SortJob<float3, XAxisComparer>))]
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<EntityWithPosition> TargetsWithPos;

        public void Execute(ref Seeker seeker, in LocalTransform transform, Entity entity)
        {
            float3 seekerPos = transform.Position;
            EntityWithPosition seekerWithPos = new EntityWithPosition()
            {
                Entity = entity,
                Position = seekerPos
            };

            // Use Binary search to get closest X coordinate
            int startIndex = TargetsWithPos.BinarySearch(seekerWithPos, new EntityXPositionComparer { });

            // When no precise match is found, BinarySearch returns the bitwise negation of the last-searched index.
            // So when startIndex is negative, we flip the bits back to get the last-searched index, then must ensure the index is within bounds.
            if (startIndex < 0) startIndex = ~startIndex;
            // If startIndex is bigger or equal to the length of the array it means there is no bigger value than our search target so we can start at the last index.
            if (startIndex >= TargetsWithPos.Length) startIndex = TargetsWithPos.Length - 1;

            Entity nearestTargetEntity = TargetsWithPos[startIndex].Entity;
            float3 nearestTargetPos = TargetsWithPos[startIndex].Position;

            // Use squared distance instead of distance because it's cheaper (no need to compute a square root)
            // Performance gain for this job with 1000 seekers and 1000 targets: 0.10-0.17ms -> 0.10-0.15ms (it is pretty much similar for this job)
            float distWithNearestSq = math.distancesq(seekerPos, nearestTargetPos);

            // Search upward to find a closer target
            Search(seekerPos, startIndex + 1, TargetsWithPos.Length, +1, ref nearestTargetPos, ref nearestTargetEntity, ref distWithNearestSq);

            // Search downward to find a closer target
            Search(seekerPos, startIndex - 1, -1, -1, ref nearestTargetPos, ref nearestTargetEntity, ref distWithNearestSq);

            seeker.NearestTargetPos = nearestTargetPos;
            seeker.NearestTarget = nearestTargetEntity;


        }

        private void Search(float3 seekerPos, int startIndex, int endIndex, int step, ref float3 nearestTargetPos, ref Entity nearestTargetEntity, ref float distWithNearestSq)
        {
            for (int i = startIndex; i != endIndex; i += step)
            {
                float3 targetPos = TargetsWithPos[i].Position;
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
                    nearestTargetEntity = TargetsWithPos[i].Entity;
                }
            }
        }
    }

    public struct EntityWithPosition
    {
        public Entity Entity;
        public float3 Position;
    }

    public struct EntityXPositionComparer : IComparer<EntityWithPosition>
    {
        public int Compare(EntityWithPosition a, EntityWithPosition b)
        {
            return a.Position.x.CompareTo(b.Position.x);
        }
    }
}