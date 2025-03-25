using Unity.Entities;

namespace ECS.EnemyRunAwayDemo
{
    public struct Enemy : IComponentData
    {
        public Entity Player;
        public float Speed; // TODO: Should I Create a speed component and use it for player and enemies ?
        public float TooCloseThreshold;
        public float XAreaLimit;
        public float YAreaLimit;
    } 
}
