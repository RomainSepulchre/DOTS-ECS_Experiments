using Unity.Entities;
using Unity.Mathematics;

namespace Burst.SIMD.SimpleFustrum
{
    public struct RandomData : IComponentData
    {
        public Random Value;
    }
}
