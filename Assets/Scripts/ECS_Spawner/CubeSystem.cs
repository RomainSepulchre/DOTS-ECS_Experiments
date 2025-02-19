using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;
using UnityEngine.SocialPlatforms;

namespace ECS.ECSExperiments
{
    // TODO: Get more info on how the attribute works to undeerstand when it can be used
    // TODO: Write some notes on the attribute once I understand better how it works
    // It seems that if only one of the system queries match, update is still processed?
    // -> it's the case it's only skip update when every queries in the system are empty, I need to take look at state.RequireForUpdate()
    [RequireMatchingQueriesForUpdate] // Skip OnUpdate if every EntityQuery is empty (it seems to work without declaring a specific Entity Query out of our Update)
    public partial struct CubeSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // TODO: It's a bit weird, when I used a If(useJobs) a query was made while being inside the if -> link to source generation -> Query is known even when not used ? 

            // Use jobs
            ProcessCubeMovementJob processCubeJob = new ProcessCubeMovementJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            };

            JobHandle processCubeHandle = processCubeJob.ScheduleParallel(state.Dependency);

            // TODO: Try to set state.Dependency instead of completing early
            processCubeHandle.Complete();

            // No Jobs
            //foreach ((RefRW<Cube> cube, RefRW<LocalTransform> localTf) in SystemAPI.Query<RefRW<Cube>, RefRW<LocalTransform>>())
            //{
            //    ProcessTimer(ref state, cube);
            //    MoveCube(ref state, cube, localTf);
            //}
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
        public float DeltaTime;

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
}