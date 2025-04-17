using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS.Ball
{
    public struct Config : IComponentData
    {
        // General
        public int NumRows;
        public int NumColumns;

        // Obstacle
        public float ObstacleGridCellSize;
        public float ObstacleRadius;
        public float ObstacleOffset;
        public Entity ObstaclePrefab;

        // Player
        public float PlayerOffset;
        public float PlayerSpeed; // in meter/s
        public Entity PlayerPrefab;

        // Ball
        public float BallStartVelocity;
        public float BallVelocityDecay;
        public float BallInteractionRangeSQ; // Squared range to compare dist with squared value which is more effficient
        public float BallKickForce;
        public Entity BallPrefab;

        // Carry
        public float3 CarryOffset;

        // Random
        public uint RandomSeed;
    }
}
