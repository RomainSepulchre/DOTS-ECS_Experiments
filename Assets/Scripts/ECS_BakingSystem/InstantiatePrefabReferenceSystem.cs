using Project.Utilities;
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

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Exec_ECS_Baking>();
        }

        public void OnStartRunning(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAll<PrefabReference>().WithNone<PrefabLoadResult>().Build();
            Debug.Log($"Number of PrefabReference: {query.CalculateEntityCount()}");

            // I have an error telling me about a null entity when I try to do this but I can't find why ? Maybe RequestEntityPrefabLoaded is not initialized correctly ?
            //state.EntityManager.AddComponent<RequestEntityPrefabLoaded>(query);
        }

        public void OnStopRunning(ref SystemState state)
        {
            
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            //var query = SystemAPI.QueryBuilder().WithAll<PrefabLoadResult>().Build();
            //Debug.Log($"Number of PrefabLoadResult: {query.CalculateEntityCount()}");

            // Try to load prefab from PrefabLoadResult and instantiate it
            foreach (var (prefab, entity) in SystemAPI.Query<RefRO<PrefabLoadResult>>().WithEntityAccess())
            {

                var instance = ecb.Instantiate(prefab.ValueRO.PrefabRoot);
                Debug.Log($"Instantiate...");
                ecb.SetComponent(instance, LocalTransform.FromPosition(new float3(0, (float)SystemAPI.Time.ElapsedTime, 0 )));

                // Remove both RequestEntityPrefabLoaded and PrefabLoadResult to prevent
                // the prefab being loaded and instantiated multiple times, respectively
                ecb.RemoveComponent<RequestEntityPrefabLoaded>(entity);
                ecb.RemoveComponent<PrefabLoadResult>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
