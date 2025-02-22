using Unity.Entities;
using UnityEngine;

namespace ECS.ECSExperiments
{
	public struct SpawnersManager : IComponentData
	{
		public int MaximumSpawnCount;
	}
}