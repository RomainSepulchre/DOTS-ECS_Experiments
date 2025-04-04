using Unity.Entities;
using UnityEngine;

namespace ECS.TargetAndSeekerDemo
{
	public class SeekerAuthoring : MonoBehaviour
	{
        public float speed;
        public float minTimer;
        public float maxTimer;
    }

    class SeekerBaker : Baker<SeekerAuthoring>
    {
        public override void Bake(SeekerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            Seeker newSeeker = new Seeker();
            AddComponent(entity, newSeeker);

            Movement newMovement = new Movement()
            {
                Speed = authoring.speed,
                Direction = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized, // Default value is the same for every entity spawned
                Timer = Random.Range(authoring.minTimer, authoring.maxTimer), // Default value is the same for every entity spawned
                MinTimer = authoring.minTimer,
                MaxTimer = authoring.maxTimer,
            };
            AddComponent(entity, newMovement);

            RandomData random = new RandomData()
            {
                Value = Unity.Mathematics.Random.CreateFromIndex((uint)Random.Range(1, uint.MaxValue)) // Default value is the same for every entity when spawned (but different if entity is baked in the hierarchy)
            };
            AddComponent(entity, random);

            AddBuffer<LastPositions>(entity);
        }
    }
}