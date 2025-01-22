using JetBrains.Annotations;
using Unity.Entities;
using Unity.Mathematics;

public class CubeComponent : IComponentData
{
    public float3 MoveDirection;
    public float MoveSpeed;

}
