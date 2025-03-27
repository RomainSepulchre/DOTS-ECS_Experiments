using Unity.Entities;
using UnityEngine;

namespace ECS.TargetAndSeekerDemo
{
	public class TargetAuthoring : MonoBehaviour
	{
        public float speed;
        public float minTimer;
        public float maxTimer;
    }

    class TargetBaker : Baker<TargetAuthoring>
    {
        public override void Bake(TargetAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            Target newTarget = new Target();
            AddComponent(entity, newTarget);

            Movement newMovement = new Movement()
            {
                Speed = authoring.speed,
                Direction = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized, // Default value is the same for every entity
                Timer = Random.Range(authoring.minTimer, authoring.maxTimer), // Default value is the same for every entity
                MinTimer = authoring.minTimer,
                MaxTimer = authoring.maxTimer,
            };
            AddComponent(entity, newMovement);
        }
    }
}
