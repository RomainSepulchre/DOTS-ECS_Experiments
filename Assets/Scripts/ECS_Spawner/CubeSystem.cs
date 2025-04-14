using Project.Utilities;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.ECSExperiments
{
    // TODO: Get more info on how the attribute works to undeerstand when it can be used
    // TODO: Write some notes on the attribute once I understand better how it works
    // [RequireMatchingQueriesForUpdate] // Skip OnUpdate if every EntityQuery is empty (it seems to work without declaring a specific Entity Query out of our Update)
    // -> only skip update when every queries in the system are empty
    // -> it seems better to use state.RequireForUpdate() or RequireAnyForUpdate() it allows for more precision

    public partial struct CubeSystem : ISystem
    {
        EntityQuery cubeQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
			// Use a WithAllRW() to keep only 1 query in the system (vs 1 read-only here + 1 RW in the update with WithAll() -> has it a real perf impact ?)
            cubeQuery = SystemAPI.QueryBuilder().WithAllRW<Cube, LocalTransform>().Build();

            // Require there is at least one cube that match the query to run the update
            state.RequireForUpdate(cubeQuery);
            state.RequireForUpdate<Exec_ECS_Experiments>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // TODO: Query is known even when not used ? It's a bit weird, when I used a If(useJobs) a query was made while being inside the if
            // => I'm pretty sure it's because of source generation and the fact that query are cached when using SystemAPI. I think the query is cached at compilation so its known by the system event if it is never called.

            // Use jobs
            //ProcessCubeMovementJob processCubeJob = new ProcessCubeMovementJob
            //{
            //    DeltaTime = SystemAPI.Time.DeltaTime
            //};

            //JobHandle processCubeHandle = processCubeJob.ScheduleParallel(state.Dependency);

            //// Assign job handle to state.Dependency to make sure my job will complete before next frame without any component conflict
            //// ? The job is processed parrallely from PresentationSystemGroup could this have an impact on rendering (position change only rendered the next frame ?)
            //state.Dependency = processCubeHandle;

            // No Jobs
            //foreach ((RefRW<Cube> cube, RefRW<LocalTransform> localTf) in SystemAPI.Query<RefRW<Cube>, RefRW<LocalTransform>>())
            //{
            //    ProcessTimer(ref state, cube);
            //    MoveCube(ref state, cube, localTf);
            //}

            // Use IJobChunk
            ProcessCubeMovementChunkJob processCubeChunkJob = new ProcessCubeMovementChunkJob
            {
                CubeHandle = SystemAPI.GetComponentTypeHandle<Cube>(false),
                LocalTfHandle = SystemAPI.GetComponentTypeHandle<LocalTransform>(false),
                DeltaTime = SystemAPI.Time.DeltaTime
            };

            JobHandle processCubeChunkHandle = processCubeChunkJob.ScheduleParallel(cubeQuery ,state.Dependency);
            state.Dependency = processCubeChunkHandle;

        }

        public void ProcessTimer(ref SystemState state, RefRW<Cube> cube)
        {
            float nextTimer = cube.ValueRO.Timer - SystemAPI.Time.DeltaTime;

            if (nextTimer <= 0)
            {
                nextTimer = cube.ValueRO.TimerDuration;
                cube.ValueRW.MoveForward = !cube.ValueRO.MoveForward;
            }
            cube.ValueRW.Timer = nextTimer;
        }

        public void MoveCube(ref SystemState state, RefRW<Cube> cube, RefRW<LocalTransform> localTf)
        {
            bool moveFoward = cube.ValueRO.MoveForward;
            float3 nextPos;

            if (moveFoward)
            {
                nextPos = localTf.ValueRO.Position + (cube.ValueRO.MoveDirection * cube.ValueRO.MoveSpeed * SystemAPI.Time.DeltaTime);
            }
            else
            {
                nextPos = localTf.ValueRO.Position - (cube.ValueRO.MoveDirection * cube.ValueRO.MoveSpeed * SystemAPI.Time.DeltaTime);
            }

            localTf.ValueRW.Position = nextPos;
        }
    }

    [BurstCompile]
    public partial struct ProcessCubeMovementJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;

        public void Execute(ref Cube cube, ref LocalTransform localTf)
        {
            // Process Timer
            float nextTimer = cube.Timer - DeltaTime;

            if (nextTimer <= 0)
            {
                nextTimer = cube.TimerDuration;
                cube.MoveForward = !cube.MoveForward;
            }
            cube.Timer = nextTimer;

            // Move Cube
            bool moveFoward = cube.MoveForward;
            float3 nextPos;

            if (moveFoward)
            {
                nextPos = localTf.Position + (cube.MoveDirection * cube.MoveSpeed * DeltaTime);
            }
            else
            {
                nextPos = localTf.Position - (cube.MoveDirection * cube.MoveSpeed * DeltaTime);
            }

            localTf.Position = nextPos;
        }
    }

    [BurstCompile]
    public partial struct ProcessCubeMovementChunkJob : IJobChunk
    {
        public ComponentTypeHandle<Cube> CubeHandle;
        public ComponentTypeHandle<LocalTransform> LocalTfHandle;
        [ReadOnly] public float DeltaTime;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {

            NativeArray<Cube> cubes = chunk.GetNativeArray(ref CubeHandle);
            NativeArray<LocalTransform> localTfs = chunk.GetNativeArray(ref LocalTfHandle);

            ChunkEntityEnumerator enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);

            while(enumerator.NextEntityIndex(out int i))
            {
                // Process Timer
                Cube newCube = cubes[i];
                float nextTimer = cubes[i].Timer - DeltaTime;

                if (nextTimer <= 0)
                {
                    nextTimer = cubes[i].TimerDuration;
                    newCube.MoveForward = !cubes[i].MoveForward;
                }
                newCube.Timer = nextTimer;
                cubes[i] = newCube;

                // Move Cube
                LocalTransform newLocalTf = localTfs[i];
                bool moveFoward = cubes[i].MoveForward;
                float3 nextPos;

                if (moveFoward)
                {
                    nextPos = localTfs[i].Position + (cubes[i].MoveDirection * cubes[i].MoveSpeed * DeltaTime);
                }
                else
                {
                    nextPos = localTfs[i].Position - (cubes[i].MoveDirection * cubes[i].MoveSpeed * DeltaTime);
                }

                newLocalTf.Position = nextPos;
                localTfs[i] = newLocalTf;
            }
        }
    }
}