using Unity.Entities;
using UnityEngine;

namespace ECS.Ball
{
    public struct Carried : IComponentData, IEnableableComponent
    {
        public Entity CarriedByEntity;
    }
}
