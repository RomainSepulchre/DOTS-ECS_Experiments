
using Unity.Entities;
using Unity.Mathematics;

namespace ECS.ECSExperiments
{
    // Entities and component: https://www.youtube.com/watch?v=jzCEzNoztzM

    public struct Spawner : IComponentData
    {
        public Entity Prefab;
        public float3 SpawnPosition;
        public float NextSpawnTime;
        public float SpawnRate;
        public int SpawnCount;
        public bool SpawnAllAtFirstFrame;
    }
}
