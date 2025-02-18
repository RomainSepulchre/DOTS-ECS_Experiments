using UnityEngine;
using Unity.Entities;

namespace ECS.ECSExperiments
{
    public class SpawnerAuthoring : MonoBehaviour
    {
        public GameObject Prefab;
        public float SpawnRate;
        public int SpawnCount;
        public bool SpawnAllAtFirstFrame;
        public bool UseJobs;
    }

    class SpawnerBaker : Baker<SpawnerAuthoring>
    {
        public override void Bake(SpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            Spawner newSpawner = new Spawner
            {
                Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
                SpawnPosition = authoring.transform.position,
                NextSpawnTime = 0.0f,
                SpawnRate = authoring.SpawnRate,
                SpawnCount = authoring.SpawnCount,
                SpawnAllAtFirstFrame = authoring.SpawnAllAtFirstFrame
            };

            AddComponent(entity, newSpawner);

            if(authoring.UseJobs)
            {
                SpawnerUseJobs newJobsTag = new SpawnerUseJobs();
                AddComponent(entity, newJobsTag);
            }
        }
    } 
}

