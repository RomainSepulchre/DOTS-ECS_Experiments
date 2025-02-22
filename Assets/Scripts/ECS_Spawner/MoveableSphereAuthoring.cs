using ECS.ECSExperiments;
using Unity.Entities;
using UnityEngine;

namespace ECS.ECSExperiments
{
    public class MoveableSphereAuthoring : MonoBehaviour
    {
        public float speed;
    }

    class MoveableSphereBaker : Baker<MoveableSphereAuthoring>
    {
        public override void Bake(MoveableSphereAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new MoveableSphere
            {
                Speed = authoring.speed
            });
        }
    } 
}
