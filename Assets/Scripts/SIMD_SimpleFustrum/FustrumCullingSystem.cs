using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace Burst.SIMD.SimpleFustrum
{
    public partial struct FustrumCullingSystem : ISystem
    {
        private NativeArray<float4> _planes;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _planes = new NativeArray<float4>(6, Allocator.Persistent);
        }

        // OnUpdate can't be Burst Compiled because we access a managed type (Camera) in FustrumCullingHelper.UpdateFustrumPlanes()
        // However once it's done we can call another function (DoCulling()) that is Burst-Compiled
        public void OnUpdate(ref SystemState state)
        {
            FustrumCullingHelper.UpdateFustrumPlanes(ref _planes);
            DoCulling(ref state);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            _planes.Dispose();
        }

        // TODO: Add a spawner system and test with huge number of sphere entities 
        [BurstCompile]
        public void DoCulling(ref SystemState state)
        {
            // Simple culling
            //CullJob cullingJob = new CullJob
            //{
            //    Planes = _planes
            //};
            //state.Dependency = cullingJob.ScheduleParallel(state.Dependency);
            //state.Dependency.Complete(); // why do they complete the job immediately in the example ?

            // Culling without branching in the code
            CullJobNoBranch cullJobNoBranch = new CullJobNoBranch
            {
                Planes = _planes
            };
            state.Dependency = cullJobNoBranch.ScheduleParallel(state.Dependency);
            state.Dependency.Complete(); // why do they complete the job immediately in the example ?
        }
    }

    /// <summary>
    /// Simple version of the culling job.
    /// </summary>
    [BurstCompile(OptimizeFor = OptimizeFor.Performance)] // How should the generated burst-compiled code be optimized (https://docs.unity3d.com/Packages/com.unity.burst@1.8/api/Unity.Burst.OptimizeFor.html)
    partial struct CullJob : IJobEntity
    {
        [ReadOnly] public NativeArray<float4> Planes;

        void Execute(ref SphereVisible visibility, ref HDRPMaterialPropertyBaseColor baseColor, in LocalToWorld localToWorld, in SphereRadius radius)
        {
            bool visible = true;

            for (int planeID = 0; planeID < 6; ++planeID)
            {
                // To Test if the sphere is inside of a camera fustrum plane, we do a dot product between the plane normal and the sphere center then we add the plane distance and the sphere radius
                // If the result is bigger than 0 we are inside otherwise we are outside
                if (math.dot(Planes[planeID].xyz, localToWorld.Position) + Planes[planeID].w + radius.Value <= 0)
                {
                    visible = false;
                    break;
                }
            }

            visibility.Value = visible ? 1 : 0;

            // Change material color to show objects out of the culling
            baseColor.Value = visible ? new float4(0.5f, 0.5f, 0.5f, 1f) : new float4(1f,0,0,1f);
        }
    }

    /// <summary>
    /// Version of the culling job that remove the break statement which generate branching in the assembly code.
    /// The job perform more computation but the performance gains from removing the branch allows the code to run faster than the simple version.
    /// </summary>
    [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
    partial struct CullJobNoBranch : IJobEntity
    {
        [ReadOnly] public NativeArray<float4> Planes;

        void Execute(ref SphereVisible visibility, ref HDRPMaterialPropertyBaseColor baseColor, in LocalToWorld localToWorld, in SphereRadius radius)
        {
            var pos = localToWorld.Position;
            visibility.Value =
                (math.dot(Planes[0].xyz, pos) + Planes[0].w + radius.Value > 0) &&
                (math.dot(Planes[1].xyz, pos) + Planes[1].w + radius.Value > 0) &&
                (math.dot(Planes[2].xyz, pos) + Planes[2].w + radius.Value > 0) &&
                (math.dot(Planes[3].xyz, pos) + Planes[3].w + radius.Value > 0) &&
                (math.dot(Planes[4].xyz, pos) + Planes[4].w + radius.Value > 0) &&
                (math.dot(Planes[5].xyz, pos) + Planes[5].w + radius.Value > 0)
                ? 1 : 0;

            // Change material color to show objects out of the culling
            baseColor.Value = visibility.Value == 1 ? new float4(0.5f, 0.5f, 0.5f, 1f) : new float4(1f, 0, 0, 1f);
        }
    }
}

