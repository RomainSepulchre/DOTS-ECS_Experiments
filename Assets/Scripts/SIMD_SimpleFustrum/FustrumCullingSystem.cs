using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Burst.SIMD.SimpleFustrum
{
    public partial struct FustrumCullingSystem : ISystem
    {
        private NativeArray<float4> _planes;
        private NativeArray<PlanePacket4> _planePackets;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _planes = new NativeArray<float4>(6, Allocator.Persistent);
            _planePackets = new NativeArray<PlanePacket4>(2, Allocator.Persistent); // 2 because we need 2 float4 to store 6 elements
        }

        // OnUpdate can't be Burst Compiled because we access a managed type (Camera) in FustrumCullingHelper.UpdateFustrumPlanes()
        // However once it's done we can call another function (DoCulling()) that is Burst-Compiled
        public void OnUpdate(ref SystemState state)
        {
            
            FustrumCullingHelper.UpdateFustrumPlanes(ref _planes); // Only needed with Simple culling and Culling without branching
            FustrumCullingHelper.CreatePlanePackets(ref _planePackets);
            DoCulling(ref state);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            _planes.Dispose();
            _planePackets.Dispose();
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
            //CullJobNoBranch cullJobNoBranch = new CullJobNoBranch
            //{
            //    Planes = _planes
            //};
            //state.Dependency = cullJobNoBranch.ScheduleParallel(state.Dependency);
            //state.Dependency.Complete(); // why do they complete the job immediately in the example ?

            // Culling with PlanePackets to allow SIMD instructions
            CullJobWithPackets cullJobWithPackets = new CullJobWithPackets
            {
                PlanePackets = _planePackets
            };
            state.Dependency = cullJobWithPackets.ScheduleParallel(state.Dependency);
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

    /// <summary>
    /// Version of the culling job where the data is repacked in plane packets to allow SIMD instructions.
    /// By checking a sphere with 2 PlanePacket4 in this solution, we have the same result as using 6 planes with only 33% of the mathematical operations.
    /// </summary>
    [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
    partial struct CullJobWithPackets : IJobEntity
    {
        [ReadOnly] public NativeArray<PlanePacket4> PlanePackets;

        void Execute(ref SphereVisible visibility, ref HDRPMaterialPropertyBaseColor baseColor, in LocalToWorld localToWorld, in SphereRadius radius)
        {
            var pos = localToWorld.Position;
            var p0 = PlanePackets[0];
            var p1 = PlanePackets[1];

            // Each component of a bool4 tell if the sphere is inside or outside of the culling for a specific plane:
            //  - 1st Packet bool4(left plane, right plane, down plane, up plane)
            //  - 2nd Packet bool4(near plane, far plane, fake plane*, fake plane*) *:fake plane are the planes we added to fill the packet, they always return false.
            // math.dot() is replaced with explicit multiply and add operations, because we’re now performing a dot product between a single position vector (pos.x, pos.y, pos.z) and four plane normal vectors (for instance, (p0.Xs, p0.Ys, p0.Zs)) simultaneously.
            bool4 firstPacketMask = (p0.Xs * pos.x + p0.Ys * pos.y + p0.Zs * pos.z + p0.Distances + radius.Value <= 0);
            bool4 secondPacketMask = (p1.Xs * pos.x + p1.Ys * pos.y + p1.Zs * pos.z + p1.Distances + radius.Value <= 0);
            
#if false // Breakdown of bool4 calulation for the example:

            // 1. We manually do the dot product by multiplying each component float4 by the corresponding float in the sphere position:
            float4 dotProduct = (p0.Xs * pos.x + p0.Ys * pos.y + p0.Zs * pos.z);
            // 2. We make the sum of dotProduct float4 from step 1 and the distance float4
            float4 sumDotAndDist = dotProduct + p0.Distances;
            // 3. We add the sphere radius float to our float4 calulated in step 2 (--> When doing the sum of a float4 and float, the float is added to every component of the float4)
            float4 resultFloat4 = sumDotAndDist + radius.Value;
            // 4. Every component of the Float4 is compared with <= 0 to define the value of each component of the bool4
            bool4 resultMask = resultFloat4 <= 0;
#endif

            // Merge both masks with a OR BITWISE OPERATION
            bool4 mergedMasks = firstPacketMask | secondPacketMask;

            // Every bool in the bool4 must be set to false to assume we are inside the culling, otherwise we are outside
            visibility.Value = mergedMasks.Equals(new bool4(false)) ? 1 : 0;

            // Change material color to show objects out of the culling
            baseColor.Value = visibility.Value == 1 ? new float4(0.5f, 0.5f, 0.5f, 1f) : new float4(1f, 0, 0, 1f);
        }
    }
}

