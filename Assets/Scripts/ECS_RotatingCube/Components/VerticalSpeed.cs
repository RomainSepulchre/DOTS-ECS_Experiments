using Unity.Entities;
using UnityEngine;

namespace ECS.RotatingCube
{
    public struct VerticalSpeed : IComponentData
    {
        // It may be a bit weird to use radians for a vertical speed but we use it with a sin
        public float RadiansPerSecond;
        public float MaxYPos;
    }
}
