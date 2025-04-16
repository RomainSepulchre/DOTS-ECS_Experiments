using Project.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Ball
{

    [UpdateBefore(typeof(BallMovementSystem))] // To be sure carry is taken into account before next ball movement
    [UpdateBefore(typeof(TransformSystemGroup))] // To ensure the entity is rendered at proper position every time it is moved
    public partial struct BallCarrySystem : ISystem
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

            // Move carried ball
            foreach (var (ballTransform, carried) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Carried>>().WithAll<Ball>())
            {
                LocalTransform carrierTransform = state.EntityManager.GetComponentData<LocalTransform>(carried.ValueRO.CarriedByEntity);
                ballTransform.ValueRW.Position = carrierTransform.Position + config.CarryOffset;
            }

            // Carry a new ball or throw a carried ball

            if(Input.GetKeyDown(KeyCode.F))
            {
                // EnabledRefRW/EnabledRefRO = enabled state of the enableable component
                foreach (var (playerTransform, carry, carryEnabled, playerEntity) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<Carry>, EnabledRefRW<Carry>>().WithAll<Player>().WithEntityAccess()
                    .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)) // Ignore the state of enableable component
                {
                    if(carryEnabled.ValueRO)
                    {
                        // put down the ball
                        carryEnabled.ValueRW = false;

                        Entity carriedEntity = carry.ValueRO.EntityCarried;
                        LocalTransform carriedTransform = state.EntityManager.GetComponentData<LocalTransform>(carriedEntity);

                        carriedTransform.Position = playerTransform.ValueRO.Position + playerTransform.ValueRO.Forward();

                        state.EntityManager.SetComponentData(carriedEntity, carriedTransform);
                        state.EntityManager.SetComponentEnabled<Carried>(carriedEntity, false);
                    }
                    else
                    {
                        // pick up the ball
                        foreach(var (ballTransform, velocity, ballEntity) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<Velocity>>().WithAll<Ball>().WithDisabled<Carried>().WithEntityAccess())
                        {
                            float distSQ = math.distancesq(playerTransform.ValueRO.Position, ballTransform.ValueRO.Position);

                            if(distSQ <= config.BallInteractionRangeSQ)
                            {
                                carryEnabled.ValueRW = true;
                                carry.ValueRW.EntityCarried = ballEntity;

                                Carried newCarried = new Carried() { CarriedByEntity = playerEntity};
                                state.EntityManager.SetComponentData(ballEntity, newCarried);
                                state.EntityManager.SetComponentEnabled<Carried>(ballEntity, true);

                                // Reset ball velocity
                                velocity.ValueRW = new Velocity();
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}
