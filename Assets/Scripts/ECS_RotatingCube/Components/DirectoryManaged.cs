using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace ECS.RotatingCube
{
    public class DirectoryManaged : IComponentData
    {
        public GameObject RotatingCube;
        public Toggle RotationToggle;

        // Managed component types need a constructor
        public DirectoryManaged()
        {
        }
    }
}
