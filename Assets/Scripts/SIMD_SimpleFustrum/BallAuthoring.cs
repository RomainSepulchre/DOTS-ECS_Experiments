using Unity.Entities;
using UnityEngine;
using Unity.Rendering;
using Unity.Mathematics;

namespace Burst.SIMD.SimpleFustrum
{
    public class BallAuthoring : MonoBehaviour
    {
        public MeshRenderer sphereRenderer;
        public SphereCollider sphereCollider;
    }

    public class BallBaker : Baker<BallAuthoring>
    {
        public override void Bake(BallAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            SphereRadius newRadius = new SphereRadius()
            {
                Value = authoring.sphereCollider.radius
            };
            AddComponent(entity, newRadius);

            SphereVisible newVisible = new SphereVisible()
            {
                Value = authoring.sphereRenderer.enabled ? 1 : 0
            };
            AddComponent(entity, newVisible);

            HDRPMaterialPropertyBaseColor baseColor = new HDRPMaterialPropertyBaseColor()
            {
                Value = new float4(0.5f, 0.5f, 0.5f, 1f)
            };
            AddComponent(entity, baseColor);
        }
    }
}
