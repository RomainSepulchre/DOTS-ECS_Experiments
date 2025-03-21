using Unity.Entities;
using UnityEngine;

namespace ECS.EnemyRunAwayDemo
{
    public class EnemySpawner : IComponentData, IEnableableComponent // 
    {
        public Entity EnemyToSpawn;
        public int SpawnAmount;
        public float SpawnAreaXLimit;
        public float SpawnAreaYLimit;
    } 
}
