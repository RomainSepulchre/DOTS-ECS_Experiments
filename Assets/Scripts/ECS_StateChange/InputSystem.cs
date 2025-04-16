using Project.Utilities;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace ECS.StateChange
{
    public partial struct InputSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MouseHit>();
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<Exec_ECS_StateChange>();
        }

        // Can't burst compile because we access camera
        public void OnUpdate(ref SystemState state)
        {
            RefRW<MouseHit> hit = SystemAPI.GetSingletonRW<MouseHit>();
            hit.ValueRW.HitChanged = false;

            // Only continue if there is a camera and we left-click
            if (Camera.main == null || !Input.GetMouseButton(0)) 
            {
                return;
            }

            // Create a ray and see check it intersect with a XZ plane
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (new Plane(Vector3.up, 0f).Raycast(ray, out float hitDist))
            {
                hit.ValueRW.HitChanged = true;
                hit.ValueRW.HitPosition = ray.GetPoint(hitDist);
            }
        }
    }
}
