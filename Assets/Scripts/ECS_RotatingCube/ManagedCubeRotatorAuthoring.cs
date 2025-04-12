using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS.RotatingCube
{
    public class ManagedCubeRotatorAuthoring : MonoBehaviour
    {
        public float rotationDegreesPerSeconds;
        public float verticalDegreesPerSeconds;
        public float maxYPosition;
    }

    class ManagedCubeRotatorBaker : Baker<ManagedCubeRotatorAuthoring>
    {
        public override void Bake(ManagedCubeRotatorAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            ManagedCubeRotator managedCubeRotator = new ManagedCubeRotator();
            AddComponentObject(entity, managedCubeRotator);

            RotationSpeed newRotSpeed = new RotationSpeed()
            {
                RadiansPerSecond = math.radians(authoring.rotationDegreesPerSeconds)
            };
            AddComponent(entity, newRotSpeed);

            VerticalSpeed newVertSpeed = new VerticalSpeed()
            {
                RadiansPerSecond = math.radians(authoring.verticalDegreesPerSeconds),
                MaxYPos = authoring.maxYPosition
            };
            AddComponent(entity, newVertSpeed);
        }
    }
}
