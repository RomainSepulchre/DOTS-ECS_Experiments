using Unity.Entities;
using UnityEngine;

namespace ECS.BakingSystem
{
    public class ConfigAuthoring : MonoBehaviour
    {
        public float spawnInterval;
        public bool loadPrefabRefInSeparateSystem;
        public bool removeLoadComponentsAfterInstantiation;
    }

    class ConfigBaker : Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Config
            {
                SpawnInterval = authoring.spawnInterval,
                LoadPrefabRefInSeparateSystem = authoring.loadPrefabRefInSeparateSystem,
                RemoveLoadComponentsAfterInstantiation = authoring.removeLoadComponentsAfterInstantiation
            });
        }
    }

    public struct Config : IComponentData
    {
        public float SpawnInterval;
        public bool LoadPrefabRefInSeparateSystem;
        public bool RemoveLoadComponentsAfterInstantiation;
    }
}
