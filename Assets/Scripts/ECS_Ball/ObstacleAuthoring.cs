using Unity.Entities;
using UnityEngine;

namespace ECS.Ball
{
    public class ObstacleAuthoring : MonoBehaviour
    {
        
    }

    class ObstacleBaker : Baker<ObstacleAuthoring>
    {
        public override void Bake(ObstacleAuthoring authoring)
        {
            Entity entity =  GetEntity(TransformUsageFlags.Dynamic);

            Obstacle newObstacle = new Obstacle();
            AddComponent(entity, newObstacle);
        }
    }
}
