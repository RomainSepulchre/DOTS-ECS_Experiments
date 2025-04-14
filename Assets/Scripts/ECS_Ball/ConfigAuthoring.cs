using Unity.Entities;
using UnityEngine;

namespace ECS.Ball
{
    public class ConfigAuthoring : MonoBehaviour
    {
        [Header("General")]
        public int numRows;
        public int numColumns;

        [Header("Obstacle")]
        public float obstacleGridCellSize;
        public float obstacleRadius;
        public float obstacleOffset;
        public GameObject obstaclePrefab;

        [Header("Player")]
        public float playerOffset;
        public float playerSpeed;
        public GameObject playerPrefab;

        [Header("Ball")]
        public float ballStartVelocity;
        public float ballVelocityDecay;
        public float ballKickingRange;
        public float ballKickForce;
        public GameObject ballPrefab;
    }

    class ConfigBaker : Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            Config newConfig = new Config()
            {
                NumRows = authoring.numRows,
                NumColumns = authoring.numColumns,

                ObstacleGridCellSize = authoring.obstacleGridCellSize,
                ObstacleRadius = authoring.obstacleRadius,
                ObstacleOffset = authoring.obstacleOffset,
                ObstaclePrefab = GetEntity(authoring.obstaclePrefab, TransformUsageFlags.Dynamic),

                PlayerOffset = authoring.playerOffset,
                PlayerSpeed = authoring.playerSpeed,
                PlayerPrefab = GetEntity(authoring.playerPrefab, TransformUsageFlags.Dynamic),

                BallStartVelocity = authoring.ballStartVelocity,
                BallVelocityDecay = authoring.ballVelocityDecay,
                BallKickingRangeSQ = authoring.ballKickingRange * authoring.ballKickingRange,
                BallKickForce = authoring.ballKickForce,
                BallPrefab = GetEntity(authoring.ballPrefab, TransformUsageFlags.Dynamic),

                RandomSeed = (uint)Random.Range(1, uint.MaxValue)
            };
            AddComponent(entity, newConfig);
        }
    }
}
