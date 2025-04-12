using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS.RotatingCube
{
    public class RotatingCubeAuthoring : MonoBehaviour
    {
        public float rotationDegreesPerSeconds;
        public float verticalDegreesPerSeconds;
        public float maxYPosition;
    }

    class RotatingCubeBaker : Baker<RotatingCubeAuthoring>
    {
        public override void Bake(RotatingCubeAuthoring authoring)
        {
            Entity entity =  GetEntity(TransformUsageFlags.Dynamic);

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

            ParentCube newParentCube = new ParentCube();
            AddComponent(entity, newParentCube);
        }
    }

}
