using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.TargetAndSeekerDemo
{
    [UpdateAfter(typeof(SpawnerSystem))]
    public partial struct MovementSystem : ISystem
    {
        EntityQuery moveQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            moveQuery = SystemAPI.QueryBuilder().WithAllRW<Movement, LocalTransform>().Build();
            state.RequireForUpdate(moveQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // TODO: Can I use Random.Range with burstcompile ?
            //uint randomSeed = (uint)UnityEngine.Random.Range(1, uint.MaxValue);
            MoveJob moveJob = new MoveJob()
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                ElapsedTime = SystemAPI.Time.ElapsedTime,
                ///RandomSeed = randomSeed,
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
        //[ReadOnly] public uint RandomSeed;

        public void Execute(ref Movement movement, ref LocalTransform transform, [EntityIndexInChunk] int indexInChunk, [ChunkIndexInQuery] int chunkIndex)
        {
            uint seed = (uint)((chunkIndex * indexInChunk) + ElapsedTime / DeltaTime);

            // Process timer and change direction is timer is finished
            ProcessTimer(ref movement, seed);

            // Process movement
            ProcessMovement(ref movement, ref transform);
        }

        private void ProcessTimer(ref Movement movement, uint seed)
        {
            movement.Timer -= DeltaTime;
            if (movement.Timer <= 0)
            {
                Random random = new Random(seed);
                float2 new2dDirection = random.NextFloat2Direction();
                movement.Direction = new float3(new2dDirection.x, 0, new2dDirection.y);
                movement.Timer = random.NextFloat(movement.MinTimer, movement.MaxTimer);
            }
        }

        private void ProcessMovement(ref Movement movement, ref LocalTransform transform)
        {
            float3 nextPos = transform.Position + (movement.Direction * movement.Speed * DeltaTime);
            transform.Position = nextPos;
        }
    }
}
