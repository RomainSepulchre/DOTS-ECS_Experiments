using Unity.Entities;

namespace ECS.TargetAndSeekerDemo
{
	public struct Spawner : IComponentData
	{
		public Entity EntityToSpawn;
        public float XSpawnAreaLimit;
        public float ZSpawnAreaLimit;
    }
}
