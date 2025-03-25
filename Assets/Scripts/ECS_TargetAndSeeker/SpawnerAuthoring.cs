using Unity.Entities;
using UnityEngine;

namespace ECS.TargetAndSeekerDemo
{
	public class SpawnerAuthoring : MonoBehaviour
	{

	}

    class SpawnerBaker : Baker<SpawnerAuthoring>
    {
        public override void Bake(SpawnerAuthoring authoring)
        {
            throw new System.NotImplementedException();
        }
    }
}
