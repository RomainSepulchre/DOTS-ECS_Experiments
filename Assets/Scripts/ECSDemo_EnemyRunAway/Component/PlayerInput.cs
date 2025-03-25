using Unity.Entities;

namespace ECS.EnemyRunAwayDemo
{
    public struct PlayerInput : IComponentData
    {
        public bool UpArrowPressed;
        public bool DownArrowPressed;
        public bool LeftArrowPressed;
        public bool RightArrowPressed;
    } 
}
