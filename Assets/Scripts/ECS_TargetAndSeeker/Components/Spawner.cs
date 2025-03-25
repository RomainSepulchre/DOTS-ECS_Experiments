using Unity.Entities;

namespace ECS.TargetAndSeekerDemo
{
	public struct Spawner : IComponentData, IEnableableComponent
	{
		public Entity EntityToSpawn;
        public int SpawnAmount;
        public float XSpawnAreaLimit;
        public float ZSpawnAreaLimit;
        public uint RandomSeed;
    }
}
