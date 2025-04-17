using Project.Utilities;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.Ball
{
    public partial struct ObstacleSpawnerSystem : ISystem
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

            Random random = new Random(config.RandomSeed);
            float obstacleScale = config.ObstacleRadius * 2;

            for (int col = 0; col < config.NumColumns; col++)
            {
                for (int row = 0; row < config.NumRows; row++)
                {
                    Entity obstacle = state.EntityManager.Instantiate(config.ObstaclePrefab);

                    LocalTransform newTransform = new LocalTransform()
                    {
                        Position = new float3
                        {
                            x = (col * config.ObstacleGridCellSize) + random.NextFloat(config.ObstacleOffset),
                            y = 0,
                            z = (row * config.ObstacleGridCellSize) + random.NextFloat(config.ObstacleOffset)
                        },
                        Scale = obstacleScale,
                        Rotation = quaternion.identity
                    };
                    state.EntityManager.SetComponentData(obstacle, newTransform);
                }
            }
        }
    }
}
