using Unity.Entities;
using UnityEngine;

namespace ECS.TargetAndSeekerDemo
{
    [UpdateAfter(typeof(SpawnerSystem))]
    public partial struct MovementSystem : ISystem
    {
        // Move entity with movement component
        // 1. Set direction
        // 2. Move entity
    } 
}
