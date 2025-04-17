using Project.Utilities;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;
using UnityEngine;
using Unity.Mathematics;

namespace ECS.Ball
{
    [UpdateBefore(typeof(TransformSystemGroup))] // To ensure the entity is rendered at proper position aftfer being spawned
    public partial struct BallSpawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<Exec_ECS_Ball>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Config config = SystemAPI.GetSingleton<Config>();

            if(Input.GetKeyDown(KeyCode.Space))
            {
                Random random = new Random(config.RandomSeed);

                foreach(var playerTransform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<Player>())
                {
                    Entity ball = state.EntityManager.Instantiate(config.BallPrefab);

                    float2 direction = random.NextFloat2Direction();
                    Velocity newVelocity = new Velocity()
                    {
                        Value = direction * config.BallStartVelocity
                    };
                    state.EntityManager.SetComponentData(ball, newVelocity);

                    float ballScale = 1;
                    float playerAndBallRadius = (playerTransform.ValueRO.Scale / 2) + (ballScale / 2);
                    float3 playerRadiusOffset = new float3(direction.x, 0, direction.y) * playerAndBallRadius;
                    LocalTransform newBallTransform = new LocalTransform()
                    {
                        Position = playerTransform.ValueRO.Position + playerRadiusOffset,
                        Rotation = Quaternion.identity,
                        Scale = ballScale
                    };
                    state.EntityManager.SetComponentData(ball, newBallTransform);

                    

                }
            }
        }
    }
}
