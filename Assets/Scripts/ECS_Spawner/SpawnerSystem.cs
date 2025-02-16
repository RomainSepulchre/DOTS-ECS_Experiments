using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine.EventSystems;
using System.Linq;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using Unity.Entities.UniversalDelegates;

//
// Run on one thread
//

//[BurstCompile]
public partial struct SpawnerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityQuery cubeQuery = SystemAPI.QueryBuilder().WithAll<Cube>().Build();
        int cubeCount = cubeQuery.CalculateEntityCount();

        foreach (RefRW<Spawner> spawner in SystemAPI.Query<RefRW<Spawner>>())
        {
            if (cubeCount > spawner.ValueRO.SpawnCount) continue;

            int spawnCount = spawner.ValueRO.SpawnCount;

            for (int i = 0; i < spawnCount; i++)
            {
                SpawnCube(ref state, spawner, i);
            }
        }

        return;
        EntityQuery cubeQuery2 = SystemAPI.QueryBuilder().WithAll<Cube>().Build();

        int cubeCount2 = cubeQuery2.CalculateEntityCount();

        foreach (RefRW<Spawner> spawner in SystemAPI.Query<RefRW<Spawner>>())
        {
            if (cubeCount2 > spawner.ValueRO.SpawnCount) continue;

            ProcessSpawner(ref state, spawner);
        }
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
        }
    }

    private void SpawnCube(ref SystemState state, RefRW<Spawner> spawner, int index)
    {
        // Instantiate new entity
        Entity newEntity = state.EntityManager.Instantiate(spawner.ValueRO.Prefab);

        Random random = Random.CreateFromIndex((uint)((SystemAPI.Time.ElapsedTime / SystemAPI.Time.DeltaTime) + index));

        // Set entity spawn position
        state.EntityManager.SetComponentData(newEntity, LocalTransform.FromPosition(spawner.ValueRO.SpawnPosition + random.NextFloat3(-10, 10)));

        Cube cube = state.EntityManager.GetComponentData<Cube>(newEntity);
        cube.MoveSpeed = random.NextFloat(0.5f,5f);
        cube.TimerDuration = random.NextFloat(1f, 5f);
        cube.MoveDirection = random.NextFloat3Direction();
        state.EntityManager.SetComponentData(newEntity, cube);

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


//
// Add components
//

//[BurstCompile]
//public partial struct SpawnerSystem : ISystem
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
//        foreach (RefRW<Spawner> spawner in SystemAPI.Query<RefRW<Spawner>>())
//        {
//            ProcessSpawner(ref state, spawner);
//        }
//    }

//    private void ProcessSpawner(ref SystemState state, RefRW<Spawner> spawner)
//    {
//        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp); 

//        if (spawner.ValueRO.NextSpawnTime < SystemAPI.Time.ElapsedTime)
//        {
//            // Instantiate new entity
//            Entity newEntity = ecb.Instantiate(spawner.ValueRO.Prefab);

//            CubeComponent cubeComponent = new CubeComponent
//            {
//                MoveDirection = new float3(0, 1, 0),
//                MoveSpeed = 10
//            };
//            ecb.AddComponent(newEntity, cubeComponent);

//            // Set entity spawn position
//            ecb.SetComponent(newEntity, LocalTransform.FromPosition(spawner.ValueRO.SpawnPosition));

//            // Reset next spawn time
//            spawner.ValueRW.NextSpawnTime = (float)SystemAPI.Time.ElapsedTime + spawner.ValueRO.SpawnRate;

//            ecb.Playback(state.EntityManager);
//        }
//    }
//}

