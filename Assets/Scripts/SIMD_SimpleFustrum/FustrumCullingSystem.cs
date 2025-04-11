using DOTS.Utilities;
using Unity.Burst;
using Unity.Burst.Intrinsics;
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
        private NativeArray<PlanePacket4> _planePackets;
        private EntityQuery sphereQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _planes = new NativeArray<float4>(6, Allocator.Persistent);
            _planePackets = new NativeArray<PlanePacket4>(2, Allocator.Persistent); // 2 because we need 2 float4 to store 6 elements
            sphereQuery = SystemAPI.QueryBuilder().WithAll<LocalToWorld,SphereRadius>().WithAllRW<SphereVisible, HDRPMaterialPropertyBaseColor>().Build();
            state.RequireForUpdate(sphereQuery);
            state.RequireForUpdate<Exec_SIMD_SimpleFustrum>();
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
            //CullJobWithPackets cullJobWithPackets = new CullJobWithPackets
            //{
            //    PlanePackets = _planePackets
            //};
            //state.Dependency = cullJobWithPackets.ScheduleParallel(state.Dependency);
            //state.Dependency.Complete(); // why do they complete the job immediately in the example ?


            // Culling with SpherePackets to have wider batches of data using SIMD instructions
            CullJobWithSpherePackets cullJobWithSpherePackets = new CullJobWithSpherePackets
            {
                LocalToWorldTypeHandle = SystemAPI.GetComponentTypeHandle<LocalToWorld>(true),
                SphereRadiusTypeHandle = SystemAPI.GetComponentTypeHandle<SphereRadius>(true),
                SphereVisibleTypeHandle = SystemAPI.GetComponentTypeHandle<SphereVisible>(false), // false = is not read only
                BaseColorTypeHandle = SystemAPI.GetComponentTypeHandle<HDRPMaterialPropertyBaseColor>(false), // false = is not read only
                FustrumPlanes = _planes
            };
            state.Dependency = cullJobWithSpherePackets.ScheduleParallel(sphereQuery, state.Dependency);
            state.Dependency.Complete();
        }
    }

    /// <summary>
    /// Simple version of the culling job.
    /// With 100000 spheres with all worker thread: 0.23ms ? faster than method without branching? and similar to plane packets?
    /// With 100000 spheres with one worker thread: 0.85-0.90ms
    /// </summary>
    [BurstCompile(OptimizeFor = OptimizeFor.Performance)] // How should the generated burst-compiled code be optimized (https://docs.unity3d.com/Packages/com.unity.burst@1.8/api/Unity.Burst.OptimizeFor.html)
    partial struct CullJob : IJobEntity
    {
        [ReadOnly] public NativeArray<float4> Planes;

        public void Execute(ref SphereVisible visibility, ref HDRPMaterialPropertyBaseColor baseColor, in LocalToWorld localToWorld, in SphereRadius radius)
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
    /// With 100000 spheres with all worker thread: ~0.26ms
    /// With 100000 spheres with one worker thread: 0.80-0.85ms
    /// </summary>
    [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
    partial struct CullJobNoBranch : IJobEntity
    {
        [ReadOnly] public NativeArray<float4> Planes;

        public void Execute(ref SphereVisible visibility, ref HDRPMaterialPropertyBaseColor baseColor, in LocalToWorld localToWorld, in SphereRadius radius)
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
    /// With 100000 spheres with all worker thread: ~0.23ms
    /// With 100000 spheres with one worker thread: 0.63-0.69ms
    /// </summary>
    [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
    partial struct CullJobWithPackets : IJobEntity
    {
        [ReadOnly] public NativeArray<PlanePacket4> PlanePackets;

        public void Execute(ref SphereVisible visibility, ref HDRPMaterialPropertyBaseColor baseColor, in LocalToWorld localToWorld, in SphereRadius radius)
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

    /// <summary>
    /// Version of the culling job where the use of SIMD optimizations is even better: instead of packing frustrum planes in packets of float4, we pack the spheres to have a wider amount of data packed
    /// This version is even faster than the previous one because instead of packing 6 planes, we pack the spheres using a chunk job which further reduces the number of mathematical operation needed
    /// With 100000 spheres with all worker thread: ~0.20ms
    /// With 100000 spheres with one worker thread: 0.54-0.59ms
    /// </summary>
    [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
    partial struct CullJobWithSpherePackets : IJobChunk
    {
        [ReadOnly] public ComponentTypeHandle<LocalToWorld> LocalToWorldTypeHandle;
        [ReadOnly] public ComponentTypeHandle<SphereRadius> SphereRadiusTypeHandle;
        [ReadOnly] public NativeArray<float4> FustrumPlanes;

        public ComponentTypeHandle<SphereVisible> SphereVisibleTypeHandle;
        public ComponentTypeHandle<HDRPMaterialPropertyBaseColor> BaseColorTypeHandle;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            // Get array of components in the chunk
            NativeArray<LocalToWorld>.ReadOnly chunkLocalToWorld = chunk.GetNativeArray(ref LocalToWorldTypeHandle).AsReadOnly();
            NativeArray<float> chunkSphereRadius = chunk.GetNativeArray(ref SphereRadiusTypeHandle).Reinterpret<float>(); // We reinterpret the data as float to make its repacking easier
            NativeArray<SphereVisible> chunkSphereVisible = chunk.GetNativeArray(ref SphereVisibleTypeHandle);
            NativeArray<HDRPMaterialPropertyBaseColor> chunkBaseColor = chunk.GetNativeArray(ref  BaseColorTypeHandle);

            float4 plane0 = FustrumPlanes[0];
            float4 plane1 = FustrumPlanes[1];
            float4 plane2 = FustrumPlanes[2];
            float4 plane3 = FustrumPlanes[3]; 
            float4 plane4 = FustrumPlanes[4];
            float4 plane5 = FustrumPlanes[5];

            for (int i = 0; chunk.Count - i >= 4; i+=4) // We check every 4 items because we pack them in float4
            {
                // Store positions in float4
                float3 posA = chunkLocalToWorld[i].Position;
                float3 posB = chunkLocalToWorld[i+1].Position;
                float3 posC = chunkLocalToWorld[i+2].Position;
                float3 posD = chunkLocalToWorld[i+3].Position;

                float4 Xs = new float4(posA.x, posB.x, posC.x, posD.x);
                float4 Ys = new float4(posA.y, posB.y, posC.y, posD.y);
                float4 Zs = new float4(posA.z, posB.z, posC.z, posD.z);

                // Store radii in a float4
                float4 Radii = chunkSphereRadius.ReinterpretLoad<float4>(i); // Load the data from the array as a different type

                // Test the 4 spheres with the 6 fustrum planes
                // We are inside if the result is bigger than 0, so they all plane checks must be true for a sphere to be inside
                bool4 mask =
                    plane0.x * Xs + plane0.y * Ys + plane0.z * Zs + plane0.w + Radii > 0.0f & // AND BIT OPERATION
                    plane1.x * Xs + plane1.y * Ys + plane1.z * Zs + plane1.w + Radii > 0.0f &
                    plane2.x * Xs + plane2.y * Ys + plane2.z * Zs + plane2.w + Radii > 0.0f &
                    plane3.x * Xs + plane3.y * Ys + plane3.z * Zs + plane3.w + Radii > 0.0f &
                    plane4.x * Xs + plane4.y * Ys + plane4.z * Zs + plane4.w + Radii > 0.0f &
                    plane5.x * Xs + plane5.y * Ys + plane5.z * Zs + plane5.w + Radii > 0.0f;

                // TODO: ? I don't see why we aren't using a bool in SphereVisible and reinterpret here directly the bool4 ? Why is using ints faster here ?
                chunkSphereVisible.ReinterpretStore(i, new int4(mask)); // Reintepret the data to store it in the array

                chunkBaseColor[i] = new HDRPMaterialPropertyBaseColor { Value = mask.x ? new float4(0.5f, 0.5f, 0.5f, 1f) : new float4(1f, 0, 0, 1f) };
                chunkBaseColor[i+1] = new HDRPMaterialPropertyBaseColor { Value = mask.y ? new float4(0.5f, 0.5f, 0.5f, 1f) : new float4(1f, 0, 0, 1f) };
                chunkBaseColor[i+2] = new HDRPMaterialPropertyBaseColor { Value = mask.z ? new float4(0.5f, 0.5f, 0.5f, 1f) : new float4(1f, 0, 0, 1f) };
                chunkBaseColor[i+3] = new HDRPMaterialPropertyBaseColor { Value = mask.w ? new float4(0.5f, 0.5f, 0.5f, 1f) : new float4(1f, 0, 0, 1f) };
            }

            // If number of entities isn't divisible by 4, process the entity left
            for (int i = (chunk.Count >> 2) << 2; i < chunk.Count; i++) // To get the start index we count number of full packet with (chunk.Count >> 2) then we get the start index of next packet with (chunk.Count >> 2) << 2.
            {
                float3 pos = chunkLocalToWorld[i].Position;
                float radius = chunkSphereRadius[i];

                int visible =
                    (math.dot(plane0.xyz, pos) + plane0.w + radius > 0.0f &&
                    math.dot(plane1.xyz, pos) + plane1.w + radius > 0.0f &&
                    math.dot(plane2.xyz, pos) + plane2.w + radius > 0.0f &&
                    math.dot(plane3.xyz, pos) + plane3.w + radius > 0.0f &&
                    math.dot(plane4.xyz, pos) + plane4.w + radius > 0.0f &&
                    math.dot(plane5.xyz, pos) + plane5.w + radius > 0.0f) ? 1 : 0;

                chunkSphereVisible[i] = new SphereVisible { Value = visible };

                chunkBaseColor[i] = new HDRPMaterialPropertyBaseColor { Value = visible == 1 ? new float4(0.5f, 0.5f, 0.5f, 1f) : new float4(1f, 0, 0, 1f) };
            }
        }
    }
}

