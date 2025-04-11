using DOTS.Utilities;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.TargetAndSeekerDemo
{
    public partial struct SpawnerSystem : ISystem
    {
        // Spawn the entity assigned at the first frame

        EntityQuery spawnersQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            spawnersQuery = SystemAPI.QueryBuilder().WithAll<Spawner>().Build();
            state.RequireForUpdate(spawnersQuery);
            state.RequireForUpdate<Exec_ECS_TargetAndSeeker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (spawner, entity) in SystemAPI.Query<RefRO<Spawner>>().WithEntityAccess())
            {
                for (int i = 0; i < spawner.ValueRO.SpawnAmount; i++)
                {
                    Entity newEntity = state.EntityManager.Instantiate(spawner.ValueRO.EntityToSpawn);

                    Random random = Random.CreateFromIndex((uint)(spawner.ValueRO.RandomSeed + i + (SystemAPI.Time.ElapsedTime / SystemAPI.Time.DeltaTime)));

                    // Set a random position
                    float xPos = random.NextFloat(-spawner.ValueRO.XSpawnAreaLimit, spawner.ValueRO.XSpawnAreaLimit);
                    float zPos = random.NextFloat(-spawner.ValueRO.ZSpawnAreaLimit, spawner.ValueRO.ZSpawnAreaLimit);
                    float3 spawnPos = new float3(xPos, 0, zPos);
                    state.EntityManager.SetComponentData(newEntity, LocalTransform.FromPosition(spawnPos));

                    // Set movement default random value otherwise they would be the same for every entity
                    if(SystemAPI.HasComponent<Movement>(newEntity))
                    {
                        Movement newMovement = SystemAPI.GetComponent<Movement>(newEntity);
                        float2 randomDir = random.NextFloat2Direction();
                        newMovement.Direction = new float3(randomDir.x,0, randomDir.y);
                        newMovement.Timer = random.NextFloat(newMovement.MinTimer, newMovement.MaxTimer);
                        state.EntityManager.SetComponentData(newEntity, newMovement);
                    }

                    // Assign the random by using the random generated for spawning this entity
                    if (SystemAPI.HasComponent<RandomData>(newEntity))
                    {
                        RandomData newRandom = SystemAPI.GetComponent<RandomData>(newEntity);
                        newRandom.Value = random;
                        state.EntityManager.SetComponentData(newEntity, newRandom);
                    }
                }

                state.EntityManager.SetComponentEnabled(entity, ComponentType.ReadWrite<Spawner>(), false);
            }
        }
    } 
}
