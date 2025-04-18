using Unity.Entities;
using UnityEngine;

namespace ECS.BakingSystem
{
    public class CubeAuthoring : MonoBehaviour
    {

    }

    class CubeBaker : Baker<CubeAuthoring>
    {
        public override void Bake(CubeAuthoring authoring)
        {
            Entity entity =  GetEntity(TransformUsageFlags.Dynamic);

            AddComponent<Cube>(entity);

        }
    }
}
