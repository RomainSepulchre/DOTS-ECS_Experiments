using Unity.Entities;
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
        public float BallKickingRangeSQ; // Squared range to compare dist with squared value which is more effficient
        public float BallKickForce;
        public Entity BallPrefab;

        // Random
        public uint RandomSeed;
    }
}
