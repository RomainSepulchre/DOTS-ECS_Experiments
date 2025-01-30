using Unity.Burst;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

[BurstCompile]
public struct EnemyBehaviorSingleJob : IJob
{
    public NativeArray<float3> Position;

    [ReadOnly] public float3 PlayerPosition;
    [ReadOnly] public float Speed;
    [ReadOnly] public float TooCloseDistance;
    [ReadOnly] public float XAreaLimit;
    [ReadOnly] public float YAreaLimit;
    [ReadOnly] public float DeltaTime;

    public void Execute()
    {
        float3 dirPlayerToThis =  math.normalize(Position[0] - PlayerPosition);

        bool playerLowerThanThis = PlayerPosition.y < Position[0].y;
        bool playerMoreLeftThanThis = PlayerPosition.x < Position[0].x;

        if (math.distance(Position[0], PlayerPosition) < TooCloseDistance)
        {
            float3 positionPrevision = Position[0] + (dirPlayerToThis / 2);

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
                    direction = GetPerpendicularClockwiseVector(dirPlayerToThis);
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
                    direction = GetPerpendicularClockwiseVector(dirPlayerToThis);
                }
            }
            else if (positionPrevision.y <= -YAreaLimit) // Reach limit on the bottom
            {
                if (playerMoreLeftThanThis) // Player is at our left so we need to go counter clockwise
                {
                    direction = GetPerpendicularClockwiseVector(dirPlayerToThis);
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

            float3 nextPosition = Position[0] + (direction * Speed * DeltaTime);
            Position[0] = nextPosition;
        }
        //else if (Vector3.Distance(position[0], PlayerPosition) > 8f)
        //{
        //    float3 dirThisToPlayer = math.normalize(PlayerPosition - position[0]);
        //    float3 nextPosition = position[0] + (dirThisToPlayer * Speed * DeltaTime);
        //    position[0] = nextPosition;
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
