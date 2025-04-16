using Unity.Entities;
using UnityEngine;

namespace ECS.Ball
{
    public class PlayerAuthoring : MonoBehaviour
    {

    }

    class PlayerBaker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            Player newPlayer = new Player();
            AddComponent(entity, newPlayer);

            Carry newCarry = new Carry();
            AddComponent(entity, newCarry);
            SetComponentEnabled<Carry>(entity, false);
        }
    }
}
