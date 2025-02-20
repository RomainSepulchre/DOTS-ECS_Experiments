using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;
using System;
using Unity.Profiling;

namespace ECS.ECSExperiments
{
    // TODO: There is something i'm not doing well with ECB, performance seems worse than not using jobs:
    // Performance are lower when spawning one by one and better when spawning 5000 entities at once so maybe it less performant with a low number of entity to create
    // Also I guess using jobs only offer a gain of performance when there is a lots of object to process and i'm only using one spawner -> check with more spawner entities
    // -> I probably need to playback the ecb later in the frame, I also need to try a parralel ECB
    // -> System update take a lot of time (0.04ms) even when there is no spawner that match the query, even when I use [RequireMatchingQueriesForUpdate] ?
    //      => Fixed by using state.RequireForUpdate() by still why does it take so much time when there is nothing to do ?

    // TODO: Retry to add a component

    public partial struct SpawnerWithJobsSystem : ISystem
    {
        EntityQuery spawnerQuery;
        EntityQuery cubeQuery;

        static readonly ProfilerMarker SpawnerJob_beforeJob = new ProfilerMarker("SpawnerJob.beforeJob");
        static readonly ProfilerMarker SpawnerJob_job = new ProfilerMarker("SpawnerJob.job");
        static readonly ProfilerMarker SpawnerJob_ecbPlayback = new ProfilerMarker("SpawnerJob.ecbPlayback");

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // using SystemAPI.QueryBuilder() is recommended over state.GetEntityQuery() (its doesn't allocate GC and is burst compaatible)
            spawnerQuery = SystemAPI.QueryBuilder().WithAllRW<Spawner>().WithAll<SpawnerUseJobs>().Build();
            cubeQuery = SystemAPI.QueryBuilder().WithAll<Cube>().Build();

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
            SpawnerJob_beforeJob.Begin();
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            NativeArray<bool> boolResult = new NativeArray<bool>(1, Allocator.TempJob);
            SpawnCubeJob spawnJob = new SpawnCubeJob
            {
                ElapsedTime = SystemAPI.Time.ElapsedTime,
                DeltaTime = SystemAPI.Time.DeltaTime,
                Ecb = ecb,
                AllCubesSpawned = boolResult,
                CubeCount = cubeQuery.CalculateEntityCount()
            };
            SpawnerJob_beforeJob.End();

            SpawnerJob_job.Begin();
            JobHandle spawnHandle = spawnJob.Schedule(spawnerQuery, state.Dependency);

            spawnHandle.Complete(); // TODO: Assign state.dependency instead
            SpawnerJob_job.End();

            SpawnerJob_ecbPlayback.Begin();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            if(spawnJob.AllCubesSpawned[0]) state.Enabled = false;
            SpawnerJob_ecbPlayback.End();
        }
    }

    [BurstCompile]
    public partial struct SpawnCubeJob : IJobEntity
    {
        [ReadOnly] public double ElapsedTime;
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public int CubeCount;
        public EntityCommandBuffer Ecb;
        public NativeArray<bool> AllCubesSpawned;
        

        public void Execute(ref Spawner spawner)
        {
            if (spawner.SpawnAllAtFirstFrame)
            {
                // Each Spawner spawns its spawnCount of Cube then disable itself
                int spawnCount = spawner.SpawnCount;

                for (int i = 0; i < spawnCount; i++)
                {
                    SpawnCube(spawner, i);
                }
                AllCubesSpawned[0] = true;
            }
            else
            {
                //// Spawn a cube at every spawn time until the spawn count is reached
                if (CubeCount >= spawner.SpawnCount)
                {
                    AllCubesSpawned[0] = true;
                    return;
                }

                ProcessSpawner(spawner);
            }
        }

        private void SpawnCube(Spawner spawner, int index)
        {
            // Instantiate new entity
            Entity newEntity = Ecb.Instantiate(spawner.Prefab);

            Random random = Random.CreateFromIndex((uint)((ElapsedTime / DeltaTime) + index));

            // Set entity spawn position
            Ecb.SetComponent(newEntity, LocalTransform.FromPosition(spawner.SpawnPosition + random.NextFloat3(-10, 10)));

            // Set random speed, timer and move direction value
            SetCubeRandomValues(newEntity, random);

            Ecb.AddComponent<AddComponentTag>(newEntity);
        }

        private void ProcessSpawner(Spawner spawner)
        {
            // Instantiate new entity
            Entity newEntity = Ecb.Instantiate(spawner.Prefab);

            Random random = Random.CreateFromIndex((uint)(ElapsedTime / DeltaTime));

            // Set entity spawn position
            Ecb.SetComponent(newEntity, LocalTransform.FromPosition(spawner.SpawnPosition + random.NextFloat3(-10, 10)));

            // Reset next spawn time
            spawner.NextSpawnTime = (float)ElapsedTime + spawner.SpawnRate;

            // Set random speed, timer and move direction value
            SetCubeRandomValues(newEntity, random);

            Ecb.AddComponent<AddComponentTag>(newEntity);
        }

        private void SetCubeRandomValues(Entity newEntity, Random random)
        {
            Cube cube = new Cube(); // I can't get component in an ECB so I need to create the component value myself
            cube.MoveDirection = random.NextFloat3Direction();
            cube.MoveSpeed = random.NextFloat(0.5f, 5f);
            cube.MoveForward = true;
            cube.TimerDuration = random.NextFloat(1f, 5f);
            cube.Timer = cube.TimerDuration;

            Ecb.SetComponent(newEntity, cube);
        }
    }
}

//
// Multithreaded using jobs
//

//[BurstCompile]
//public partial struct OptimizedSpawnerSystem : ISystem
//{
//    public void OnCreate(ref SystemState state)
//    {
//    }

//    public void OnDestroy(ref SystemState state)
//    {
//    }

//    [BurstCompile]
//    public void OnUpdate(ref SystemState state)
//    {
//        EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);

//        ProcessSpawnerJob spawnerJob = new ProcessSpawnerJob
//        {
//            ElapsedTime = SystemAPI.Time.ElapsedTime,
//            Ecb = ecb
//        };
//        spawnerJob.ScheduleParallel();
//    }

//    private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
//    {
//        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();

//        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

//        return ecb.AsParallelWriter();
//    }
//}

//[BurstCompile]
//public partial struct ProcessSpawnerJob : IJobEntity
//{
//    public EntityCommandBuffer.ParallelWriter Ecb;
//    public double ElapsedTime;

//    private void Execute([ChunkIndexInQuery] int chunkIndex, ref Spawner spawner)
//    {
//        if (spawner.NextSpawnTime < ElapsedTime)
//        {
//            Entity newEntity = Ecb.Instantiate(chunkIndex, spawner.Prefab);

//            Ecb.SetComponent(chunkIndex, newEntity, LocalTransform.FromPosition(spawner.SpawnPosition));

//            spawner.NextSpawnTime = (float)ElapsedTime + spawner.SpawnRate;
//        }
//    }
//}





