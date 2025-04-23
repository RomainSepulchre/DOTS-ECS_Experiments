using Project.Utilities;
using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace ECS.StateChange
{
    public partial struct SwapMaterialSystem : ISystem
    {
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
            ComponentLookup<MaterialMeshInfo> matMeshInfoLookup = SystemAPI.GetComponentLookup<MaterialMeshInfo>();


            foreach (var (meshInfo, swapMat) in SystemAPI.Query<RefRW<MaterialMeshInfo>, RefRW<SwapMaterial>>())
            {
                if (config.ChangeMaterial != swapMat.ValueRO.MaterialChanged)
                {
                    if (config.ChangeMaterial)
                    {
                        // Set to new material
                        meshInfo.ValueRW = matMeshInfoLookup[config.ObjWithNewMat];
                    }
                    else
                    {
                        // Use initial material
                        meshInfo.ValueRW = matMeshInfoLookup[config.ObjWithInitialMat];
                    }
                    swapMat.ValueRW.MaterialChanged = config.ChangeMaterial;
                }
            }
        }
    }
}
