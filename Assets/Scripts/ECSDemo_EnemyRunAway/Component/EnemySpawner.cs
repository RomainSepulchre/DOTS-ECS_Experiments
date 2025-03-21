using Unity.Entities;

namespace ECS.EnemyRunAwayDemo
{
    public struct EnemySpawner : IComponentData, IEnableableComponent // Enableable component or disable system/remove component ?
    {
        public Entity EnemyToSpawn;
        public Entity Player;
        public int SpawnAmount;
        public float SpawnAreaXLimit;
        public float SpawnAreaYLimit;
    } 
}
