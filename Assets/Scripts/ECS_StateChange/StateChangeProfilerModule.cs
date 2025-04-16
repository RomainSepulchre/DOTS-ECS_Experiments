using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEngine;

namespace ECS.StateChange
{
    [ProfilerModuleMetadata("State changes")]
    public class StateChangeProfilerModule : ProfilerModule
    {
        static readonly ProfilerCounterDescriptor[] k_Counters = new ProfilerCounterDescriptor[]
        {
            new ProfilerCounterDescriptor(GameStats.StateChangeCountName, GameStats.TestCategory)
        };


        public StateChangeProfilerModule() : base( k_Counters )
        {

        }
    }

    public static class GameStats
    {
        public static readonly ProfilerCategory TestCategory = ProfilerCategory.Scripts;

        public const string StateChangeCountName = "State Change";

        public static readonly ProfilerCounterValue<int> StateChangeCount =
            new ProfilerCounterValue<int>(TestCategory, StateChangeCountName, ProfilerMarkerDataUnit.Count);
    }
}
