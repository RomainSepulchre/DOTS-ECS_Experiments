using Project.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Profiling.LowLevel.Unsafe;
using Unity.Transforms;
using UnityEngine;

namespace ECS.StateChange
{
    public partial struct SpinSystem : ISystem
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

            long before = ProfilerUnsafeUtility.Timestamp;
            if(config.Mode == Mode.Value)
            {
                SpinByValueJob spinByValueJob = new SpinByValueJob()
                {
                    Offset = quaternion.RotateY(SystemAPI.Time.DeltaTime * math.PI)
                };
                state.Dependency = spinByValueJob.ScheduleParallel(state.Dependency);
            }
            else
            {
                SpinJob spinJob = new SpinJob()
                {
                    Offset = quaternion.RotateY(SystemAPI.Time.DeltaTime * math.PI)
                };
                state.Dependency = spinJob.ScheduleParallel(state.Dependency);
            }
            state.Dependency.Complete();
            long after = ProfilerUnsafeUtility.Timestamp;

            var conversionRatio = ProfilerUnsafeUtility.TimestampToNanosecondsConversionRatio;
            long elapsed = (after - before) * conversionRatio.Numerator / conversionRatio.Denominator;
            //SystemAPI.GetSingleton<StateChangeProfilerModule.FrameData>().ValueRW.SpinPerf = elapsed;
        }
    }

    [BurstCompile]
    public partial struct SpinByValueJob : IJobEntity
    {
        [ReadOnly] public quaternion Offset;

        public void Execute(ref LocalTransform transform, in Spin spin)
        {
            if(spin.IsSpinning)
            {
                transform = transform.Rotate(Offset);
            }
        }
    }

    [WithAll(typeof(Spin))]
    [BurstCompile]
    public partial struct SpinJob : IJobEntity
    {
        [ReadOnly] public quaternion Offset;

        public void Execute(ref LocalTransform transform)
        {
            transform = transform.Rotate(Offset);
        }
    }
}
