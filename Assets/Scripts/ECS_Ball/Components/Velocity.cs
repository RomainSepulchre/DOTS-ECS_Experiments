using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS.Ball
{
    public struct Velocity : IComponentData
    {
        public float2 Value;
    }
}
