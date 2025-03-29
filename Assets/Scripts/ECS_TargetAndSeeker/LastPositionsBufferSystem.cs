using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;

namespace ECS.TargetAndSeekerDemo
{
    [UpdateAfter(typeof(MovementSystem))] // Update after movement to haave last updated position
    [UpdateAfter(typeof(DrawDebugLineSystem))] // Update after all others systems to prevent sync point
    public partial struct LastPositionsBufferSystem : ISystem
    {
        EntityQuery lastPosBufferQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // In Query builder we can require the buffer by its component name
            lastPosBufferQuery = SystemAPI.QueryBuilder().WithAllRW<LastPositions>().WithAll<LocalTransform>().Build();
            state.RequireForUpdate(lastPosBufferQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Writing on dynamic buffer is a structural change it can only be done on the main thread or in an EntityCommandBuffer
            // -> Test entity command buffer and compare speed with main thread
            foreach( var (lastPosBuffer, transform) in SystemAPI.Query<DynamicBuffer<LastPositions>, RefRO<LocalTransform>>())
            {
                if(lastPosBuffer.Length == lastPosBuffer.Capacity)
                {
                    lastPosBuffer.RemoveAt(0);
                }
                lastPosBuffer.Add(new LastPositions { Position = transform.ValueRO.Position });

                // Draw line on the main thread (way slower than parallel job)
                //float3 currentIndexPos;
                //for (var i = lastPosBuffer.Length - 1; i > 9; i -= 10) // i > 9 and i -= 10 to draw line between every 10th position
                //{
                //    // i/length (100 -> 0)
                //    byte alpha = (byte)math.lerp(0, 255, (float)i / lastPosBuffer.Length);
                //    currentIndexPos = lastPosBuffer[i].Position;
                //    float3 nextPos = lastPosBuffer[i - 10].Position;
                //    Debug.DrawLine(currentIndexPos, nextPos, new Color32(255, 128, 0, alpha));
                //}
            }

            DrawMovementLineJob drawLineJob = new DrawMovementLineJob()
            {
                LastPosBufferLookup = SystemAPI.GetBufferLookup<LastPositions>()
            };
            state.Dependency = drawLineJob.ScheduleParallel(lastPosBufferQuery, state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct DrawMovementLineJob : IJobEntity
    {
        [ReadOnly] public BufferLookup<LastPositions> LastPosBufferLookup;

        public void Execute(Entity entity)
        {
            float3 currentIndexPos;
            DynamicBuffer<LastPositions> lastPosBuffer = LastPosBufferLookup[entity];

            int positionStep = 20; // draw a line every 20th position
            for (var i = lastPosBuffer.Length - 1; i > positionStep - 1; i -= positionStep) 
            {
                byte alpha = (byte)math.lerp(0, 255, (float)i / lastPosBuffer.Length);
                currentIndexPos = lastPosBuffer[i].Position;
                float3 nextPos = lastPosBuffer[i - positionStep].Position;

                // Debug.DrawLine works in a burst compiled job but the rendering is blinking when I draw more than a specific number of line
                Debug.DrawLine(currentIndexPos, nextPos, new Color32(255, 128, 0, alpha));
            }
        }
    }

}
