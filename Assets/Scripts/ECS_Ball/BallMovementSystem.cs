using Project.Utilities;
using Unity.Burst;
using Unity.Collections;
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

            // TODO: TempJob vs state.WorldUpdateAllocator ?
            NativeArray<LocalTransform> obstTransforms = SystemAPI.QueryBuilder().WithAll<LocalTransform, Obstacle>().Build().ToComponentDataArray<LocalTransform>(Allocator.TempJob);

            BallMovementJob moveJob = new BallMovementJob()
            {
                Config = config,
                ObstTransforms = obstTransforms,
                DeltaTime = SystemAPI.Time.DeltaTime
            };
            state.Dependency = moveJob.ScheduleParallel(state.Dependency);
            obstTransforms.Dispose(state.Dependency);
        }
    }

    [WithAll(typeof(Ball))]
    [WithDisabled(typeof(Carried))]
    [BurstCompile]
    public partial struct BallMovementJob : IJobEntity
    {
        [ReadOnly] public Config Config;
        [ReadOnly] public NativeArray<LocalTransform> ObstTransforms;
        [ReadOnly] public float DeltaTime;

        public void Execute(ref LocalTransform ballTransform, ref Velocity velocity)
        {
            if (velocity.Value.Equals(float2.zero)) return;

            float minDist = Config.ObstacleRadius + (ballTransform.Scale / 2); // sphere radius is its scale/2
            float minDistSQ = minDist * minDist;

            float magnitude = math.length(velocity.Value);
            float3 velocityDirection = new float3(velocity.Value.x, 0, velocity.Value.y);
            float3 newPosition = ballTransform.Position + velocityDirection * DeltaTime;

            // TODO: method to check is collision highly unoptimized -> check way of doing spatial query
            // Check Ball/Obstacle collision
            foreach (var obstacleTransform in ObstTransforms)
            {
                if (math.distancesq(obstacleTransform.Position, ballTransform.Position) <= minDistSQ)
                {
                    // Reflect ball
                    float2 obstToBallDirection = math.normalize(ballTransform.Position - obstacleTransform.Position).xz;
                    velocity.Value = math.reflect(math.normalize(velocity.Value), obstToBallDirection) * magnitude;
                    float3 reflectDirection = new float3(velocity.Value.x, 0, velocity.Value.y);
                    newPosition = ballTransform.Position + reflectDirection * DeltaTime;
                    break;
                }
            }

            ballTransform.Position = newPosition;

            // Decay velocity
            float decayFactor = Config.BallVelocityDecay * DeltaTime;
            float newMagnitude = math.max(magnitude - decayFactor, 0);
            velocity.Value = math.normalizesafe(velocity.Value) * newMagnitude; // math.normalizesafe -> return a default vector if result is not finite
        }
    }
}
