using ECS.TargetAndSeekerDemo;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Transforms;
using UnityEngine;

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
        foreach (var (seeker, transform) in SystemAPI.Query<RefRO<Seeker>, RefRO<LocalTransform>>())
        {
            float3 seekerPos = transform.ValueRO.Position;
            Debug.DrawLine(seekerPos, seeker.ValueRO.NearestTargetPos, Color.white);
        }
    }
}
