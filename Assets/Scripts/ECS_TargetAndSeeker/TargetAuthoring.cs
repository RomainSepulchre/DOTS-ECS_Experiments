using Unity.Entities;
using UnityEngine;

namespace ECS.TargetAndSeekerDemo
{
	public class TargetAuthoring : MonoBehaviour
	{

	}

    class TargetBaker : Baker<TargetAuthoring>
    {
        public override void Bake(TargetAuthoring authoring)
        {
            throw new System.NotImplementedException();
        }
    }
}
