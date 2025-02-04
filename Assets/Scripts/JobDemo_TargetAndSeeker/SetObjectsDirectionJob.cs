using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using Unity.Burst;

namespace Jobs.TargetAndSeekerDemo
{
    [BurstCompile]
    public struct SetObjectsDirectionJob : IJobParallelFor
    {
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public uint RandomSeed;
        public NativeArray<float> Timers;
        public NativeArray<float3> Directions;

        public void Execute(int index)
        {
            uint seed = RandomSeed + (uint)index;
            Random random = new Random(seed);

            Timers[index] -= DeltaTime;
            if (Timers[index] <= 0)
            {
                float2 newRandomDir = random.NextFloat2Direction();
                Directions[index] = new float3(newRandomDir.x, 0, newRandomDir.y);
                Timers[index] = random.NextFloat(1f, 5f);
            }
        }
    } 
}
