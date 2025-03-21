using Unity.Entities;
using UnityEngine;

namespace ECS.EnemyRunAwayDemo
{
    public class EnemySpawnerAuthoring : MonoBehaviour
    {
        public Prefab enemyToSpawn;
        public int spawnAmount;
        public float spawnAreaXLimit;
        public float spawnAreaYLimit;
    }

    public class EnemySpawnerBaker : Baker<EnemySpawnerAuthoring>
    {
        public override void Bake(EnemySpawnerAuthoring authoring)
        {
            throw new System.NotImplementedException();
        }
    }
}