using Project.Utilities;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Burst.SIMD.SimpleFustrum
{
    public partial struct BallSpawnerSystem : ISystem
    {

        EntityQuery spawnerQuery;
        public void OnCreate(ref SystemState state)
        {
            spawnerQuery = SystemAPI.QueryBuilder().WithAll<BallSpawner>().WithAllRW<RandomData>().Build();
            state.RequireForUpdate(spawnerQuery);
            state.RequireForUpdate<Exec_SIMD_SimpleFustrum>();
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach(var (spawner, random) in SystemAPI.Query<RefRO<BallSpawner>, RefRW<RandomData>>() )
            {
                for (var i = 0; i < spawner.ValueRO.SpawnAmount; i++)
                {
                    Entity newBall = state.EntityManager.Instantiate(spawner.ValueRO.BallToSpawn);

                    LocalTransform randomPos = LocalTransform.FromPosition(random.ValueRW.Value.NextFloat3(spawner.ValueRO.MinSpawnPos, spawner.ValueRO.MaxSpawnPos));
                    state.EntityManager.SetComponentData(newBall, randomPos);
                }
            }

            // Disable system once ball spawned
            state.Enabled = false;
        }
    }
}
