using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace ECS.ECSExperiments
{
    public partial struct InputSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Add a component on the system to hold the input data
            state.EntityManager.AddComponent<InputData>(state.SystemHandle);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Update the Component Data
            InputData updatedInputData = new InputData
            {
                UpKeyPressed = Input.GetKey(KeyCode.UpArrow),
                DownKeyPressed = Input.GetKey(KeyCode.DownArrow),
                LeftKeyPressed = Input.GetKey(KeyCode.LeftArrow),
                RightKeyPressed = Input.GetKey(KeyCode.RightArrow),
                SpaceKeyPressed = Input.GetKey(KeyCode.Space),
                RCtrlKeyPressed = Input.GetKey(KeyCode.RightControl)
            };
            SystemAPI.SetComponent<InputData>(state.SystemHandle, updatedInputData);

            //if (updatedInputData.UpKeyDown)
            //{
            //    Debug.Log($"UP KEY DOWN");
            //}
            //else if (updatedInputData.DownKeyDown)
            //{
            //    Debug.Log($"DOWN KEY DOWN");
            //}
            //else if (updatedInputData.LeftKeyDown)
            //{
            //    Debug.Log($"LEFT KEY DOWN");
            //}
            //else if (updatedInputData.RightKeyDown)
            //{
            //    Debug.Log($"RIGHT KEY DOWN");
            //}
            //else if (updatedInputData.SpaceKeyDown)
            //{
            //    Debug.Log($"SPACE KEY DOWN");
            //}
            //else if (updatedInputData.RCtrlKeyPressed)
            //{
            //    Debug.Log($"RIGHT CONTROL KEY DOWN");
            //}
        }

        public void OnDestroy(ref SystemState state)
        {
            // Most of component data is automatically destroyed when the system is destroyed, the main exception is Native Container.
            // If Native Container existed in the component, we must ensure the memory is disposed (usually, OnDestroy is the best place for this)
        }
    } 
}
