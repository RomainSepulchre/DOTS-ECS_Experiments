using ECS.TargetAndSeekerDemo;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Transforms;
using UnityEngine;

namespace ECS.TargetAndSeekerDemo
{
    [UpdateAfter(typeof(SeekerSystem))]
    public partial struct DrawDebugLineSystem : ISystem
    {
        EntityQuery seekersQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            seekersQuery = SystemAPI.QueryBuilder().WithAllRW<Seeker>().Build();
            state.RequireForUpdate(seekersQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Draw debug line on main thread
            //foreach (var (seeker, transform) in SystemAPI.Query<RefRO<Seeker>, RefRO<LocalTransform>>())
            //{
            //    float3 seekerPos = transform.ValueRO.Position;
            //    Debug.DrawLine(seekerPos, seeker.ValueRO.NearestTargetPos, Color.white);
            //}

            // TODO: This could be moved in SeekerSystem ?? 
            // Draw debug line using a job
            DrawDebugLineJob drawDebugLineJob = new DrawDebugLineJob();
            state.Dependency = drawDebugLineJob.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct DrawDebugLineJob : IJobEntity
    {
        public void Execute(in Seeker seeker, in LocalTransform transform)
        {
            float3 seekerPos = transform.Position;
            Debug.DrawLine(seekerPos, seeker.NearestTargetPos, Color.white);
        }
    }
}
