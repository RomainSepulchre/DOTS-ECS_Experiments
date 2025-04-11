using DOTS.Utilities;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace ECS.EnemyRunAwayDemo
{
    [UpdateBefore(typeof(PlayerSystem))]  // Must run before player system
    public partial struct InputSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Add my player input component on the system handle
            state.EntityManager.AddComponent<PlayerInput>(state.SystemHandle);
            state.RequireForUpdate<Exec_ECS_EnemyRunAway>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Update the value of PlayerInput component
            PlayerInput newInput = new PlayerInput()
            {
                UpArrowPressed = Input.GetKey(KeyCode.UpArrow),
                DownArrowPressed = Input.GetKey(KeyCode.DownArrow),
                LeftArrowPressed = Input.GetKey(KeyCode.LeftArrow),
                RightArrowPressed = Input.GetKey(KeyCode.RightArrow),
            };
            SystemAPI.SetComponent<PlayerInput>(state.SystemHandle, newInput);
        }

        // No native array data, so no component cleaning needed at OnDestroy
    } 
}
