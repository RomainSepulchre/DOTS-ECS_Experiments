using Unity.Entities;
using UnityEngine;

namespace ECS.TargetAndSeekerDemo
{
	public class SpawnerAuthoring : MonoBehaviour
	{
        public GameObject objToSpawn;
        public int spawnAmount;
        public float xSpawnAreaLimit;
        public float zSpawnAreaLimit;
    }

    class SpawnerBaker : Baker<SpawnerAuthoring>
    {
        public override void Bake(SpawnerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            Spawner newSpawner = new Spawner()
            {
                EntityToSpawn = GetEntity(authoring.objToSpawn, TransformUsageFlags.Dynamic),
                SpawnAmount = authoring.spawnAmount,
                XSpawnAreaLimit = authoring.xSpawnAreaLimit,
                ZSpawnAreaLimit = authoring.zSpawnAreaLimit,
                RandomSeed = (uint)UnityEngine.Random.Range(1, uint.MaxValue)
            };
            AddComponent(entity, newSpawner);
        }
    }
}
