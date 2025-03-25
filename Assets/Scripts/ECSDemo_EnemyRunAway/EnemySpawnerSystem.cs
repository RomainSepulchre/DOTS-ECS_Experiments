using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.EnemyRunAwayDemo
{
    public partial struct EnemySpawnerSystem : ISystem
    {
        EntityQuery spawnerQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            spawnerQuery = SystemAPI.QueryBuilder().WithAll<EnemySpawner>().Build();
            state.RequireForUpdate(spawnerQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (spawner, entity) in SystemAPI.Query<RefRO<EnemySpawner>>().WithEntityAccess())
            {
                for (var i = 0; i < spawner.ValueRO.SpawnAmount; i++)
                {
                    Entity newEnemy = state.EntityManager.Instantiate(spawner.ValueRO.EnemyToSpawn);

                    Random random = Random.CreateFromIndex((uint)((SystemAPI.Time.ElapsedTime / SystemAPI.Time.DeltaTime) + i));

                    // Set a random position in the spawn area
                    float xPos = random.NextFloat(-spawner.ValueRO.SpawnAreaXLimit, spawner.ValueRO.SpawnAreaXLimit);
                    float yPos = random.NextFloat(-spawner.ValueRO.SpawnAreaYLimit, spawner.ValueRO.SpawnAreaYLimit);
                    float3 spawnPosition = new float3(xPos, yPos, 0);
                    state.EntityManager.SetComponentData(newEnemy, LocalTransform.FromPosition(spawnPosition));

                    // Set Player on enemy
                    Enemy enemyComp = state.EntityManager.GetComponentData<Enemy>(newEnemy);
                    enemyComp.Player = spawner.ValueRO.Player;
                    state.EntityManager.SetComponentData(newEnemy, enemyComp);
                }

                // Disabled spawner component once everything has been spawned
                state.EntityManager.SetComponentEnabled(entity, ComponentType.ReadWrite<EnemySpawner>(), false);
            }
        }
    } 
}
