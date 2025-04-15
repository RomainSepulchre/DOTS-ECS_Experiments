using Project.Utilities;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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

            // TODO: transform in a job

            // Input.GetAxis is burst compatible
            float horizontal = Input.GetAxis($"Horizontal");
            float vertical = Input.GetAxis($"Vertical");
            float3 inputDirection = new float3(horizontal, 0, vertical);

            float3 movement = inputDirection * SystemAPI.Time.DeltaTime * config.PlayerSpeed;

            if (movement.Equals(float3.zero)) return; // No Input

            foreach(var transform in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<Player>())
            {
                float minDist = config.ObstacleRadius + (transform.ValueRO.Scale/2); // capsule radius is its scale/2
                float minDistSQ = minDist * minDist;
                float3 newPosition = transform.ValueRO.Position + movement;

                // TODO: method to check is collision highly unoptimized -> check way of doing spatial query
                // Check for player/obstacle collision
                foreach (var obstTransform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<Obstacle>())
                {
                    if(math.distancesq(newPosition, obstTransform.ValueRO.Position) <= minDistSQ)
                    {
                        newPosition = transform.ValueRO.Position;
                        break;
                    }
                }

                transform.ValueRW.Position = newPosition;
            }
        }
    }
}
