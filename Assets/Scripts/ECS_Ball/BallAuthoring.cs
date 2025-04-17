using Unity.Entities;
using UnityEngine;
using Unity.Rendering;
using Unity.Mathematics;

namespace ECS.Ball
{
    public class BallAuthoring : MonoBehaviour
    {

    }

    class BallBaker : Baker<BallAuthoring>
    {
        public override void Bake(BallAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            Ball newBall = new Ball();
            AddComponent(entity, newBall);

            Velocity newVelocity = new Velocity();
            AddComponent(entity,newVelocity);

            Carried newCarried = new Carried();
            AddComponent(entity, newCarried);
            SetComponentEnabled<Carried>(entity, false);

            HDRPMaterialPropertyBaseColor newBaseColor = new HDRPMaterialPropertyBaseColor() { Value = new float4(0.7555548f, 1f, 0f, 1f) };
            AddComponent(entity, newBaseColor);
        }
    }
}
