using Unity.Entities;

namespace ECS.RotatingCube
{
    public struct RotationSpeed : IComponentData
    {
        public float RadiansPerSecond;
    }
}
