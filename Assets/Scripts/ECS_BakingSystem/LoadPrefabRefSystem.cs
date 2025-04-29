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
        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //
            // Prefab loading is almost instantaneous when doing this instead of using OnStartRunning()
            //

            // Only run the update once
            state.Enabled = false;

            var query = SystemAPI.QueryBuilder().WithAll<PrefabReference>().WithNone<PrefabLoadResult>().Build();
            Debug.Log($"Number of PrefabReference: {query.CalculateEntityCount()}");

            // I have an error telling me about a null entity when I try to do this but I can't find why ? Maybe RequestEntityPrefabLoaded is not initialized correctly ?
            // -> the issue happen after RequestEntityPrefabLoaded has been added, in the WeakAssetReferenceLoadingSystem when trying to load the prefab, I need to find what happen after RequestEntityPrefabLoaded is added
            // -> EntityPrefabReference send to load asset has not the correct data, Do I need to set RequestEntityPrefabLoaded with PrefabReference ? If yes why isn't this shown in the doc ?
            //      -> Why do the doc seems to insinuate everything is setup automatically when adding the RequestEntityPrefabLoaded component ?
            //state.EntityManager.AddComponent<RequestEntityPrefabLoaded>(query);

            // Manually set RequestEntityPrefabLoaded with PrefabReference
            // -> Fail to load prefab scene (Loading Entity Scene failed because the entity header file couldn't be resolved.). Same error that happened suddenly when adding RequestEntityPrefabLoaded at baking
            NativeArray<PrefabReference> prefabReferences = query.ToComponentDataArray<PrefabReference>(Allocator.Temp);
            NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < entities.Count(); i++)
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
