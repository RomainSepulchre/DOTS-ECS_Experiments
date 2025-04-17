using Unity.Entities;
using UnityEngine;

namespace ECS.Ball
{
    public struct Carry : IComponentData, IEnableableComponent
    {
        public Entity EntityCarried;
    }
}
