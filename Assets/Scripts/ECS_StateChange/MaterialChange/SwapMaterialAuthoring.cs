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
            // TODO: Why objects moved at weird position (-750, 20, 750) and not having scale change with dynamic ?
            //Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            Entity entity = GetEntity(TransformUsageFlags.Renderable);

            SwapMaterial newSwapMaterial = new SwapMaterial()
            {
                MaterialChanged = false,
            };
            AddComponent(entity, newSwapMaterial);
        }
    }
}
