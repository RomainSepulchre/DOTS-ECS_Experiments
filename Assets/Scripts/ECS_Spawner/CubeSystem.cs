using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct CubeSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {       
        foreach ( (RefRW<Cube> cube, RefRW<LocalTransform> localTf)  in SystemAPI.Query<RefRW<Cube>, RefRW<LocalTransform>>())
        {
            ProcessTimer(ref state, cube);
            MoveCube(ref state, cube, localTf);
        }
    }


    public void ProcessTimer(ref SystemState state, RefRW<Cube> cube)
    {
        float nextTimer = cube.ValueRO.Timer - SystemAPI.Time.DeltaTime;

        if(nextTimer <= 0)
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

        if(moveFoward)
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
