using Unity.Entities;
using Unity.Mathematics;

namespace ECS.TargetAndSeekerDemo
{
	public struct Seeker : IComponentData
	{
		public Entity NearestTarget;
        public float3 NearestTargetPos;
    } 
}
