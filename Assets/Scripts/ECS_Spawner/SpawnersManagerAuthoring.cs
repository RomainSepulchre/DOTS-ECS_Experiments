using Unity.Entities;
using UnityEngine;

namespace ECS.ECSExperiments
{
	public class SpawnersManagerAuthoring : MonoBehaviour
	{
		public int maximumSpawnCount;
	}

	class SpawnersManagerBaker : Baker<SpawnersManagerAuthoring>
	{
        public override void Bake(SpawnersManagerAuthoring authoring)
		{
            var entity = GetEntity(TransformUsageFlags.None);

            SpawnersManager spawnsManager = new SpawnersManager
			{
				MaximumSpawnCount = authoring.maximumSpawnCount
			};

			AddComponent(entity, spawnsManager);
		}
    }
}


