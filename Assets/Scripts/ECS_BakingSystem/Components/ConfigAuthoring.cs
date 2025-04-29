using Unity.Entities;
using UnityEngine;

namespace ECS.BakingSystem
{
    public class ConfigAuthoring : MonoBehaviour
    {
        public float spawnInterval;
    }

    class ConfigBaker : Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Config { SpawnInterval = authoring.spawnInterval });
        }
    }

    public struct Config : IComponentData
    {
        public float SpawnInterval;
    }
}
