using DOTS.Utilities;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace ECS.RotatingCube
{
    public partial struct ManagedCubeRotationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DirectoryManaged>();
            state.RequireForUpdate<Exec_ECS_RotatingCube>();
        }

        public void OnUpdate(ref SystemState state)
        {
            DirectoryManaged directory = SystemAPI.ManagedAPI.GetSingleton<DirectoryManaged>();

            // TODO: Set ManagedCubeRotator as a tag component and create a new component to store the gameObject that we add ónly if we wasn't added yet
            // Make sure managedCubeRotator.Value has been set
            foreach (var (managedCubeRotator, entity) in SystemAPI.Query<ManagedCubeRotator>().WithEntityAccess())
            {
                if (managedCubeRotator.Value == null)
                {
                    state.EntityManager.SetComponentData(entity, new ManagedCubeRotator(directory.RotatingCube));
                    return;
                }
            }

            if (!directory.RotationToggle.isOn)
            {
                return;
            }

            // Update cube rotation
            foreach (var (transform, speed, go) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotationSpeed>, ManagedCubeRotator>())
            {
                transform.ValueRW = transform.ValueRO.RotateY(speed.ValueRO.RadiansPerSecond * SystemAPI.Time.DeltaTime);

                go.Value.transform.rotation = transform.ValueRO.Rotation;
            }
        }
    }
}
