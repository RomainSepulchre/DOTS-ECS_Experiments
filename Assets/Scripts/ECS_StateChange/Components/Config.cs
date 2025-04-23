using Unity.Entities;
using UnityEngine;

namespace ECS.StateChange
{
    public struct Config : IComponentData
    {
        public Entity Prefab;
        public uint Size;
        public float Radius;
        public Mode Mode;

        public bool ChangeMaterial;
        public Entity ObjWithInitialMat;
        public Entity ObjWithNewMat;
    }
}
