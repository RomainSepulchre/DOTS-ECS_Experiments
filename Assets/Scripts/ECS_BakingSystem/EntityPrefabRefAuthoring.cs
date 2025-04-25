using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Scenes;
using UnityEngine;

namespace ECS.BakingSystem
{
    public class EntityPrefabRefAuthoring : MonoBehaviour
    {
        public GameObject go;
    }

    class EntityPrefabRefBaker : Baker<EntityPrefabRefAuthoring>
    {
        public override void Bake(EntityPrefabRefAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
     
            //Entity prefab = GetEntity(authoring.go, TransformUsageFlags.None);

            // Prefab ref
            EntityPrefabReference prefabRef = new EntityPrefabReference(authoring.go);
            AddComponent(entity, new PrefabReference() { Value = prefabRef });

            // When doing this RequestEntityPrefabLoaded seems to work without error but this seems bad to do -> it worked until the subscene becomae impossible to reload
            //AddComponent<RequestEntityPrefabLoaded>(entity, new RequestEntityPrefabLoaded() { Prefab = prefabRef } );
        }
    }

    public struct PrefabReference : IComponentData
    {
        public EntityPrefabReference Value;
    }
}
