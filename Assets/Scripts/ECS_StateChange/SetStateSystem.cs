using Ionic.Zlib;
using Project.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Profiling.LowLevel.Unsafe;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace ECS.StateChange
{
    public partial struct SetStateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MouseHit>();
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<Exec_ECS_StateChange>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Config config = SystemAPI.GetSingleton<Config>();
            MouseHit hit = SystemAPI.GetSingleton<MouseHit>();

            if(hit.HitChanged == false)
            {
                return;
            }

            float radiusSQ = config.Radius * config.Radius;
            var ecbSystem = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();

            long before = ProfilerUnsafeUtility.Timestamp;

            if(config.Mode == Mode.Value)
            {
                SetValueJob setValueJob = new SetValueJob()
                {
                    RadiusSQ = radiusSQ,
                    HitPosition = hit.HitPosition
                };
                state.Dependency = setValueJob.ScheduleParallel(state.Dependency);
            }
            else if(config.Mode == Mode.StructuralChange)
            {
                AddSpinJob addSpinJob = new AddSpinJob()
                {
                    RadiusSQ = radiusSQ,
                    HitPosition = hit.HitPosition,
                    ECB = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
                };
                state.Dependency = addSpinJob.ScheduleParallel(state.Dependency);

                RemoveSpinJob removeSpinJob = new RemoveSpinJob()
                {
                    RadiusSQ = radiusSQ,
                    HitPosition = hit.HitPosition,
                    ECB = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
                };
                state.Dependency = removeSpinJob.ScheduleParallel(state.Dependency);
            }
            else if(config.Mode == Mode.EnableableComponent)
            {
                EnableSpinJob enableSpinJob = new EnableSpinJob()
                {
                    RadiusSQ = radiusSQ,
                    HitPosition = hit.HitPosition,
                    ECB = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
                };
                state.Dependency = enableSpinJob.ScheduleParallel(state.Dependency);

                DisableSpinJob disableSpinJob = new DisableSpinJob()
                {
                    RadiusSQ = radiusSQ,
                    HitPosition = hit.HitPosition,
                };
                state.Dependency = disableSpinJob.ScheduleParallel(state.Dependency);
            }

            state.Dependency.Complete();

            long after = ProfilerUnsafeUtility.Timestamp;

            var conversionRatio = ProfilerUnsafeUtility.TimestampToNanosecondsConversionRatio;
            long elapsed = (after - before) * conversionRatio.Numerator / conversionRatio.Denominator;
            //SystemAPI.GetSingleton<StateChangeProfilerModule.FrameData>().ValueRW.SetStatePerf = elapsed;
        }
    }

    [BurstCompile]
    public partial struct SetValueJob : IJobEntity
    {
        [ReadOnly] public float RadiusSQ;
        [ReadOnly] public float3 HitPosition;

        public void Execute(ref HDRPMaterialPropertyBaseColor color, ref Spin objSpin, in LocalTransform transform)
        {
            if (math.distancesq(transform.Position, HitPosition) <= RadiusSQ)
            {
                color.Value = (Vector4)Color.red;
                objSpin.IsSpinning = true;
            }
            else
            {
                color.Value = (Vector4)Color.white;
                objSpin.IsSpinning = false;
            }
        }
    }

    [WithNone(typeof(Spin))]
    [BurstCompile]
    public partial struct AddSpinJob : IJobEntity
    {
        [ReadOnly] public float RadiusSQ;
        [ReadOnly] public float3 HitPosition;
        public EntityCommandBuffer.ParallelWriter ECB;

        public void Execute(Entity entity, ref HDRPMaterialPropertyBaseColor color, in LocalTransform transform, [ChunkIndexInQuery] int chunkIndex)
        {
            if (math.distancesq(transform.Position, HitPosition) <= RadiusSQ)
            {
                color.Value = (Vector4)Color.red;
                ECB.AddComponent<Spin>(chunkIndex, entity);
            }
        }
    }

    [WithAll(typeof(Spin))]
    [BurstCompile]
    public partial struct RemoveSpinJob : IJobEntity
    {
        [ReadOnly] public float RadiusSQ;
        [ReadOnly] public float3 HitPosition;
        public EntityCommandBuffer.ParallelWriter ECB;

        public void Execute(Entity entity, ref HDRPMaterialPropertyBaseColor color, in LocalTransform transform, [ChunkIndexInQuery] int chunkIndex)
        {
            if (math.distancesq(transform.Position, HitPosition) > RadiusSQ)
            {
                color.Value = (Vector4)Color.white;
                ECB.RemoveComponent<Spin>(chunkIndex, entity);
            }
        }
    }

    [WithNone(typeof(Spin))]
    [BurstCompile]
    public partial struct EnableSpinJob : IJobEntity
    {
        [ReadOnly] public float RadiusSQ;
        [ReadOnly] public float3 HitPosition;
        public EntityCommandBuffer.ParallelWriter ECB;

        public void Execute(Entity entity, ref HDRPMaterialPropertyBaseColor color, in LocalTransform transform, [ChunkIndexInQuery] int chunkIndex)
        {
            if (math.distancesq(transform.Position, HitPosition) <= RadiusSQ)
            {
                color.Value = (Vector4)Color.red;
                ECB.SetComponentEnabled<Spin>(chunkIndex, entity, true);
            }
        }
    }

    [WithAll(typeof(Spin))]
    [BurstCompile]
    public partial struct DisableSpinJob : IJobEntity
    {
        [ReadOnly] public float RadiusSQ;
        [ReadOnly] public float3 HitPosition;

        public void Execute(Entity entity, ref HDRPMaterialPropertyBaseColor color, in LocalTransform transform, EnabledRefRW<Spin> spinEnabled)
        {
            if (math.distancesq(transform.Position, HitPosition) > RadiusSQ)
            {
                color.Value = (Vector4)Color.white;
                spinEnabled.ValueRW = false;
            }
        }
    }

}
