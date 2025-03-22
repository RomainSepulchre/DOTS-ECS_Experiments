using Unity.Burst;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

namespace Jobs.EnemyRunAwayDemo
{
    [BurstCompile]
    public struct EnemiesBehaviorJob : IJobParallelForTransform
    {
        [ReadOnly] public float3 PlayerPosition;
        [ReadOnly] public float Speed;
        [ReadOnly] public float TooCloseDistance;
        [ReadOnly] public float XAreaLimit;
        [ReadOnly] public float YAreaLimit;
        [ReadOnly] public float DeltaTime;

        public void Execute(int index, TransformAccess transform)
        {
            float3 dirPlayerToThis = math.normalize((float3)transform.position - PlayerPosition);

            bool playerLowerThanThis = PlayerPosition.y < transform.position.y;
            bool playerMoreLeftThanThis = PlayerPosition.x < transform.position.x;

            if (math.distance(transform.position, PlayerPosition) < TooCloseDistance)
            {
                float3 positionPrevision = (float3)transform.position + (dirPlayerToThis / 2);

                float3 direction;

                if (positionPrevision.x >= XAreaLimit) // Reach limit on the right
                {
                    if (playerLowerThanThis) // Player is lower so we need to go counter clockwise
                    {
                        direction = GetPerpendicularCounterClockwiseVector(dirPlayerToThis);
                    }
                    else // Player is higher so we need to go clockwise
                    {
                        direction = GetPerpendicularClockwiseVector(dirPlayerToThis);
                    }
                }
                else if (positionPrevision.x <= -XAreaLimit) // Reach limit on the left
                {
                    if (playerLowerThanThis) // Player is lower so we need to go clockwise
                    {
                        direction = GetPerpendicularClockwiseVector(dirPlayerToThis);
                    }
                    else // Player is higher so we need to go counter clockwise
                    {
                        direction = GetPerpendicularCounterClockwiseVector(dirPlayerToThis);
                    }
                }
                else if (positionPrevision.y >= YAreaLimit) // Reach limit on the top
                {
                    if (playerMoreLeftThanThis) // Player is at our left so we need to go clockwise
                    {
                        direction = GetPerpendicularClockwiseVector(dirPlayerToThis);
                    }
                    else // Player is at our right so we need to go counter clockwise
                    {
                        direction = GetPerpendicularCounterClockwiseVector(dirPlayerToThis);
                    }
                }
                else if (positionPrevision.y <= -YAreaLimit) // Reach limit on the bottom
                {
                    if (playerMoreLeftThanThis) // Player is at our left so we need to go counter clockwise
                    {
                        direction = GetPerpendicularCounterClockwiseVector(dirPlayerToThis);
                    }
                    else // Player is at our right so we need to go clockwise
                    {
                        direction = GetPerpendicularClockwiseVector(dirPlayerToThis);
                    }
                }
                else
                {
                    direction = dirPlayerToThis;
                }

                float3 nextPosition = (float3)transform.position + (direction * Speed * DeltaTime);
                transform.position = nextPosition;
            }
            //else if (Vector3.Distance(transform.position, PlayerPosition) > 8f)
            //{
            //    float3 dirThisToPlayer = math.normalize(PlayerPosition - (float3)transform.position);
            //    float3 nextPosition = (float3)transform.position + (dirThisToPlayer * Speed * DeltaTime);
            //    transform.position = nextPosition;
            //}
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