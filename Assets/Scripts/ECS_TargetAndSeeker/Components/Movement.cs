using Unity.Entities;
using Unity.Mathematics;

namespace ECS.TargetAndSeekerDemo
{
    public struct Movement : IComponentData
    {
        public float Speed;
        public float3 Direction;
        public float Timer;
        public float MinTimer;
        public float MaxTimer;
    }
}
