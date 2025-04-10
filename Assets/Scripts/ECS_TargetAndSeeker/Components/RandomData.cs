using Unity.Entities;
using Unity.Mathematics;

namespace ECS.TargetAndSeekerDemo
{
    public struct RandomData : IComponentData
    {
        public Random Value;
    }
}
