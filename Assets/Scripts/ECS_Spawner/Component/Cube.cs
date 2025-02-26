using JetBrains.Annotations;
using Unity.Entities;
using Unity.Mathematics;


namespace ECS.ECSExperiments
{
    // Entities and component: https://www.youtube.com/watch?v=jzCEzNoztzM

    public struct Cube : IComponentData, IEnableableComponent
    {
        public float3 MoveDirection;
        public float MoveSpeed;
        public bool MoveForward;
        public float Timer;
        public float TimerDuration;
    } 
}