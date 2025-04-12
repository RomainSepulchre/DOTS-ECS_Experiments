using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace ECS.RotatingCube
{
    public class ManagedCubeRotator : IComponentData
    {
        public GameObject Value;

        // Managed component types need a constructor
        public ManagedCubeRotator(GameObject value)
        {
            Value = value;
        }

        public ManagedCubeRotator()
        {
        }
    }
}
