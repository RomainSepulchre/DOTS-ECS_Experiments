using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Scenes;
using UnityEngine;

namespace ECS.BakingSystem
{
    public class EntityPrefabRefAuthoring : MonoBehaviour
    {
        public GameObject prefab;
    }

    class EntityPrefabRefBaker : Baker<EntityPrefabRefAuthoring>
    {
        public override void Bake(EntityPrefabRefAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            // Create and set EntityPrefabReference
            EntityPrefabReference prefabRef = new EntityPrefabReference(authoring.prefab);
            AddComponent(entity, new PrefabReference()
            {
                Prefab = prefabRef,
            });
        }
    }

    public struct PrefabReference : IComponentData
    {
        public EntityPrefabReference Prefab;
    }
}
