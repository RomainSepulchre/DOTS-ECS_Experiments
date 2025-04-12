using DOTS.Utilities;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace ECS.RotatingCube
{
    public partial struct ReparentingSystem : ISystem
    {
        bool attached;
        float timer;
        const float INTERVAL = 1f;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            timer = INTERVAL;
            attached = true;
            state.RequireForUpdate<Exec_ECS_RotatingCube>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            timer -= SystemAPI.Time.DeltaTime;
            if (timer <= 0)
            {
                timer = INTERVAL;

                Entity parentCube = SystemAPI.GetSingletonEntity<ParentCube>();

                // Use ECB because we can't add or remove components (=structural changes) while iterating over entities
                EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

                if(attached)
                {
                    DynamicBuffer<Child> children = SystemAPI.GetBuffer<Child>(parentCube);
                    for (int i = 0; i < children.Length; i++)
                    {
                        ecb.RemoveComponent<Parent>(children[i].Value);
                    }
                }
                else
                {
                    foreach (var (transform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithNone<ParentCube, ManagedCubeRotator>().WithEntityAccess())
                    {
                        ecb.AddComponent(entity, new Parent { Value = parentCube });
                    }
                }

                ecb.Playback(state.EntityManager);
                attached = !attached;
            }
            else return;
            
        }
    }
}
