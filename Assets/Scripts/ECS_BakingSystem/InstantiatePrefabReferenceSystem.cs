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
            // When using OnStartRunning(), Prefab loading takes almost 5/6s which is way more than when using OnUpdate in a dedicated system
            //

            // Only run this when LoadPrefabRefInSeparateSystem is disabled
            Config config = SystemAPI.GetSingleton<Config>();
            if (config.LoadPrefabRefInSeparateSystem) return;

            // Query the entities with an EntityPrefabReference to load
            EntityQuery query = SystemAPI.QueryBuilder().WithAll<PrefabReference>().WithNone<PrefabLoadResult>().Build();

            // When adding RequestEntityPrefabLoaded we set its prefab value with the EntityPrefabReference to load
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

        public void OnStopRunning(ref SystemState state)
        {          
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Only run the rest of the system when at least one prefab has been loaded (Ideally I should use state.RequireForUpdate<PrefabLoadResult>() but that would block loading in OnStartRunning())
            EntityQuery prefabLoadedQuery = SystemAPI.QueryBuilder().WithAll<PrefabReference, PrefabLoadResult>().Build();
            if (prefabLoadedQuery.CalculateEntityCount() <= 0) return;

            // Timer for spawn interval
            timer -= SystemAPI.Time.DeltaTime;
            if(timer > 0)
            {
                return;
            }
            Config config = SystemAPI.GetSingleton<Config>();
            timer = config.SpawnInterval;

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Instantiate every loaded prefab
            foreach (var (prefab, entity) in SystemAPI.Query<RefRO<PrefabLoadResult>>().WithAll<PrefabReference>().WithEntityAccess())
            {
                var instance = ecb.Instantiate(prefab.ValueRO.PrefabRoot);
                ecb.SetComponent(instance, LocalTransform.FromPosition(new float3(0, (float)SystemAPI.Time.ElapsedTime, 0)));

                if(config.RemoveLoadComponentsAfterInstantiation)
                {
                    // Remove both RequestEntityPrefabLoaded and PrefabLoadResult to load and instantiate the prefab only once
                    ecb.RemoveComponent<RequestEntityPrefabLoaded>(entity);
                    ecb.RemoveComponent<PrefabLoadResult>(entity);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
