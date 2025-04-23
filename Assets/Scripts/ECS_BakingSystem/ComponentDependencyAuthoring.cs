using Unity.Entities;
using UnityEngine;

namespace ECS.BakingSystem
{
    public class ComponentDependencyAuthoring : MonoBehaviour
    {
        [TextArea(1, 10)]
        public string testDescription = "What to test ? Enable/Disable dependency and test the following to see how the baker behave with and without dependency:" +
            "\n\n- Delete/Restore gameObject assigned in the field and see if the baker re-runs by checking logs." +
            "\n- Add/Remove Collider component on the gameObjects assigned in the field and see if the baker re-runs by checking logs.";

        [Space(10)]
        public bool useDependency;

        [Space(10)]
        public GameObject go;
    }

    class ComponentDependencyBaker : Baker<ComponentDependencyAuthoring>
    {
        public override void Bake(ComponentDependencyAuthoring authoring)
        {
            if(authoring.useDependency) DependsOn(authoring.go);

            if (authoring.go == null)
            {
                Debug.LogError($"Baking component dependency: null gameObject");
                return;
            }
            
            Collider collider = GetComponent<Collider>(authoring.go);
            if (collider == null)
            {
                Debug.Log($"Baking component dependency: no collider component on gameObject");
                return;
            }
            else
            {
                Debug.Log($"Baking component dependency: All good!");
            }

            Entity entity = GetEntity(TransformUsageFlags.None);
        }
    }
}
