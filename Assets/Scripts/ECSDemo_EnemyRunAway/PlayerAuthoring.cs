using Unity.Entities;
using UnityEngine;

namespace ECS.EnemyRunAwayDemo
{
    public class PlayerAuthoring : MonoBehaviour
    {
        
    }

    public class PlayerBaker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            throw new System.NotImplementedException();
        }
    }
}
