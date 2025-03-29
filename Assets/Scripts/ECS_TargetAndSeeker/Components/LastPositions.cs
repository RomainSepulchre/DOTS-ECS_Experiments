using Unity.Entities;
using Unity.Mathematics;

namespace ECS.TargetAndSeekerDemo
{
    [InternalBufferCapacity(200)]
    public struct LastPositions : IBufferElementData
    {
        public float3 Position;
    }
}
