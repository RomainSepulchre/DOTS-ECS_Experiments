using Unity.Collections;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

namespace ECS.EnemyRunAwayDemo
{
    [UpdateAfter(typeof(PlayerSystem))]
    public partial struct EnemiesSystem : ISystem
    {
        EntityQuery enemyQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            enemyQuery = SystemAPI.QueryBuilder().WithAllRW<LocalTransform>().WithAll<Enemy>().Build();
            state.RequireForUpdate(enemyQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // ? What is happening when using ComponentLookup<LocalTransform> + ref LocalTransform in the job ?
            // -> It doesn't seems to be possible to use both in the same job, it look like in this case I should only use the component lookup to access LocalTransform which is fine
            RunAwayFromPlayer runAwayFromPlayerJob = new RunAwayFromPlayer()
            {
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
                DeltaTime = SystemAPI.Time.DeltaTime
            };

            // Schedule the job and reassign system dependency
            // [NativeDisableParallelForRestriction] ont the ComponentLookup in the job allow to use parrallel scheduling
            state.Dependency = runAwayFromPlayerJob.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct RunAwayFromPlayer : IJobEntity
    {
        // using [NativeDisableParallelForRestriction] allow to run job in parallel but we need to be sure the data we look up doesn't overlaps the data we want to read and write
        [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> LocalTransformLookup;
        [ReadOnly] public float DeltaTime;

        public void Execute(in Enemy enemy, Entity entity)
        {
            
            // Ok to compare local position because they're all at the root of the hierarchy
            float3 playerPos = LocalTransformLookup[enemy.Player].Position;
            float3 enemyPos = LocalTransformLookup[entity].Position;

            if (math.distancesq(playerPos, enemyPos) < math.lengthsq(enemy.TooCloseThreshold))
            {
                float3 runAwayDirection;

                float3 dirPlayerToThis = math.normalize(enemyPos - playerPos);
                float3 futurePos = enemyPos + (dirPlayerToThis / 2); // check where enemy will be if he move of half the player dir vector

                bool playerIsLower = playerPos.y < enemyPos.y;
                bool playerIsAtLeft = playerPos.x < enemyPos.x;

                if (futurePos.x >= enemy.XAreaLimit) // Near right limit
                {
                    if(playerIsLower) runAwayDirection = GetPerpendicularCounterClockwiseVector(futurePos);
                    else runAwayDirection = GetPerpendicularClockwiseVector(futurePos);
                }
                else if (futurePos.x <= -enemy.XAreaLimit) // Near left limit
                {
                    if (playerIsLower) runAwayDirection = GetPerpendicularClockwiseVector(futurePos);
                    else runAwayDirection = GetPerpendicularCounterClockwiseVector(futurePos);
                }
                else if (futurePos.y >= enemy.YAreaLimit) // Near top limit
                {
                    if (playerIsAtLeft) runAwayDirection = GetPerpendicularClockwiseVector(futurePos);
                    else runAwayDirection = GetPerpendicularCounterClockwiseVector(futurePos);
                }
                else if (futurePos.y <= -enemy.YAreaLimit) // Near bottom limit
                {
                    if (playerIsAtLeft) runAwayDirection = GetPerpendicularCounterClockwiseVector(futurePos);
                    else runAwayDirection = GetPerpendicularClockwiseVector(futurePos);
                }
                else
                {
                    runAwayDirection = dirPlayerToThis;
                }

                RefRW<LocalTransform> enemyTransform = LocalTransformLookup.GetRefRW(entity);
                enemyTransform.ValueRW.Position += (runAwayDirection * enemy.Speed * DeltaTime);
            }
        }

        private float3 GetPerpendicularClockwiseVector(float3 initialVector)
        {
            return new float3(initialVector.y, -initialVector.x, initialVector.z);
        }

        private float3 GetPerpendicularCounterClockwiseVector(float3 initialVector)
        {
            return new float3(-initialVector.y, initialVector.x, initialVector.z);
        }
    }


}
