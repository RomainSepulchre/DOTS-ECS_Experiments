using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

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
            foreach( var (lastPosBuffer, transform) in SystemAPI.Query<DynamicBuffer<LastPositions>, RefRO<LocalTransform>>())
            {
                if(lastPosBuffer.Length == lastPosBuffer.Capacity)
                {
                    lastPosBuffer.RemoveAt(0);
                }
                lastPosBuffer.Add(new LastPositions { Position = transform.ValueRO.Position });

                float3 currentIndexPos;
                for (var i = lastPosBuffer.Length - 1; i > 9; i -= 10) // i > 9 and i -= 10 to draw line between every 10th position
                {
                    // i/length (100 -> 0)
                    byte alpha = (byte)math.lerp(0, 255, (float)i/lastPosBuffer.Length);
                    currentIndexPos = lastPosBuffer[i].Position;
                    float3 nextPos = lastPosBuffer[i - 10].Position;
                    Debug.DrawLine(currentIndexPos, nextPos, new Color32 (255,128,0, alpha));
                }
            }
        }
    }
}
