using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace Jobs.TargetAndSeekerDemo
{
    [BurstCompile]
    public struct MoveObjectsJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<float3> Directions;
        [ReadOnly] public float Speed;
        [ReadOnly] public float DeltaTime;

        public void Execute(int index, TransformAccess transform)
        {
            float3 newPos = (float3)transform.position + (Directions[index] * Speed * DeltaTime);
            transform.position = newPos;
        }
    }

}