using DOTS.Utilities;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.RotatingCube
{
    public partial struct CubeRotationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Exec_ECS_RotatingCube>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {


            foreach (var (transform, speed) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotationSpeed>>())
            {
                transform.ValueRW = transform.ValueRO.RotateY(speed.ValueRO.RadiansPerSecond * SystemAPI.Time.DeltaTime);
            }

            // USe an aspect to Query LocalTransform and VerticalSpeed
            foreach (var verticalMovement in SystemAPI.Query<VerticalMovementAspect>())
            {
                verticalMovement.MoveVertically(SystemAPI.Time.ElapsedTime);
            }
        }
    }

    // For data access safety the aspect struct and the Ref(RefRW, RefRO) must be marked as readonly
    // Struct is also partial due to source generation
    // Aspects are convienent for dealing with large sets of components
    readonly partial struct VerticalMovementAspect : IAspect
    {
        readonly RefRW<LocalTransform> _transform;
        readonly RefRO<VerticalSpeed> _speed;

        public void MoveVertically(double elapsedTime)
        {
            _transform.ValueRW.Position.y = (float)math.sin(elapsedTime * _speed.ValueRO.RadiansPerSecond) * _speed.ValueRO.MaxYPos;
        }
    }
}
