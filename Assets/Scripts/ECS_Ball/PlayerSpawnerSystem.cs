using Project.Utilities;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Ball
{
    [UpdateAfter(typeof(ObstacleSpawnerSystem))] // Need obstacles to be spawned
    [UpdateBefore(typeof(TransformSystemGroup))] // To ensure the entity render properly in its first frame
    public partial struct PlayerSpawnerSystem : ISystem
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
            state.Enabled = false; // Disable the system after this update has run

            Config config = SystemAPI.GetSingleton<Config>();

            // Loop through obstacles to place a player next to them
            foreach(var obstTransform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<Obstacle>())
            {
                Entity player = state.EntityManager.Instantiate(config.PlayerPrefab);

                LocalTransform newTransform = new LocalTransform()
                {
                    Position = new float3
                    {
                        x = obstTransform.ValueRO.Position.x + config.PlayerOffset,
                        y = 1,
                        z = obstTransform.ValueRO.Position.z + config.PlayerOffset
                    },
                    Scale = 1,
                    Rotation = quaternion.identity
                };
                state.EntityManager.SetComponentData(player, newTransform);
            }
        }
    }
}
