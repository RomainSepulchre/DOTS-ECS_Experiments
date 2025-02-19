using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using Unity.Profiling;

namespace ECS.ECSExperiments
{
    //
    // Run on one thread
    //
        
    public partial struct SpawnerSystem : ISystem
    {
        EntityQuery cubeQuery; // It probably better to assign our query once and cache it than to recreate it every update
        EntityQuery spawnerQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            cubeQuery = SystemAPI.QueryBuilder().WithAll<Cube>().Build();
            spawnerQuery = SystemAPI.QueryBuilder().WithAllRW<Spawner>().WithNone<SpawnerUseJobs>().Build();

            // Require there is at least one spawner that match the query to run the update
            state.RequireForUpdate(spawnerQuery);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            bool allCubesSpawned = false;
            foreach (RefRW<Spawner> spawner in SystemAPI.Query<RefRW<Spawner>>().WithNone<SpawnerUseJobs>())
            {
                bool spawnAllCubes = spawner.ValueRO.SpawnAllAtFirstFrame;
                int cubeCount = cubeQuery.CalculateEntityCount();

                if (spawnAllCubes)
                {
                    // Each Spawner spawns its spawnCount of Cube then disable itself
                    int spawnCount = spawner.ValueRO.SpawnCount;

                    for (int i = 0; i < spawnCount; i++)
                    {
                        SpawnCube(ref state, spawner, i);
                    }

                    allCubesSpawned = true;
                }
                else
                {
                    // Spawn a cube at every spawn time until the spawn count is reached
                    if (cubeCount >= spawner.ValueRO.SpawnCount)
                    {
                        allCubesSpawned = true;
                        continue;
                    }

                    ProcessSpawner(ref state, spawner);
                }
            }

            // If all cubes spawned or, disable the system
            if (allCubesSpawned) state.Enabled = false;
        }

        private void ProcessSpawner(ref SystemState state, RefRW<Spawner> spawner)
        {
            if (spawner.ValueRO.NextSpawnTime < SystemAPI.Time.ElapsedTime)
            {
                // Instantiate new entity
                Entity newEntity = state.EntityManager.Instantiate(spawner.ValueRO.Prefab);

                Random random = Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime / SystemAPI.Time.DeltaTime));

                // Set entity spawn position
                state.EntityManager.SetComponentData(newEntity, LocalTransform.FromPosition(spawner.ValueRO.SpawnPosition + random.NextFloat3(-10, 10)));

                // Reset next spawn time
                spawner.ValueRW.NextSpawnTime = (float)SystemAPI.Time.ElapsedTime + spawner.ValueRO.SpawnRate;

                // Set random speed, timer and move direction value
                SetCubeRandomValues(ref state, newEntity, random);
            }
        }

        private void SpawnCube(ref SystemState state, RefRW<Spawner> spawner, int index)
        {
            // Instantiate new entity
            Entity newEntity = state.EntityManager.Instantiate(spawner.ValueRO.Prefab);

            Random random = Random.CreateFromIndex((uint)((SystemAPI.Time.ElapsedTime / SystemAPI.Time.DeltaTime) + index));

            // Set entity spawn position
            state.EntityManager.SetComponentData(newEntity, LocalTransform.FromPosition(spawner.ValueRO.SpawnPosition + random.NextFloat3(-10, 10)));

            // Set random speed, timer and move direction value
            SetCubeRandomValues(ref state, newEntity, random);
        }

        private void SetCubeRandomValues(ref SystemState state, Entity newEntity, Random random)
        {
            Cube cube = state.EntityManager.GetComponentData<Cube>(newEntity);
            cube.MoveDirection = random.NextFloat3Direction();
            cube.MoveSpeed = random.NextFloat(0.5f, 5f);
            cube.TimerDuration = random.NextFloat(1f, 5f);
            cube.Timer = cube.TimerDuration;

            state.EntityManager.SetComponentData(newEntity, cube);
        }
    }
}

