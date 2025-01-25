using JetBrains.Annotations;
using Unity.Entities;
using Unity.Mathematics;


// Entities and component: https://www.youtube.com/watch?v=jzCEzNoztzM

public class CubeComponent : IComponentData
{
    public float3 MoveDirection;
    public float MoveSpeed;

}
