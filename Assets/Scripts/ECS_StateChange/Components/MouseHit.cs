using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS.StateChange
{
    public struct MouseHit : IComponentData
    {
        public float3 HitPosition;
        public bool HitChanged;
    }
}
