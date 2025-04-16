using Project.Utilities;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Ball
{
    public partial struct BallKickingSystem : ISystem
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

            if (Input.GetKeyDown(KeyCode.E))
            {
                foreach (var playerTransform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<Player>())
                {
                    foreach (var (ballTransform, velocity) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<Velocity>>().WithAll<Ball>())
                    {
                        float distPlayerToBallSQ = math.distancesq(playerTransform.ValueRO.Position, ballTransform.ValueRO.Position);

                        if(distPlayerToBallSQ <= config.BallKickingRangeSQ)
                        {
                            float3 dirPlayerToBall = ballTransform.ValueRO.Position - playerTransform.ValueRO.Position;
                            dirPlayerToBall.y = 0;
                            velocity.ValueRW.Value += math.normalizesafe(new Vector2(dirPlayerToBall.x, dirPlayerToBall.z)) * config.BallKickForce;
                        }
                    }
                }
            }
        }
    }
}
