using Unity.Entities;
using UnityEngine;

namespace ECS.StateChange
{
    public struct SwapMaterial : IComponentData
    {
        public bool MaterialChanged;
    }

    public class SwapMaterialAuthoring : MonoBehaviour
    {
    }

    class SwapMaterialBaker : Baker<SwapMaterialAuthoring>
    {
        public override void Bake(SwapMaterialAuthoring authoring)
        {
            // ? Why objects moved at weird position (-750, 20, 750) and not having scale change with TransformUsageFlags.Dynamic ?
            // -> It's simply that ObjectSpawnerSystem process this entity since it query every object with a LocalTransform and TransformUsageFlags.Dynamic add a localTransform to this entity
            //    I could simply fix this by excluding SwapMaterial component from the ObjectSpawnerSystem query but anyway I don't need to move this cube
            Entity entity = GetEntity(TransformUsageFlags.Renderable);

            SwapMaterial newSwapMaterial = new SwapMaterial()
            {
                MaterialChanged = false,
            };
            AddComponent(entity, newSwapMaterial);
        }
    }
}
