using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Jobs.TargetAndSeekerDemo
{
    public class TargetAndSeekerManager : MonoBehaviour
    {
        public static TargetAndSeekerManager instance;

        public DemoMode demoMode;

        [SerializeField] private Target target;
        [SerializeField] private Seeker seeker;

        [SerializeField] private int numberOfTarget;
        [SerializeField] private int numberOfSeeker;

        [SerializeField] private float xAreaLimit;
        [SerializeField] private float zAreaLimit;

        public enum DemoMode
        {
            MainThread,
            SingleThreadJob,
            ParrellelJob,
            ParrallelJobOptimized
        }

        public static Transform[] targetsTransform;
        public static Transform[] seekersTransform;

        // Using Persistent allocated array instead TempJob allocated array doesn't seems to impact perf but I guess it's better to allocate the memory only once and dispose at on destroy rather than doing it every frame.
        NativeArray<float3> seekersPosArray;
        NativeArray<float3> targetsPosArray;
        NativeArray<float3> nearestPosArray;

        private void Awake()
        {
            // Initialize singleton
            if (instance != null)
            {
                Destroy(instance);
            }
            instance = this;

            // Initialize target and seeker Transform Array
            targetsTransform = new Transform[numberOfTarget];
            seekersTransform = new Transform[numberOfSeeker];

            // Initialize Native array
            seekersPosArray = new NativeArray<float3>(seekersTransform.Length, Allocator.Persistent);
            targetsPosArray = new NativeArray<float3>(targetsTransform.Length, Allocator.Persistent);
            nearestPosArray = new NativeArray<float3>(seekersTransform.Length, Allocator.Persistent);

            // Spawn targets and seekers
            SpawnTargets();
            SpawnSeekers();
        }

        private void LateUpdate()
        {
            switch (demoMode)
            {
                default:
                case DemoMode.MainThread:
                    break;

                case DemoMode.SingleThreadJob:
                    FindNearest_SingleThreadJob();
                    break;

                case DemoMode.ParrellelJob:
                    FindNearest_ParrallelJob();
                    break;

                case DemoMode.ParrallelJobOptimized:
                    FindNearest_ParrallelJobOptimized();
                    break;

                    // TODO: Use a IJobParralelForTransform to move target and seeker and see if there is a performance improvement
            }

        }

        private void OnDestroy()
        {
            seekersPosArray.Dispose();
            targetsPosArray.Dispose();
            nearestPosArray.Dispose();
        }

        private void SpawnTargets()
        {
            for (int i = 0; i < numberOfTarget; i++)
            {
                Vector3 spawnPos = new Vector3(Random.Range(-xAreaLimit, xAreaLimit), 0, Random.Range(-zAreaLimit, zAreaLimit));
                GameObject newTarget = Instantiate(target.gameObject, spawnPos, Quaternion.identity);
                targetsTransform[i] = newTarget.transform;
            }
        }

        private void SpawnSeekers()
        {
            for (int i = 0; i < numberOfSeeker; i++)
            {
                Vector3 spawnPos = new Vector3(Random.Range(-xAreaLimit, xAreaLimit), 0, Random.Range(-zAreaLimit, zAreaLimit));
                GameObject newSeeker = Instantiate(seeker.gameObject, spawnPos, Quaternion.identity);
                seekersTransform[i] = newSeeker.transform;
            }
        }

        private void FindNearest_SingleThreadJob()
        {
            // Fill Native float3 arrays with positions from the transform arrays
            for (int i = 0; i < seekersTransform.Length; i++)
            {
                seekersPosArray[i] = seekersTransform[i].position;
            }

            for (int i = 0; i < targetsTransform.Length; i++)
            {
                targetsPosArray[i] = targetsTransform[i].position;
            }

            FindNearestSingleThreadJob job = new FindNearestSingleThreadJob()
            {
                SeekersPos = seekersPosArray,
                TargetsPos = targetsPosArray,
                NearestPos = nearestPosArray
            };

            JobHandle handle = job.Schedule();

            handle.Complete();

            for (int i = 0; i < seekersPosArray.Length; i++)
            {
                Debug.DrawLine(seekersPosArray[i], nearestPosArray[i], Color.white);
            }
        }

        private void FindNearest_ParrallelJob()
        {
            // Fill Native float3 arrays with positions from the transform arrays
            for (int i = 0; i < seekersTransform.Length; i++)
            {
                seekersPosArray[i] = seekersTransform[i].position;
            }

            for (int i = 0; i < targetsTransform.Length; i++)
            {
                targetsPosArray[i] = targetsTransform[i].position;
            }

            FindNearestParrallelJob job = new FindNearestParrallelJob()
            {
                SeekersPos = seekersPosArray,
                TargetsPos = targetsPosArray,
                NearestPos = nearestPosArray
            };


            //int numBatches = Mathf.Max(1, Mathf.RoundToInt(JobsUtility.JobWorkerCount * 0.75f)); // Use 75 % of the worker thread
            int numBatches = Mathf.Max(1, JobsUtility.JobWorkerCount); // Use maximum of worker thread since i'm sure this won't block any other jobs
            int batchSize = seekersPosArray.Length / numBatches;

            JobHandle handle = job.Schedule(seekersPosArray.Length, batchSize);

            handle.Complete();

            for (int i = 0; i < seekersPosArray.Length; i++)
            {
                Debug.DrawLine(seekersPosArray[i], nearestPosArray[i], Color.white);
            }
        }

        private void FindNearest_ParrallelJobOptimized()
        {
            for (int i = 0; i < targetsTransform.Length; i++)
            {
                targetsPosArray[i] = targetsTransform[i].position;
            }

            SortJob<float3, XAxisComparer> sortJob = targetsPosArray.SortJob(new XAxisComparer { });
            JobHandle sortJobHandle = sortJob.Schedule();

            // Fill Native float3 arrays with positions from the transform arrays (do this on parralel of sortJob)
            for (int i = 0; i < seekersTransform.Length; i++)
            {
                seekersPosArray[i] = seekersTransform[i].position;
            }

            FindNearestOptimizedParrallelJob findNearestJob = new FindNearestOptimizedParrallelJob()
            {
                SeekersPos = seekersPosArray,
                TargetsPos = targetsPosArray,
                NearestPos = nearestPosArray
            };

            //int numBatches = Mathf.Max(1, Mathf.RoundToInt(JobsUtility.JobWorkerCount * 0.75f)); // Use 75 % of the worker thread
            int numBatches = Mathf.Max(1, JobsUtility.JobWorkerCount); // Use maximum of worker thread since i'm sure this won't block any other jobs
            int batchSize = seekersPosArray.Length / numBatches;

            JobHandle handle = findNearestJob.Schedule(seekersPosArray.Length, batchSize, sortJobHandle);

            handle.Complete();

            for (int i = 0; i < seekersPosArray.Length; i++)
            {
                Debug.DrawLine(seekersPosArray[i], nearestPosArray[i], Color.white);
            }
        }
    }
}
