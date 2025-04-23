using Unity.Entities;
using UnityEngine;

namespace ECS.BakingSystem
{
    public class FilterComponentAuthoring : MonoBehaviour
    {
        public float valueBakingType;
        public float valueTempBakingType;
    }

    class FilterComponentBaker : Baker<FilterComponentAuthoring>
    {
        public override void Bake(FilterComponentAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            TempBakingTypeComponent tempBakingTypeComponent = new TempBakingTypeComponent()
            {
                Value = authoring.valueTempBakingType,
            };
            AddComponent(entity, tempBakingTypeComponent);

            BakingTypeComponent bakingTypeComponent = new BakingTypeComponent()
            {
                Value = authoring.valueBakingType,
            };
            AddComponent(entity, bakingTypeComponent);
        }
    }

    [TemporaryBakingType]
    public struct TempBakingTypeComponent : IComponentData
    {
        public float Value;
    }

    [BakingType]
    public struct BakingTypeComponent : IComponentData
    {
        public float Value;
    }

}
