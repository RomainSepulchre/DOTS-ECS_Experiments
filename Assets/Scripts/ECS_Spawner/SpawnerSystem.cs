using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using Unity.Profiling;
using System;
using Project.Utilities;

namespace ECS.ECSExperiments
{
    //
    // Run on one thread
    //

    [UpdateBefore(typeof(CubeSystem))] // Make sure it's runned before the other system that use jobs to avoid sync point
    [UpdateBefore(typeof(SpawnerWithJobsSystem))]
    public partial struct SpawnerSystem : ISystem
    {
        EntityQuery cubeQuery; // It probably better to assign our query once and cache it than to recreate it every update
        EntityQuery spawnerQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // TODO: From what I understood calling System API already automatically cache the query
            // -> So i'm not sure it's needed to declare them here
            cubeQuery = SystemAPI.QueryBuilder().WithAll<Cube>().Build();
            spawnerQuery = SystemAPI.QueryBuilder().WithAllRW<Spawner>().WithNone<SpawnerUseJobs>().Build();

            // Require there is at least one spawner that match the query to run the update
            state.RequireForUpdate(spawnerQuery);
            state.RequireForUpdate<Exec_ECS_Experiments>();

            // ? This can't be mixed with a singleton backed in the hierarchy, this happens before the end of the baking
            //if (SystemAPI.HasSingleton<SpawnersManager>() == false) // Create the singleton a default value if doesn't exist
            //{
            //    SpawnersManager spawnsManager = new SpawnersManager { MaximumSpawnCount = 9999 };
            //    state.EntityManager.CreateSingleton(spawnsManager, "SpawnerManagerSingleton");
            //}
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            int maxSpawnLimit = SystemAPI.GetSingleton<SpawnersManager>().MaximumSpawnCount;
            int cubeCount = cubeQuery.CalculateEntityCount();

            if(cubeCount >= maxSpawnLimit) 
            {
                // Disable system if we reached the spawn limit for all the spawner entities
                state.Enabled = false;
                return;
            }

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach ( var (spawner, entity) in SystemAPI.Query<RefRW<Spawner>>().WithNone<SpawnerUseJobs>().WithEntityAccess())
            {
                // We need to check this early because of ECB playback delay (we need to be sure the spawn count wasn't changed between this update and the previous) 
                if (spawner.ValueRO.SpawnCount >= spawner.ValueRO.SpawnMax) // We reached this spawner limit
                {
                    ecb.SetEnabled(entity, false);
                    continue;
                }

                bool spawnAllCubes = spawner.ValueRO.SpawnAllAtFirstFrame;
                if (spawnAllCubes)
                {
                    // Each Spawner spawns its spawnCount of Cube then disable itself
                    int spawnMax = spawner.ValueRO.SpawnMax;
                    // Only for SpawnCubesAllallECB(), we need a temporary struct on which we increment the spawnCount since we only change on the component it at ecb playback
                    //Spawner tempSpawner = SystemAPI.GetComponent<Spawner>(entity); 

                    for (int i = 0; i < spawnMax; i++)
                    {
                        //SpawnCubes(ref state, spawner, i);
                        SpawnCubes(ref state, spawner, i, ecb);
                        //SpawnCubesAllECB(ref state, spawner, i, ecb, entity, ref tempSpawner);
                    }
                }
                else
                {
                    //TODO: Two at the same pos at first frame ?

                    //ProcessSpawner(ref state, spawner);
                    ProcessSpawner(ref state, spawner, ecb);
                    //ProcessSpawnerAllECB(ref state, spawner, ecb, entity);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private void ProcessSpawner(ref SystemState state, RefRW<Spawner> spawner)
        {
            if (spawner.ValueRO.NextSpawnTime < SystemAPI.Time.ElapsedTime)
            {
                // Instantiate new entity
                Entity newEntity = state.EntityManager.Instantiate(spawner.ValueRO.Prefab);
                spawner.ValueRW.SpawnCount++;               

                Random random = Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime / SystemAPI.Time.DeltaTime));

                // Set entity spawn position
                state.EntityManager.SetComponentData(newEntity, LocalTransform.FromPosition(spawner.ValueRO.SpawnPosition + random.NextFloat3(-10, 10)));

                // Reset next spawn time
                spawner.ValueRW.NextSpawnTime = (float)SystemAPI.Time.ElapsedTime + spawner.ValueRO.SpawnRate;

                // Set random speed, timer and move direction value
                SetCubeRandomValues(ref state, newEntity, random);
            }
        }

        private void ProcessSpawner(ref SystemState state, RefRW<Spawner> spawner, EntityCommandBuffer ecb)
        {
            if (spawner.ValueRO.NextSpawnTime < SystemAPI.Time.ElapsedTime)
            {
                // Instantiate new entity
                Entity newEntity = state.EntityManager.Instantiate(spawner.ValueRO.Prefab);
                spawner.ValueRW.SpawnCount++;

                Random random = Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime / SystemAPI.Time.DeltaTime));

                // Set entity spawn position
                state.EntityManager.SetComponentData(newEntity, LocalTransform.FromPosition(spawner.ValueRO.SpawnPosition + random.NextFloat3(-10, 10)));

                // Reset next spawn time
                spawner.ValueRW.NextSpawnTime = (float)SystemAPI.Time.ElapsedTime + spawner.ValueRO.SpawnRate;

                // Set random speed, timer and move direction value
                SetCubeRandomValues(ref state, newEntity, random);
                ecb.AddComponent<AddComponentTag>(newEntity);
            }
        }

        private void ProcessSpawnerAllECB(ref SystemState state, RefRW<Spawner> spawner, EntityCommandBuffer ecb, Entity spawnerEntity)
        {
            if (spawner.ValueRO.NextSpawnTime < SystemAPI.Time.ElapsedTime)
            {
                // Instantiate new entity
                Entity newEntity = ecb.Instantiate(spawner.ValueRO.Prefab);

                // I get the spawner component from the entity to avoid copying all the value one by one from RefRW<Spawner>
                Spawner newSpawner = SystemAPI.GetComponent<Spawner>(spawnerEntity);
                newSpawner.SpawnCount++;
                newSpawner.NextSpawnTime = (float)SystemAPI.Time.ElapsedTime + spawner.ValueRO.SpawnRate;
                ecb.SetComponent<Spawner>(spawnerEntity, newSpawner); // I set the spawn count in the ecb because the entity isn't actually spawned until the ECB playback

                Random random = Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime / SystemAPI.Time.DeltaTime));

                // Set entity spawn position
                ecb.SetComponent(newEntity, LocalTransform.FromPosition(spawner.ValueRO.SpawnPosition + random.NextFloat3(-10, 10)));

                // Set random speed, timer and move direction value
                SetCubeRandomValues(ref state, newEntity, random, ecb);

                ecb.AddComponent<AddComponentTag>(newEntity);
            }
        }

        private void SpawnCubes(ref SystemState state, RefRW<Spawner> spawner, int index)
        {
            // Instantiate new entity
            Entity newEntity = state.EntityManager.Instantiate(spawner.ValueRO.Prefab);

            spawner.ValueRW.SpawnCount++;

            Random random = Random.CreateFromIndex((uint)((SystemAPI.Time.ElapsedTime / SystemAPI.Time.DeltaTime) + index));

            // Set entity spawn position
            state.EntityManager.SetComponentData(newEntity, LocalTransform.FromPosition(spawner.ValueRO.SpawnPosition + random.NextFloat3(-10, 10)));

            // Set random speed, timer and move direction value
            SetCubeRandomValues(ref state, newEntity, random);
        }

        private void SpawnCubes(ref SystemState state, RefRW<Spawner> spawner, int index, EntityCommandBuffer ecb)
        {
            // Instantiate new entity
            Entity newEntity = state.EntityManager.Instantiate(spawner.ValueRO.Prefab);

            spawner.ValueRW.SpawnCount++;

            Random random = Random.CreateFromIndex((uint)((SystemAPI.Time.ElapsedTime / SystemAPI.Time.DeltaTime) + index));

            // Set entity spawn position
            state.EntityManager.SetComponentData(newEntity, LocalTransform.FromPosition(spawner.ValueRO.SpawnPosition + random.NextFloat3(-10, 10)));

            // Set random speed, timer and move direction value
            SetCubeRandomValues(ref state, newEntity, random);

            ecb.AddComponent<AddComponentTag>(newEntity);
        }

        private void SpawnCubesAllECB(ref SystemState state, RefRW<Spawner> spawner, int index, EntityCommandBuffer ecb, Entity spawnerEntity, ref Spawner tempSpawner)
        {
            // Instantiate new entity
            Entity newEntity = ecb.Instantiate(spawner.ValueRO.Prefab);

            // I get the spawner component from the entity to avoid copying all the value one by one from RefRW<Spawner>
            tempSpawner.SpawnCount++ ;
            ecb.SetComponent<Spawner>(spawnerEntity, tempSpawner); // I set the spawn count in the ecb because the entity isn't actually spawned until the ECB playback

            Random random = Random.CreateFromIndex((uint)((SystemAPI.Time.ElapsedTime / SystemAPI.Time.DeltaTime) + index));

            // Set entity spawn position
            ecb.SetComponent(newEntity, LocalTransform.FromPosition(spawner.ValueRO.SpawnPosition + random.NextFloat3(-10, 10)));

            // Set random speed, timer and move direction value
            SetCubeRandomValues(ref state, newEntity, random, ecb);

            ecb.AddComponent<AddComponentTag>(newEntity);
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

        private void SetCubeRandomValues(ref SystemState state, Entity newEntity, Random random, EntityCommandBuffer ecb)
        {
            Cube cube = new Cube();
            cube.MoveDirection = random.NextFloat3Direction();
            cube.MoveSpeed = random.NextFloat(0.5f, 5f);
            cube.MoveForward = true;
            cube.TimerDuration = random.NextFloat(1f, 5f);
            cube.Timer = cube.TimerDuration;

            ecb.SetComponent(newEntity, cube);
        }
    }
}

