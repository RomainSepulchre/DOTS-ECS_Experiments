using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;

namespace Burst.SIMD.SimpleFustrum
{
    public struct FustrumCullingHelper
    {
        static Camera _camera;
        static Plane[] _planesOOP = new Plane[6];

        public static void UpdateFustrumPlanes(ref NativeArray<float4> planes)
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            GeometryUtility.CalculateFrustumPlanes(_camera, _planesOOP);

            for (int i = 0; i < 6; i++)
            {
                planes[i] = new float4(_planesOOP[i].normal, _planesOOP[i].distance);
            }
        }

        public static void CreatePlanePackets(ref NativeArray<PlanePacket4> planePackets)
        {
            var planes = new NativeArray<float4>(6, Allocator.Temp); // no need to manually dispose temp allocation, it's automatically disposed at the end of the frame
            UpdateFustrumPlanes(ref planes);

            int cullingPlaneCount = planes.Length;

            // ?: +3 => because one packets can have up to 4 float (1+3) and even if we only need to store one element we will have at least one packet, so doing +3 we know how many packets we need even if they are not full.
            // ?: >> 2 => we shift of 2 bits on the right to know the number of packets we need
            // ex: 9+3=12 (or 1100 in binary),  12>>2=3 (or 11 in binary)
            int packetsCount = (cullingPlaneCount + 3) >> 2;

            for (int i = 0; i < cullingPlaneCount; i++)
            {
                // We get the index of the packet using bit shift: every 4 elements we reach next packet index
                // (ex: index is 2 (or 10 in binary), 2>>2=0 (index of first packet). index is 4 (or 100 in binary), 4>>2=1 (index of second packet))
                int packetIndex = i >> 2;

                var p = planePackets[packetIndex];

                // ?: i & 3 => Bit AND operation with 3 (11 in binary) to keep only two first bits of i, this allows us to fill the correct element in the current packet
                // ex: 0 & 3 = 0  (00 & 11 = 00 in binary), 1 & 3 = 1 (01 & 11 = 01 in binary), 2 & 3 = 2 (10 & 11 = 10 in binary), 3 & 3 = 3  (11 & 11 = 11 in binary)
                //     4 & 3 = 0  (100 & 011 = 000 in binary), 5 & 3 = 1 (101 & 011 = 001 in binary), 6 & 3 = 2 (110 & 011 = 010 in binary), 7 & 3 = 3  (111 & 011 = 011 in binary) 
                p.Xs[i & 3] = planes[i].x;
                p.Ys[i & 3] = planes[i].y;
                p.Zs[i & 3] = planes[i].z;
                p.Distances[i & 3] = planes[i].w;

                planePackets[packetIndex] = p;
            }

            // Populate the remaining empty packets elements with planes values that are always "in"
            for (int i = cullingPlaneCount; i < 4 * packetsCount; i++)
            {
                var p = planePackets[i >> 2];
                p.Xs[i & 3] = 1.0f;
                p.Ys[i & 3] = 0.0f;
                p.Zs[i & 3] = 0.0f;

                // We want to set these distances to a very large number, but one which
                // still allows us to add sphere radius values. Let's try 1 billion.
                p.Distances[i & 3] = 1e9f;

                planePackets[i >> 2] = p;
            }
        }
    }

    public struct PlanePacket4
    {
        public float4 Xs;
        public float4 Ys;
        public float4 Zs;
        public float4 Distances;
    }
}
