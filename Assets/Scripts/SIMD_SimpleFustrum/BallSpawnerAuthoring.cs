using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Random = UnityEngine.Random;

namespace Burst.SIMD.SimpleFustrum
{
    public class BallSpawnerAuthoring : MonoBehaviour
    {
        public int spawnAmount;
        public GameObject ballToSpawn;
        public Vector3 minSpawnPos;
        public Vector3 maxSpawnPos;
    }

    public class BallSpawnerBaker : Baker<BallSpawnerAuthoring>
    {
        public override void Bake(BallSpawnerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            BallSpawner ballSpawner = new BallSpawner()
            {
                SpawnAmount = authoring.spawnAmount,
                BallToSpawn = GetEntity(authoring.ballToSpawn, TransformUsageFlags.Dynamic),
                MinSpawnPos = authoring.minSpawnPos,
                MaxSpawnPos = authoring.maxSpawnPos,
            };
            AddComponent(entity, ballSpawner);

            RandomData newRandom = new RandomData()
            {
                Value = new Unity.Mathematics.Random((uint)Random.Range(1, int.MaxValue))
            };
            AddComponent(entity, newRandom);
        }
    }
}
