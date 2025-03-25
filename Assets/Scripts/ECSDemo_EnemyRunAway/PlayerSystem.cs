using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.EnemyRunAwayDemo
{
    public partial struct PlayerSystem : ISystem
    {
        SystemHandle inputSysHandle;
        EntityQuery playerQuery;

        // Cannot BurstCompile state.World.GetExistingSystem<InputSystem>()
        public void OnCreate(ref SystemState state)
        {
            inputSysHandle = state.World.GetExistingSystem<InputSystem>();
            playerQuery = SystemAPI.QueryBuilder().WithAllRW<LocalTransform>().WithAll<Player>().Build();
            state.RequireForUpdate(playerQuery); // Only update if there is at least one Entity with localTransform and player component
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            PlayerInput input = SystemAPI.GetComponent<PlayerInput>(inputSysHandle);

            MovePlayer movePlayerJob = new MovePlayer()
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Input = input,
            };

            //Schedule the job and reassign system dependency
            state.Dependency = movePlayerJob.Schedule(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct MovePlayer : IJobEntity
    {
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public PlayerInput Input;

        public void Execute(ref LocalTransform transform, in Player player)
        {
            float upAxis = (Input.UpArrowPressed ? 1 : 0 ) - (Input.DownArrowPressed ? 1 : 0);
            float rightAxis = (Input.RightArrowPressed? 1 : 0 ) - (Input.LeftArrowPressed ? 1 : 0);

            float3 moveDirection = new float3(rightAxis, upAxis, 0);
            if(upAxis != 0 && rightAxis != 0) moveDirection = math.normalize(moveDirection);

            transform.Position += (moveDirection * DeltaTime * player.Speed);
        }
    }
}