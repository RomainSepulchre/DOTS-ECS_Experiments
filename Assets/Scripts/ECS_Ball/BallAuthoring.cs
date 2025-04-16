using Unity.Entities;
using UnityEngine;

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
        }
    }
}
