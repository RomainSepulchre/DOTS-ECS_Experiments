using Unity.Entities;
using UnityEngine;

namespace ECS.EnemyRunAwayDemo
{
    public class PlayerAuthoring : MonoBehaviour
    {
        public float speed;
    }

    public class PlayerBaker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            Player newPlayer = new Player()
            {
                Speed = authoring.speed,
            };

            AddComponent(entity, newPlayer);
        }
    }
}
