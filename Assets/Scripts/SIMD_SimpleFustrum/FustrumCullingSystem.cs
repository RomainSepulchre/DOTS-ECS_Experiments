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

        [BurstCompile]
        public void DoCulling(ref SystemState state)
        {
            CullJob cullingJob = new CullJob
            {
                Planes = _planes
            };
            state.Dependency = cullingJob.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
        }
    }

    [BurstCompile(OptimizeFor = OptimizeFor.Performance)] // TODO: Checks what OptimizeFor.Performance exactly does ?
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
            baseColor.Value = visible ? new float4(0.5f, 0.5f, 0.5f, 1f) : new float4(1f,0,0,1f);
        }
    }
}

