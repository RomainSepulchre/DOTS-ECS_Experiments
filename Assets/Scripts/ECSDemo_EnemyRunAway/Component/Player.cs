using Unity.Entities;

namespace ECS.EnemyRunAwayDemo
{
    public struct Player : IComponentData
    {
        public float Speed; // TODO: Should I Create a speed component and use it for player and enemies ?
    } 
}
