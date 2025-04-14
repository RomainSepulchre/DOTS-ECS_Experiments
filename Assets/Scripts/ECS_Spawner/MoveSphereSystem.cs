using Project.Utilities;
using ECS.ECSExperiments;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECS.ECSExperiments
{
    [UpdateAfter(typeof(InputSystem))]
    public partial struct MoveSphereSystem : ISystem
    {
        SystemHandle inputSystemHandle;
        EntityQuery sphereQuery;

        public void OnCreate(ref SystemState state)
        {
            inputSystemHandle = state.World.GetExistingSystem<InputSystem>(); // Not compatible with burst compile
            sphereQuery = SystemAPI.QueryBuilder().WithAllRW<LocalTransform>().WithAll<MoveableSphere>().Build();
            state.RequireForUpdate(sphereQuery);
            state.RequireForUpdate<Exec_ECS_Experiments>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            InputData currentInputData = SystemAPI.GetComponent<InputData>(inputSystemHandle);

            ProcessSphereMovementJob sphereMoveJob = new ProcessSphereMovementJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                InputData = currentInputData
            };

            state.Dependency = sphereMoveJob.Schedule(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct ProcessSphereMovementJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public InputData InputData;

        public void Execute(ref LocalTransform localTf, in MoveableSphere moveSphere)
        {
            float3 direction = float3.zero;

            if (InputData.UpKeyPressed)
            {
                direction += localTf.Forward();
            }

            if (InputData.DownKeyPressed)
            {
                direction -= localTf.Forward();
            }

            if (InputData.LeftKeyPressed)
            {
                direction -= localTf.Right();
            }

            if (InputData.RightKeyPressed)
            {
                direction += localTf.Right();
            }

            if (InputData.SpaceKeyPressed)
            {
                direction += localTf.Up();
            }

            if (InputData.RCtrlKeyPressed)
            {
                direction -= localTf.Up();
            }

            localTf.Position += (direction * DeltaTime * moveSphere.Speed);
        }
    }
}
