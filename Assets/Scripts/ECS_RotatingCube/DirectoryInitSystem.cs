using DOTS.Utilities;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace ECS.RotatingCube
{
    public partial struct DirectoryInitSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Exec_ECS_RotatingCube>();
        }

        // Can't burst compile since we dealwith managed objects
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false; // Update once

            GameObject go = GameObject.Find("Directory");
            if (go == null)
            {
                throw new System.Exception($"'Directory' GameObject is missing");
            }

            Directory directory = go.GetComponent<Directory>();

            DirectoryManaged directoryManaged = new DirectoryManaged();
            directoryManaged.RotatingCube = directory.RotatingCube;
            directoryManaged.RotationToggle = directory.RotationToggle;

            Entity entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, directoryManaged);

        }
    }
}
