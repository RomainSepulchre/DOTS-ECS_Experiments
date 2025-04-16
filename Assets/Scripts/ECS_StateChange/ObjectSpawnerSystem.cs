using Project.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace ECS.StateChange
{
    public partial struct ObjectSpawnerSystem : ISystem
    {
        Config priorConfig; // Cache of the previous config to compare config between frames

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<Exec_ECS_StateChange>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Config config = SystemAPI.GetSingleton<Config>();

            if (ConfigEquals(priorConfig, config))
            {
                return;
            }      
            priorConfig = config;

            // Destroy existing objects
            EntityQuery objectsQuery = SystemAPI.QueryBuilder().WithAll<HDRPMaterialPropertyBaseColor>().Build();
            state.EntityManager.DestroyEntity(objectsQuery);

            // Instantiate objects to fit new config
            NativeArray<Entity> entities = state.EntityManager.Instantiate(config.Prefab, (int)(config.Size * config.Size), Allocator.Temp);

            float center = (config.Size - 1) / 2f;

            int index = 0;
            foreach (var transform in SystemAPI.Query<RefRW<LocalTransform>>())
            {
                transform.ValueRW.Scale = 1;
                transform.ValueRW.Position.x = (index % config.Size - center) * 1.5f;
                transform.ValueRW.Position.z = (index / config.Size - center) * 1.5f;
                index++;
            }

            EntityQuery spinQuery = SystemAPI.QueryBuilder().WithAll<Spin>().Build();

            if(config.Mode == Mode.Value)
            {
                state.EntityManager.AddComponent<Spin>(objectsQuery);
            }
            else if(config.Mode == Mode.EnableableComponent)
            {
                state.EntityManager.AddComponent<Spin>(objectsQuery);
                state.EntityManager.SetComponentEnabled<Spin>(spinQuery, false);
            }
        }

        private bool ConfigEquals(Config a, Config b)
        {
            bool samePrefab = a.Prefab == b.Prefab;
            bool sameSize = a.Size == b.Size;
            bool sameRadius = a.Radius == b.Radius;
            bool sameMode = a.Mode == b.Mode;

            return samePrefab && sameSize && sameRadius && sameMode;
        }
    }
}
