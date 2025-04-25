using Unity.Entities;
using UnityEngine;

namespace Project.Utilities
{
    public class ExecuteAuthoring : MonoBehaviour
    {
        public bool SIMD_SimpleFustrum;
        public bool ECS_EnemyRunAwyay;
        public bool ECS_Experiments;
        public bool ECS_TargetAndSeeker;
        public bool ECS_RotatingCube;
        public bool ECS_Ball;
        public bool ECS_StateChange;
        public bool ECS_Baking;
    }

    class ExecuteBaker : Baker<ExecuteAuthoring>
    {
        public override void Bake(ExecuteAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            if (authoring.SIMD_SimpleFustrum) AddComponent<Exec_SIMD_SimpleFustrum>(entity);
            if (authoring.ECS_EnemyRunAwyay) AddComponent<Exec_ECS_EnemyRunAway>(entity);
            if (authoring.ECS_Experiments) AddComponent<Exec_ECS_Experiments>(entity);
            if (authoring.ECS_TargetAndSeeker) AddComponent<Exec_ECS_TargetAndSeeker>(entity);
            if (authoring.ECS_RotatingCube) AddComponent<Exec_ECS_RotatingCube>(entity);
            if (authoring.ECS_Ball) AddComponent<Exec_ECS_Ball>(entity);
            if (authoring.ECS_StateChange) AddComponent<Exec_ECS_StateChange>(entity);
            if (authoring.ECS_Baking) AddComponent<Exec_ECS_Baking>(entity);
        }
    }

    public struct Exec_SIMD_SimpleFustrum : IComponentData
    {
    }
    public struct Exec_ECS_EnemyRunAway : IComponentData
    {
    }
    public struct Exec_ECS_Experiments : IComponentData
    {
    }
    public struct Exec_ECS_TargetAndSeeker : IComponentData
    {
    }
    public struct Exec_ECS_RotatingCube : IComponentData
    {
    }
    public struct Exec_ECS_Ball : IComponentData
    {
    }
    public struct Exec_ECS_StateChange : IComponentData
    {
    }
    public struct Exec_ECS_Baking : IComponentData
    {
    }
}
