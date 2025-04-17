using Project.Utilities;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Collections;

namespace ECS.Ball
{
    [UpdateBefore(typeof(TransformSystemGroup))] // To ensure the entity is rendered at proper position every time it is moved
    public partial struct PlayerMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<Exec_ECS_Ball>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Config config = SystemAPI.GetSingleton<Config>();

            // Input.GetAxis is burst compatible
            float horizontal = Input.GetAxis($"Horizontal");
            float vertical = Input.GetAxis($"Vertical");
            float3 inputDirection = new float3(horizontal, 0, vertical);

            float3 movement = inputDirection * SystemAPI.Time.DeltaTime * config.PlayerSpeed;

            if (movement.Equals(float3.zero)) return; // No Input

            // TODO: TempJob vs state.WorldUpdateAllocator ?
            NativeArray<LocalTransform> obstTransforms = SystemAPI.QueryBuilder().WithAll<LocalTransform, Obstacle>().Build().ToComponentDataArray<LocalTransform>(Allocator.TempJob);

            PlayerMovementJob moveJob = new PlayerMovementJob()
            {
                Movement = movement,
                Config = config,
                ObstTransforms = obstTransforms
            };
            state.Dependency = moveJob.ScheduleParallel(state.Dependency);
            obstTransforms.Dispose(state.Dependency);
        }
    }

    [WithAll(typeof(Player))] // Equivalent to .WithAll() in a classic entityQuery
    [BurstCompile]
    public partial struct PlayerMovementJob : IJobEntity
    {
        [ReadOnly] public float3 Movement;
        [ReadOnly] public Config Config;
        [ReadOnly] public NativeArray<LocalTransform> ObstTransforms;

        public void Execute(ref LocalTransform transform)
        {
            float minDist = Config.ObstacleRadius + (transform.Scale / 2); // capsule radius is its scale/2
            float minDistSQ = minDist * minDist;
            float3 newPosition = transform.Position + Movement;

            // TODO: method to check is collision highly unoptimized -> check way of doing spatial query
            // Check for player/obstacle collision
            foreach (var obstTransform in ObstTransforms)
            {
                if (math.distancesq(newPosition, obstTransform.Position) <= minDistSQ)
                {
                    newPosition = transform.Position;
                    break;
                }
            }

            transform.Position = newPosition;
        }
    }

}
