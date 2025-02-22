using System.ComponentModel.Design;
using Unity.Entities;
using UnityEngine;

namespace ECS.ECSExperiments
{
	public struct MoveableSphere : IComponentData
	{
		public float Speed;
	}

}