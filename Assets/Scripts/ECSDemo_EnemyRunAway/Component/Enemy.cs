using Unity.Entities;
using UnityEngine;

namespace ECS.EnemyRunAwayDemo
{
    public struct Enemy : IComponentData
    {
        public Entity Player;
        public float Speed;
        public float TooCloseThreshold;
        public float XAreaLimit;
        public float YAreaLimit;
    } 
}
