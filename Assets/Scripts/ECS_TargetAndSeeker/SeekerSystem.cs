using Unity.Entities;
using UnityEngine;

namespace ECS.TargetAndSeekerDemo
{
    [UpdateAfter(typeof(MovementSystem))]
    [UpdateAfter(typeof(SpawnerSystem))]
    public partial struct SeekerSystem : ISystem
    {
        // Find the seeker nearest target
        // Draw debug line to nearest target
    }
}