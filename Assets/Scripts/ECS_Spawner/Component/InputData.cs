using Unity.Entities;
using UnityEngine;

namespace ECS.ECSExperiments
{
    public struct InputData : IComponentData
    {
        public bool UpKeyPressed;
        public bool DownKeyPressed;
        public bool LeftKeyPressed;
        public bool RightKeyPressed;
        public bool SpaceKeyPressed;
        public bool RCtrlKeyPressed;
    } 
}
