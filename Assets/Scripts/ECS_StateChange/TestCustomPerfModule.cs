using UnityEngine;

namespace ECS.StateChange
{
    public class TestCustomPerfModule : MonoBehaviour
    {
        private int i = 0;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
            i++;
            GameStats.StateChangeCount.Value = i;
        }
    }
}
