using Project.Utilities;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Ball
{
    public partial struct BallMovementSystem : ISystem
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

            // TODO: Transform into job

            foreach (var (ballTransform, velocity) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<Velocity>>().WithAll<Ball>())
            {
                if (velocity.ValueRO.Value.Equals(float2.zero)) continue;

                float minDist = config.ObstacleRadius + (ballTransform.ValueRO.Scale / 2); // sphere radius is its scale/2
                float minDistSQ = minDist * minDist;

                float magnitude = math.length(velocity.ValueRO.Value);
                float3 velocityDirection = new float3(velocity.ValueRO.Value.x, 0, velocity.ValueRO.Value.y) ;
                float3 newPosition = ballTransform.ValueRO.Position + velocityDirection * SystemAPI.Time.DeltaTime;

                // Check Ball/Obstacle collision
                foreach (var obstacleTransform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<Obstacle>())
                {
                    if(math.distancesq(obstacleTransform.ValueRO.Position, ballTransform.ValueRO.Position) <= minDistSQ)
                    {
                        // Reflect ball
                        float2 obstToBallDirection = math.normalize(ballTransform.ValueRO.Position - obstacleTransform.ValueRO.Position).xz;
                        velocity.ValueRW.Value = math.reflect(math.normalize(velocity.ValueRO.Value), obstToBallDirection) * magnitude;
                        float3 reflectDirection = new float3(velocity.ValueRO.Value.x, 0, velocity.ValueRO.Value.y);
                        newPosition = ballTransform.ValueRO.Position + reflectDirection * SystemAPI.Time.DeltaTime;
                        break;
                    }
                }

                ballTransform.ValueRW.Position = newPosition;

                // Decay velocity
                float decayFactor = config.BallVelocityDecay * SystemAPI.Time.DeltaTime;
                float newMagnitude = math.max(magnitude - decayFactor, 0);
                velocity.ValueRW.Value = math.normalizesafe(velocity.ValueRO.Value) * newMagnitude; // math.normalizesafe -> return a default vector if result is not finite
            }
        }
    }
}
