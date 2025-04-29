using Project.Utilities;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;

namespace ECS.BakingSystem
{
    public partial struct LoadPrefabRefSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<Exec_ECS_Baking>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //
            // When using a dedicated system OnUpdate(), Prefab loading is almost instantaneous contrary to when we use OnStartRunning() in the instantiate system
            //

            // Only run the update once
            state.Enabled = false;

            // Only run this when LoadPrefabRefInSeparateSystem is enabled
            Config config = SystemAPI.GetSingleton<Config>();
            if (!config.LoadPrefabRefInSeparateSystem) return;  

            // Query the entities with an EntityPrefabReference to load
            EntityQuery query = SystemAPI.QueryBuilder().WithAll<PrefabReference>().WithNone<PrefabLoadResult>().Build();

            //  When adding RequestEntityPrefabLoaded we set its prefab value with the EntityPrefabReference to load
            NativeArray<PrefabReference> prefabReferences = query.ToComponentDataArray<PrefabReference>(Allocator.Temp);
            NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++) // TODO: Burst compile (use length ?)
            {
                RequestEntityPrefabLoaded requestEntityPrefabLoaded = new RequestEntityPrefabLoaded()
                {
                    Prefab = prefabReferences[i].Prefab
                };
                state.EntityManager.AddComponentData(entities[i], requestEntityPrefabLoaded);
            }
        }
    }
}
