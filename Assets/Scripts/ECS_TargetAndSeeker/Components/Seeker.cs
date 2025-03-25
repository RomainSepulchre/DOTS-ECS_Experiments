using Unity.Entities;
using Unity.Mathematics;

namespace ECS.TargetAndSeekerDemo
{
	public struct Seeker : IComponentData
	{
		//public Entity NearestTarget; // TODO: find a way to also keep entity updated with binary search
        public float3 NearestTargetPos;
    } 
}
