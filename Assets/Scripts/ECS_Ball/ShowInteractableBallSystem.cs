using Project.Utilities;
using System.Xml.Schema;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Ball
{
    [UpdateAfter(typeof(PlayerMovementSystem))]
    public partial struct ShowInteractableBallSystem : ISystem
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

            foreach (var (ballTransform, ballBaseColor, carriedEnable) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<HDRPMaterialPropertyBaseColor>, EnabledRefRO<Carried>>().WithAll<Ball>()
                    .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
            {
                foreach (var playerTransform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<Player>().WithDisabled<Carry>())
                {
                    float distSQ = math.distancesq(playerTransform.ValueRO.Position, ballTransform.ValueRO.Position);

                    if (distSQ <= config.BallInteractionRangeSQ && carriedEnable.ValueRO == false)
                    {
                        ballBaseColor.ValueRW.Value = new float4(0.7555548f, 0.25f, 0f, 1f);
                        break;
                    }
                    else
                    {
                        ballBaseColor.ValueRW.Value = new float4(0.7555548f, 1f, 0f, 1f);
                    }
                }
            }
        }
    }
}
