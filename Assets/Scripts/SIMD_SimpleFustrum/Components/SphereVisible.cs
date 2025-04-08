using Unity.Entities;
using UnityEngine;

namespace Burst.SIMD.SimpleFustrum
{
    public struct SphereVisible : IComponentData
    {
        // We use a int instead of a bool, it will make things simpler later
        public int Value; // 1 = Visible, 0 = Not Visible
    }
}
