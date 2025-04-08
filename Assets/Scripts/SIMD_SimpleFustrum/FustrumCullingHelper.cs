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
    }
}
