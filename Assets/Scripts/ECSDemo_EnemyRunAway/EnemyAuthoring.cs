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
            throw new System.NotImplementedException();
        }
    }
}
