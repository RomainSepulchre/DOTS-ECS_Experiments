using Unity.Entities;
using UnityEngine;

namespace DOTS.Utilities
{
    public class ExecuteAuthoring : MonoBehaviour
    {
        public bool SIMD_SimpleFustrum;
        public bool ECS_EnemyRunAwyay;
        public bool ECS_Experiments;
        public bool ECS_TargetAndSeeker;
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
}
