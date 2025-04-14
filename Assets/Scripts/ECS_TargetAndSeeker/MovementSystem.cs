using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Project.Utilities;

namespace ECS.TargetAndSeekerDemo
{
    [UpdateAfter(typeof(SpawnerSystem))]
    public partial struct MovementSystem : ISystem
    {
        EntityQuery moveQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            moveQuery = SystemAPI.QueryBuilder().WithAllRW<Movement, LocalTransform>().WithAllRW<RandomData>().Build();
            state.RequireForUpdate(moveQuery);
            state.RequireForUpdate<Exec_ECS_TargetAndSeeker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            MoveJob moveJob = new MoveJob()
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                ElapsedTime = SystemAPI.Time.ElapsedTime,
            };

            // Schedule job
            state.Dependency = moveJob.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct MoveJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public double ElapsedTime;

        public void Execute(ref Movement movement, ref LocalTransform transform, ref RandomData random)
        {
            // Process timer and change direction is timer is finished
            ProcessTimer(ref movement, ref random);

            // Process movement
            ProcessMovement(ref movement, ref transform);
        }

        private void ProcessTimer(ref Movement movement, ref RandomData random)
        {
            movement.Timer -= DeltaTime;
            if (movement.Timer <= 0)
            {
                float2 new2dDirection = random.Value.NextFloat2Direction();
                movement.Direction = new float3(new2dDirection.x, 0, new2dDirection.y);
                movement.Timer = random.Value.NextFloat(movement.MinTimer, movement.MaxTimer);
            }
        }

        private void ProcessMovement(ref Movement movement, ref LocalTransform transform)
        {
            float3 nextPos = transform.Position + (movement.Direction * movement.Speed * DeltaTime);
            transform.Position = nextPos;
        }
    }
}
