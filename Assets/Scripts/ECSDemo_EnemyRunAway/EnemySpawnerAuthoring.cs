using Unity.Entities;
using UnityEngine;

namespace ECS.EnemyRunAwayDemo
{
    public class EnemySpawnerAuthoring : MonoBehaviour
    {
        public GameObject enemyToSpawn;
        public GameObject player;
        public int spawnAmount;
        public float spawnAreaXLimit;
        public float spawnAreaYLimit;
    }

    public class EnemySpawnerBaker : Baker<EnemySpawnerAuthoring>
    {
        public override void Bake(EnemySpawnerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            EnemySpawner newSpawner = new EnemySpawner()
            {
                EnemyToSpawn = GetEntity(authoring.enemyToSpawn, TransformUsageFlags.Dynamic),
                Player = GetEntity(authoring.player, TransformUsageFlags.Dynamic),
                SpawnAmount = authoring.spawnAmount,
                SpawnAreaXLimit = authoring.spawnAreaXLimit,
                SpawnAreaYLimit = authoring.spawnAreaYLimit
            };

            AddComponent(entity, newSpawner);
        }
    }
}