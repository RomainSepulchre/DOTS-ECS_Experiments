using Unity.Entities;
using UnityEngine;

namespace ECS.StateChange
{
    public class ConfigAuthoring : MonoBehaviour
    {
        public GameObject prefab;
        public uint size;
        public float radius;
        public Mode mode;
    }

    class ConfigBaker : Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            Config newConfig = new Config()
            {
                Prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic),
                Size = authoring.size,
                Radius = authoring.radius,
                Mode = authoring.mode,
            };
            AddComponent(entity, newConfig);

            MouseHit newMouseHit = new MouseHit();
            AddComponent(entity, newMouseHit);
        }
    }

    public enum Mode
    {
        Value = 1,
        StructuralChange = 2,
        EnableableComponent = 3
    }
}
