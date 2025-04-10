using Unity.Entities;
using Unity.Mathematics;

namespace Burst.SIMD.SimpleFustrum
{
    public struct BallSpawner : IComponentData
    {
        public int SpawnAmount;
        public Entity BallToSpawn;
        public float3 MinSpawnPos;
        public float3 MaxSpawnPos;
    }
}
