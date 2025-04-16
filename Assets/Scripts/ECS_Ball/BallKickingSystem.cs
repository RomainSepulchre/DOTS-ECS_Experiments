using Project.Utilities;
using Unity.Burst;
using Unity.Collections;
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
                EntityQuery playersQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform, Player>().Build();

                KickBallJob kickBallJob = new KickBallJob()
                {
                    Config = config,
                    PlayerTransforms = playersQuery.ToComponentDataArray<LocalTransform>(state.WorldUpdateAllocator),
                };
                state.Dependency = kickBallJob.ScheduleParallel(state.Dependency);

                //foreach (var playerTransform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<Player>())
                //{
                //    foreach (var (ballTransform, velocity) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<Velocity>>().WithAll<Ball>())
                //    {
                //        float distPlayerToBallSQ = math.distancesq(playerTransform.ValueRO.Position, ballTransform.ValueRO.Position);

                //        if (distPlayerToBallSQ <= config.BallKickingRangeSQ)
                //        {
                //            float2 dirPlayerToBall = ballTransform.ValueRO.Position.xz - playerTransform.ValueRO.Position.xz;
                //            velocity.ValueRW.Value += math.normalizesafe(dirPlayerToBall) * config.BallKickForce;
                //        }
                //    }
                //}
            }
        }
    }

    // TODO: job issue
    // To transform in a job I needed to use the ball data as input to be able to modify the velocity
    // -> which cause issue if 2 players are near the same ball we take into account the first player in the array and not the closest
    // (it should never happen in this case with how player are spawned but I still would like to find a solution)
    [WithAll(typeof(Ball))]
    [BurstCompile]
    public partial struct KickBallJob : IJobEntity
    {
        [ReadOnly] public Config Config;
        [ReadOnly] public NativeArray<LocalTransform> PlayerTransforms;

        public void Execute(ref Velocity velocity, in LocalTransform ballTransform)
        {         
            for (int i = 0; i < PlayerTransforms.Length; i++)
            {
                float distPlayerToBallSQ = math.distancesq(PlayerTransforms[i].Position, ballTransform.Position);

                if (distPlayerToBallSQ <= Config.BallKickingRangeSQ)
                {
                    float2 dirPlayerToBall = ballTransform.Position.xz - PlayerTransforms[i].Position.xz;
                    velocity.Value += math.normalizesafe(dirPlayerToBall) * Config.BallKickForce;
                    return;
                }
            }
        }
    }
}
