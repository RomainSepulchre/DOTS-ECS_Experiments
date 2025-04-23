using NUnit.Framework;
using Unity.Entities;
using UnityEditor.Experimental.GraphView;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

namespace ECS.BakingSystem
{
    public class BakingDependencyAuthoring : MonoBehaviour
    {
        [TextArea(1, 10)]
        public string testDescription = "When using dependency, baking will re-run when changing mesh on the linked mesh filter component otherwise it will only run if a field on the authoring component has a change." +
            "\n\nHow to test ?" +
            "\n\nChange the mesh assigned in the field and the mesh assigned in the component to see if a new component SameMeshInBothFields is added when both mesh are the same.";

        [Space(10)]
        public bool useBakingDependency;

        [Space(10)]
        public MeshFilter meshFilter;
        public Mesh mesh;
    }

class BakingDependencyBaker : Baker<BakingDependencyAuthoring>
{
    public override void Bake(BakingDependencyAuthoring authoring)
    {
        Entity entity =  GetEntity(TransformUsageFlags.None);
        if(authoring.useBakingDependency)
        {
            // By making the baker dependant of mesh filter, the baker tracks the data of the mesh filter
            // and also re-run the baking when we change the mesh on the component
                DependsOn(authoring.meshFilter);

                if (authoring.meshFilter.sharedMesh == authoring.mesh)
                {
                    AddComponent<SameMeshInBothFields>(entity);
                }
            }
            else
            {
                // With this BakingDependencyTest re-run baking only when meshFilter or mesh field is updated
                // But it doesn't update if I change the mesh assigned on the mesh filter component linked in the field
                if (authoring.meshFilter.sharedMesh == authoring.mesh)
                {
                    AddComponent<SameMeshInBothFields>(entity);
                }
            }
        }
    }
}
