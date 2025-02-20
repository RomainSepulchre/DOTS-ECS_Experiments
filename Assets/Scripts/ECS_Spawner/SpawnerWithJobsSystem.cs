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
using ECS.ECSExperiments;

namespace ECS.ECSExperiments
{
    // TODO: There is something i'm not doing well with ECB, performance seems worse than not using jobs:
    // Performance are lower when spawning one by one and better when spawning 5000 entities at once so maybe it less performant with a low number of entity to create
    // Also I guess using jobs only offer a gain of performance when there is a lots of object to process and i'm only using one spawner -> check with more spawner entities
    // -> I probably need to playback the ecb later in the frame, I also need to try a parralel ECB
    // -> System update take a lot of time (0.04ms) even when there is no spawner that match the query, even when I use [RequireMatchingQueriesForUpdate] ?
    //      => Fixed by using state.RequireForUpdate() by still why does it take so much time when there is nothing to do ?
    // => Passing an EntityQuery to an EntityManager method is the most efficient way to make structural changes. This is because the method can operate on whole chunks rather than individual entities.
    // => The added overhead of using an EntityCommandBuffer might be worth it to avoid introducing a new sync point.
    // -> On main thread spawn system, doing all structural changes in an ecb takes at worse the same time and is sometimes faster, what is happening with the ecb in my job ???

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
            var singleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = singleton.CreateCommandBuffer(state.WorldUnmanaged);
            SpawnerJob_beforeJob.End();

            //
            // Single job
            //

            //SpawnerJob_job.Begin();
            //SpawnCubeJob spawnJob = new SpawnCubeJob
            //{
            //    ElapsedTime = SystemAPI.Time.ElapsedTime,
            //    DeltaTime = SystemAPI.Time.DeltaTime,
            //    Ecb = ecb,
            //    CubeCount = cubeQuery.CalculateEntityCount()
            //};
            
            //JobHandle spawnHandle = spawnJob.Schedule(spawnerQuery, state.Dependency);

            //state.Dependency = spawnHandle;
            //SpawnerJob_job.End();

            //
            // ParralelJob
            //

            SpawnerJob_job.Begin();
            SpawnCubeJobParallel spawnJobParallel = new SpawnCubeJobParallel
            {
                ElapsedTime = SystemAPI.Time.ElapsedTime,
                DeltaTime = SystemAPI.Time.DeltaTime,
                Ecb = ecb.AsParallelWriter(),
                CubeCount = cubeQuery.CalculateEntityCount()
            };

            JobHandle spawnParallelHandle = spawnJobParallel.ScheduleParallel(spawnerQuery, state.Dependency);
            state.Dependency = spawnParallelHandle;
            SpawnerJob_job.End();
        }
    }

    [BurstCompile]
    public partial struct SpawnCubeJob : IJobEntity
    {
        [ReadOnly] public double ElapsedTime;
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public int CubeCount;
        public EntityCommandBuffer Ecb;
        

        public void Execute(Entity entity, ref Spawner spawner)
        {
            bool allCubesSpawned = false;
            if (spawner.SpawnAllAtFirstFrame)
            {
                // Each Spawner spawns its spawnCount of Cube then disable itself
                int spawnCount = spawner.SpawnCount;

                for (int i = 0; i < spawnCount; i++)
                {
                    SpawnCube(spawner, i);
                }
                allCubesSpawned = true;
            }
            else
            {
                //// Spawn a cube at every spawn time until the spawn count is reached
                if (CubeCount >= spawner.SpawnCount)
                {
                    allCubesSpawned = true;
                }
                else
                {
                    ProcessSpawner(spawner);
                }
            }

            // Disable the entity instead of disabling the system because otherwise I need to complete the job inside the system and can't pass the job hadnle to the system
            if (allCubesSpawned) Ecb.AddComponent<Disabled>(entity);
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

    [BurstCompile]
    public partial struct SpawnCubeJobParallel : IJobEntity
    {
        [ReadOnly] public double ElapsedTime;
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public int CubeCount;
        public EntityCommandBuffer.ParallelWriter Ecb;

        public void Execute(Entity entity, [ChunkIndexInQuery] int chunkIndexInQuery, ref Spawner spawner)
        {
            bool allCubesSpawned = false;
            if (spawner.SpawnAllAtFirstFrame)
            {
                // Each Spawner spawns its spawnCount of Cube then disable itself
                int spawnCount = spawner.SpawnCount;

                for (int i = 0; i < spawnCount; i++)
                {
                    SpawnCube(spawner, i, chunkIndexInQuery);
                }
                allCubesSpawned = true;
            }
            else
            {
                //// Spawn a cube at every spawn time until the spawn count is reached
                if (CubeCount >= spawner.SpawnCount)
                {
                    allCubesSpawned = true;
                }
                else
                {
                    ProcessSpawner(spawner, chunkIndexInQuery);
                }
            }

            // Disable the entity instead of disabling the system because otherwise I need to complete the job inside the system and can't pass the job hadnle to the system
            if (allCubesSpawned) Ecb.AddComponent<Disabled>(chunkIndexInQuery, entity);
        }

        private void SpawnCube(Spawner spawner, int index, int chunkIndexInQuery)
        {
            // Instantiate new entity
            Entity newEntity = Ecb.Instantiate(chunkIndexInQuery, spawner.Prefab);

            Random random = Random.CreateFromIndex((uint)((ElapsedTime / DeltaTime) + index));

            // Set entity spawn position
            Ecb.SetComponent(chunkIndexInQuery, newEntity, LocalTransform.FromPosition(spawner.SpawnPosition + random.NextFloat3(-10, 10)));

            // Set random speed, timer and move direction value
            SetCubeRandomValues(newEntity, random, chunkIndexInQuery);

            Ecb.AddComponent<AddComponentTag>(chunkIndexInQuery, newEntity);
        }

        private void ProcessSpawner(Spawner spawner, int chunkIndexInQuery)
        {
            // Instantiate new entity
            Entity newEntity = Ecb.Instantiate(chunkIndexInQuery, spawner.Prefab);

            Random random = Random.CreateFromIndex((uint)(ElapsedTime / DeltaTime));

            // Set entity spawn position
            Ecb.SetComponent(chunkIndexInQuery, newEntity, LocalTransform.FromPosition(spawner.SpawnPosition + random.NextFloat3(-10, 10)));

            // Reset next spawn time
            spawner.NextSpawnTime = (float)ElapsedTime + spawner.SpawnRate;

            // Set random speed, timer and move direction value
            SetCubeRandomValues(newEntity, random, chunkIndexInQuery);

            Ecb.AddComponent<AddComponentTag>(chunkIndexInQuery, newEntity);
        }

        private void SetCubeRandomValues(Entity newEntity, Random random, int chunkIndexInQuery)
        {
            Cube cube = new Cube(); // I can't get component in an ECB so I need to create the component value myself
            cube.MoveDirection = random.NextFloat3Direction();
            cube.MoveSpeed = random.NextFloat(0.5f, 5f);
            cube.MoveForward = true;
            cube.TimerDuration = random.NextFloat(1f, 5f);
            cube.Timer = cube.TimerDuration;

            Ecb.SetComponent(chunkIndexInQuery, newEntity, cube);
        }
    }
}