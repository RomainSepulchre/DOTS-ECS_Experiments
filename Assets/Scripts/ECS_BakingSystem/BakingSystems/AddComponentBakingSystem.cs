using Unity.Entities;
using UnityEngine;

namespace ECS.BakingSystem
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)] // this attribute define the system as a baking system
    public partial struct AddComponentBakingSystem : ISystem
    {
        public void OnUpdate(ref SystemState state) // Update is called at every single baking pass in bakign systems
        {
            EntityQuery cubeQuery = SystemAPI.QueryBuilder().WithAll<Cube>().WithNone<AddedWithBakingSystem>().Build();
            state.EntityManager.AddComponent<AddedWithBakingSystem>(cubeQuery); // Add the AddedWithBakingSystem component on every entity with the cube component

            // We also need to add a part to clean AddedWithBakingSystem component when the Cube component is removed otherwise AddedWithBakingSystem remains

            EntityQuery cleanQuery = SystemAPI.QueryBuilder().WithAll<AddedWithBakingSystem>().WithNone<Cube>().Build();
            state.EntityManager.RemoveComponent<AddedWithBakingSystem>(cleanQuery); // Remove the AddedWithBakingSystem component on every entity without the cube component
        }
    }
}
