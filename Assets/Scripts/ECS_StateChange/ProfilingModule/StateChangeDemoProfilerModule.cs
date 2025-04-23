using Unity.Entities;
using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEngine;

namespace ECS.StateChange.StateChangeProfilerModule
{
    [ProfilerModuleMetadata("StateChangesDemo")]
    public class StateChangeDemoProfilerModule : ProfilerModule
    {
        // Performance category
        public static readonly ProfilerCategory StateChangeCategory = ProfilerCategory.Scripts;

        //
        // Define all the counter value we want to track
        //
        static readonly string SetStatePerfCounterName = "SetStateSystem";

        static readonly ProfilerCounterValue<long> SetStatePerfCounterValue = new ProfilerCounterValue<long>(
            StateChangeCategory,
            SetStatePerfCounterName,
            ProfilerMarkerDataUnit.TimeNanoseconds,
            ProfilerCounterOptions.FlushOnEndOfFrame);

        internal static long StateChangePerf
        {
            set => SetStatePerfCounterValue.Value = value;
        }

        static readonly string SpinPerfCounterName = "SpinSystem";

        static readonly ProfilerCounterValue<long> SpinPerfCounterValue = new ProfilerCounterValue<long>(
            StateChangeCategory,
            SpinPerfCounterName,
            ProfilerMarkerDataUnit.TimeNanoseconds,
            ProfilerCounterOptions.FlushOnEndOfFrame);

        internal static long SpinPerf
        {
            set => SpinPerfCounterValue.Value = value;
        }

        // All the counter to show in the module must be add in a ProfilerCounterDescriptor Array passed to the constructor through base
        static readonly ProfilerCounterDescriptor[] k_Counters = new ProfilerCounterDescriptor[]
        {
            new ProfilerCounterDescriptor(SetStatePerfCounterName, StateChangeCategory),
            new ProfilerCounterDescriptor(SpinPerfCounterName, StateChangeCategory)
        };

        // Base allow to pass counters and set some of the module parameters like the chartType for example
        public StateChangeDemoProfilerModule() : base(k_Counters, ProfilerModuleChartType.StackedTimeArea)
        {
        }

        // Singleton component to update data from ECS systems
        public struct FrameData : IComponentData
        {
            public long SetStatePerf;
            public long SpinPerf;
        }
    }
}
