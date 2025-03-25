using Unity.Entities;
using UnityEngine;

namespace ECS.EnemyRunAwayDemo
{
    public class EnemyAuthoring : MonoBehaviour
    {
        public float speed;
        public float tooCloseThreshold;
        public float xAreaLimit;
        public float yAreaLimit;
    }

    public class EnemyBaker : Baker<EnemyAuthoring>
    {
        public override void Bake(EnemyAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            Enemy newEnemy = new Enemy()
            {
                Speed = authoring.speed,
                TooCloseThreshold = authoring.tooCloseThreshold,
                XAreaLimit = authoring.xAreaLimit,
                YAreaLimit = authoring.yAreaLimit,
            };

            AddComponent(entity, newEnemy);
        }
    }
}
