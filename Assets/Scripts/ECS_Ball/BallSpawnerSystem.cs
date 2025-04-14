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

                    Velocity newVelocity = new Velocity()
                    {
                        Value = random.NextFloat2Direction() * config.BallStartVelocity
                    };
                    state.EntityManager.SetComponentData(ball, newVelocity);

                    LocalTransform newBallTransform = new LocalTransform()
                    {
                        Position = playerTransform.ValueRO.Position +( new float3(newVelocity.Value.x, 0, newVelocity.Value.y)),
                        Rotation = Quaternion.identity,
                        Scale = 1
                    };
                    state.EntityManager.SetComponentData(ball, newBallTransform);

                    

                }
            }
        }
    }
}
