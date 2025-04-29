using Project.Utilities;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Scenes;
using Unity.Transforms;
using UnityEngine;

namespace ECS.BakingSystem
{
    // TODO: I miss too much information on RequestEntityPrefabLoaded and prefab scene loading to understand where the issue is, Check back later
    // The doc seems to miss lots of information on how to use EntityPrefabReference and what is actually happening with RequestEntityPrefabLoaded
    public partial struct InstantiatePrefabReferenceSystem : ISystem, ISystemStartStop
    {
        EntityQuery prefabRefQuery;
        float timer;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            prefabRefQuery = SystemAPI.QueryBuilder().WithAll<PrefabReference>().Build();

            state.RequireForUpdate<Config>();
            state.RequireForUpdate<Exec_ECS_Baking>();
            state.RequireForUpdate(prefabRefQuery);
        }

        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            //
            // Prefab loading takes almost 5/6s when doing this instead of using the OnUpdate of a dedicated system
            //

            //var query = SystemAPI.QueryBuilder().WithAll<PrefabReference>().WithNone<PrefabLoadResult>().Build();
            //Debug.Log($"Number of PrefabReference: {query.CalculateEntityCount()}");

            //// I have an error telling me about a null entity when I try to do this but I can't find why ? Maybe RequestEntityPrefabLoaded is not initialized correctly ?
            //// -> the issue happen after RequestEntityPrefabLoaded has been added, in the WeakAssetReferenceLoadingSystem when trying to load the prefab, I need to find what happen after RequestEntityPrefabLoaded is added
            //// -> EntityPrefabReference send to load asset has not the correct data, Do I need to set RequestEntityPrefabLoaded with PrefabReference ? If yes why isn't this shown in the doc ?
            ////      -> Why do the doc seems to insinuate everything is setup automatically when adding the RequestEntityPrefabLoaded component ?
            ////state.EntityManager.AddComponent<RequestEntityPrefabLoaded>(query);

            //// Manually set RequestEntityPrefabLoaded with PrefabReference
            //// -> Fail to load prefab scene (Loading Entity Scene failed because the entity header file couldn't be resolved.). Same error that happened suddenly when adding RequestEntityPrefabLoaded at baking
            //NativeArray<PrefabReference> prefabReferences = query.ToComponentDataArray<PrefabReference>(Allocator.Temp);
            //NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);

            //for (int i = 0; i < entities.Count(); i++)
            //{
            //    RequestEntityPrefabLoaded requestEntityPrefabLoaded = new RequestEntityPrefabLoaded()
            //    {
            //        Prefab = prefabReferences[i].Value
            //    };
            //    state.EntityManager.AddComponentData(entities[i], requestEntityPrefabLoaded);
            //}
        }

        public void OnStopRunning(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            timer -= SystemAPI.Time.DeltaTime;

            if(timer > 0)
            {
                return;
            }

            Config config = SystemAPI.GetSingleton<Config>();
            timer = config.SpawnInterval;

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Try to load prefab from PrefabLoadResult and instantiate it
            foreach (var (prefab, entity) in SystemAPI.Query<RefRO<PrefabLoadResult>>().WithEntityAccess())
            {
                var instance = ecb.Instantiate(prefab.ValueRO.PrefabRoot);
                ecb.SetComponent(instance, LocalTransform.FromPosition(new float3(0, (float)SystemAPI.Time.ElapsedTime, 0)));

                // Remove both RequestEntityPrefabLoaded and PrefabLoadResult to prevent
                // the prefab being loaded and instantiated multiple times, respectively
                // TODO: WHY DO YOU CONTINUE TO INSTANTIATE AFTER THIS ?
                ecb.RemoveComponent<RequestEntityPrefabLoaded>(entity);
                ecb.RemoveComponent<PrefabLoadResult>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
