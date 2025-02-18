using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace ECS.ECSExperiments
{
    public partial struct SpawnerWithJobsSystem : ISystem
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
            //SpawnCubeJob spawnJob = new SpawnCubeJob
            //{
            //    ElaspedTime = SystemAPI.Time.ElapsedTime
            //};
        }
    }

    [BurstCompile]
    public partial struct SpawnCubeJob : IJobEntity
    {
        public double ElaspedTime;

        public void Execute()
        {

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



