using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace ECS.BakingSystem
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)] 
    public partial struct FilterComponentBakingSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var bakingType in SystemAPI.Query<RefRO<BakingTypeComponent>>())
            {
                Debug.Log($"Baking type component: {bakingType.ValueRO.Value}");
            }

            foreach (var tempBakingType in SystemAPI.Query<RefRO<TempBakingTypeComponent>>())
            {
                Debug.Log($"Temporary baking type component: {tempBakingType.ValueRO.Value}");
            }

            // Observations:
            // - Renaming gameobject call bakingType but not Temporary baking type
            // - Chaging a value in the authoring component fields calls both
            // - When baking is triggered but nothing changes on FilterComponentAuthoring temporary is not called but baking type is called
            // => Temporary only exist if the baker adding temporary component has run during this baking pass.
        }
    }
}
