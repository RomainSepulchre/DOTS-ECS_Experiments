using Unity.Entities;
using UnityEngine;

namespace ECS.StateChange
{
    public struct Spin : IComponentData, IEnableableComponent
    {
        public bool IsSpinning;
    }
}
