using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class CubeAuthoring : MonoBehaviour
{
    public Vector3 direction;
    public float moveSpeed;
    public bool moveForward;
    public float timerDuration;
}

class CubeBaker : Baker<CubeAuthoring>
{
    public override void Bake(CubeAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);

        Cube newCube = new Cube()
        {
            MoveDirection = authoring.direction,
            MoveSpeed = authoring.moveSpeed,
            MoveForward = authoring.moveForward,
            Timer = authoring.timerDuration,
            TimerDuration = authoring.timerDuration,
        };

        AddComponent(entity, newCube);
    }
}
