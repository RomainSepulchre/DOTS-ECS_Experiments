using JetBrains.Annotations;
using Unity.Entities;
using Unity.Mathematics;


// Entities and component: https://www.youtube.com/watch?v=jzCEzNoztzM

public struct Cube : IComponentData
{
    public float3 MoveDirection;
    public float MoveSpeed;
    public bool MoveForward;
    public float Timer;
    public float TimerDuration;
}