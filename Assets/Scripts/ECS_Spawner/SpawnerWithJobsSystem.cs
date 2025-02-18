using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace ECS.ECSExperiments
{
    public partial struct SpawnerWithJobsSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //SpawnCubeJob spawnJob = new SpawnCubeJob
            //{
            //    ElaspedTime = SystemAPI.Time.ElapsedTime
            //};
        }
    }

    [BurstCompile]
    public partial struct SpawnCubeJob : IJobEntity
    {
        public double ElaspedTime;

        public void Execute()
        {

        }
    }
}



