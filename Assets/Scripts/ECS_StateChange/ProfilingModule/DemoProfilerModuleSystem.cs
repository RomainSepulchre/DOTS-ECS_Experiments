using ECS.StateChange.StateChangeProfilerModule;
using Project.Utilities;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Playables;

namespace ECS.StateChange.StateChangeProfilerModule
{
    [UpdateInGroup(typeof(PresentationSystemGroup))] // Update after simulation finished
    public partial struct DemoProfilerModuleSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Create entity to hold singleton component with profiler counter data
            Entity entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponent<StateChangeDemoProfilerModule.FrameData>(entity);

            state.RequireForUpdate<Exec_ECS_StateChange>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // Cast as ref to allow the perf counter to flush data on end of frame doesn't seem to work
            ref var frameData = ref SystemAPI.GetSingletonRW<StateChangeDemoProfilerModule.FrameData>().ValueRW; // SetState counter is set to 0 when no operation is done during the frame

            // Update profiler counter data from component data
            StateChangeDemoProfilerModule.StateChangePerf = frameData.SetStatePerf;
            StateChangeDemoProfilerModule.SpinPerf = frameData.SpinPerf;
            
        }
    }
}
