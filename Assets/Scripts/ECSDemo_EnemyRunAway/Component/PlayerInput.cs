using Unity.Entities;
using UnityEngine;

namespace ECS.EnemyRunAwayDemo
{
    public class PlayerInput : IComponentData
    {
        public bool UpArrowPressed;
        public bool DownArrowPressed;
        public bool LeftArrowPressed;
        public bool RightArrowPressed;
    } 
}
