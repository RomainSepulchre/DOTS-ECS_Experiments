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
            // TODO: Understand what's happen when using ComponentLookup<LocalTransform> + ref LocalTransform in the job ?
            RunAwayFromPlayer runAwayFromPlayerJob = new RunAwayFromPlayer()
            {
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
                DeltaTime = SystemAPI.Time.DeltaTime
            };

            // TODO: schedule in parrallel when ComponentLookup<LocalTransform> + ref LocalTransform is fixed
            //Schedule the job and reassign system dependency
            state.Dependency = runAwayFromPlayerJob.Schedule(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct RunAwayFromPlayer : IJobEntity
    {
        public ComponentLookup<LocalTransform> LocalTransformLookup;
        [ReadOnly] public float DeltaTime;

        public void Execute(in Enemy enemy, Entity entity)
        {
            
            // Ok to compare local position because they're all at the root of the hierarchy
            float3 playerPos = LocalTransformLookup[enemy.Player].Position;
            float3 enemyPos = LocalTransformLookup[entity].Position;

            if (math.distancesq(playerPos, enemyPos) < math.lengthsq(enemy.TooCloseThreshold))
            {
                float3 dirPlayerToThis = math.normalize(enemyPos - playerPos);

                // TODO: Add move area limits

                RefRW<LocalTransform> enemyTransform = LocalTransformLookup.GetRefRW(entity);
                enemyTransform.ValueRW.Position += (dirPlayerToThis * enemy.Speed * DeltaTime);
                
            }
        }
    }


}
