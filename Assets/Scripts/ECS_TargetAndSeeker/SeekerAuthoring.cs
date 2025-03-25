using Unity.Entities;
using UnityEngine;

namespace ECS.TargetAndSeekerDemo
{
	public class SeekerAuthoring : MonoBehaviour
	{

	}

    class SeekerBaker : Baker<SeekerAuthoring>
    {
        public override void Bake(SeekerAuthoring authoring)
        {
            throw new System.NotImplementedException();
        }
    }

}