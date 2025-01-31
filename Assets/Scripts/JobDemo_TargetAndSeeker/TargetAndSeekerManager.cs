using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

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

        // Spawn targets and seekers
        SpawnTargets();
        SpawnSeekers();
    }

    //private void Start()
    //{
    //    TestBinarySearch();
    //}

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
        }
        
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
        NativeArray<float3> seekersPosArray = new NativeArray<float3>(seekersTransform.Length, Allocator.TempJob);
        NativeArray<float3> targetsPosArray = new NativeArray<float3>(targetsTransform.Length, Allocator.TempJob);
        NativeArray<float3> nearestPosArray = new NativeArray<float3>(seekersTransform.Length, Allocator.TempJob);

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

        seekersPosArray.Dispose();
        targetsPosArray.Dispose();
        nearestPosArray.Dispose();
    }

    private void FindNearest_ParrallelJob()
    {
        NativeArray<float3> seekersPosArray = new NativeArray<float3>(seekersTransform.Length, Allocator.TempJob);
        NativeArray<float3> targetsPosArray = new NativeArray<float3>(targetsTransform.Length, Allocator.TempJob);
        NativeArray<float3> nearestPosArray = new NativeArray<float3>(seekersTransform.Length, Allocator.TempJob);

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

        seekersPosArray.Dispose();
        targetsPosArray.Dispose();
        nearestPosArray.Dispose();
    }

    private void FindNearest_ParrallelJobOptimized()
    {
        NativeArray<float3> targetsPosArray = new NativeArray<float3>(targetsTransform.Length, Allocator.TempJob);

        for (int i = 0; i < targetsTransform.Length; i++)
        {
            targetsPosArray[i] = targetsTransform[i].position;
        }

        SortJob<float3, XAxisComparer> sortJob = targetsPosArray.SortJob(new XAxisComparer { });
        JobHandle sortJobHandle = sortJob.Schedule();

        NativeArray<float3> seekersPosArray = new NativeArray<float3>(seekersTransform.Length, Allocator.TempJob);
        NativeArray<float3> nearestPosArray = new NativeArray<float3>(seekersTransform.Length, Allocator.TempJob);

        // Fill Native float3 arrays with positions from the transform arrays
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

        seekersPosArray.Dispose();
        targetsPosArray.Dispose();
        nearestPosArray.Dispose();
    }

    private void TestBinarySearch()
    {
        float3 seekerPos = new float3(-6.3f, 0, 1.3f);
        Debug.Log($"SeekerPos:{seekerPos}");

        NativeArray<float3> testTargetArray = new NativeArray<float3>(8, Allocator.TempJob);
        
        testTargetArray[0] = new float3(-1f, 0, 5f);
        testTargetArray[1] = new float3(-4f, 0, -2f);
        testTargetArray[2] = new float3(-5f, 0, 3f);
        testTargetArray[3] = new float3(4f, 0, -3f);
        testTargetArray[4] = new float3(2f, 0, 0);
        testTargetArray[5] = new float3(3f, 0, -1f);
        testTargetArray[6] = new float3(-2f, 0, 1f);
        testTargetArray[7] = new float3(5f, 0, 2f);

        string items = "";
        int index = 0;
        foreach (var item in testTargetArray)
        {
            items += $"{index}-{item}, ";
            index++;
        }
        Debug.Log($"testTargetArray BEFORE sort: [{items}]");

        SortJob<float3, XAxisComparer> sortJob = testTargetArray.SortJob(new XAxisComparer { });
        JobHandle sortJobHandle = sortJob.Schedule();

        sortJobHandle.Complete();

        items = "";
        index = 0;
        foreach (var item in testTargetArray)
        {
            items += $"{index}-{item}, ";
            index++;
        }
        Debug.Log($"testTargetArray AFTER sort: [{items}]");

        

        int startIndex = testTargetArray.BinarySearch(seekerPos, new XAxisComparer { });

        Debug.Log($"Binary search start index: {startIndex}");
        if (startIndex < 0)
        {
            startIndex = ~startIndex;
            Debug.Log($"Binary search - index < 0 so we flip the bit: {startIndex}");
        }
        if (startIndex >= testTargetArray.Length)
        {
            startIndex = testTargetArray.Length - 1;
            Debug.Log($"Binary search - index > array length so set it to last array index: {startIndex}");
        }

        float3 nearestTargetPos = testTargetArray[startIndex];
        float distWithNearest = math.distance(seekerPos, nearestTargetPos);

        Debug.Log($"Default index={startIndex}, nearPos={nearestTargetPos}, dist with nearest={distWithNearest}");

        // Search upward to find a closer target
        for (int i = startIndex+1; i < testTargetArray.Length; i++)
        {
            float3 targetPos = testTargetArray[i];
            float distWithTarget = math.distance(seekerPos, targetPos);

            Debug.Log($"Search index={i}, targetPos={targetPos}, dist with target={distWithTarget}");

            // If the square of the x distance is greater than the current nearest, we can stop searching.
            float xDist = seekerPos.x - targetPos.x;
            if (xDist < 0) xDist *= -1;
            Debug.Log($"Search index={i}, Test X distance: xDist={xDist}, dist with nearest={distWithNearest}"); 
            if (xDist > distWithNearest)
            {
                Debug.Log($"Search index={i}, xDist > distWithNearest, break we're getting away");
                break;
            }

            if (distWithTarget < distWithNearest)
            {
                distWithNearest = distWithTarget;
                nearestTargetPos = targetPos;
                Debug.Log($"Found new shorter dist at index={i}, nearest={nearestTargetPos}, dist with nearest={distWithNearest}");

            }
        }

        // Search downward to find a closer target
        for (int i = startIndex-1; i >= 0; i--)
        {
            float3 targetPos = testTargetArray[i];
            float distWithTarget = math.distance(seekerPos, targetPos);

            Debug.Log($"Search index={i}, targetPos={targetPos}, dist with target={distWithTarget}");

            // If the square of the x distance is greater than the current nearest, we can stop searching.
            float xDist = seekerPos.x - targetPos.x;
            if (xDist < 0) xDist *= -1;
            Debug.Log($"Search index={i}, Test X distance: xDist={xDist}, dist with nearest={distWithNearest}");
            if (xDist > distWithNearest)
            {
                Debug.Log($"Search index={i}, xDist > distWithNearest, break we're getting away");
                break;
            }

            if (distWithTarget < distWithNearest)
            {
                distWithNearest = distWithTarget;
                nearestTargetPos = targetPos;
                Debug.Log($"Found new shorter dist at index={i}, nearest={nearestTargetPos}, dist with nearest={distWithNearest}");
            }
        }

        Debug.Log($"FINAL NO JOB nearest found: pos={nearestTargetPos}, dist={distWithNearest}");

        NativeArray<float3> testSeekerArray = new NativeArray<float3>(1, Allocator.TempJob);
        NativeArray<float3> testNearestArray = new NativeArray<float3>(1, Allocator.TempJob);

        testSeekerArray[0] = seekerPos;

        FindNearestOptimizedParrallelJob findNearestJob = new FindNearestOptimizedParrallelJob()
        {
            SeekersPos = testSeekerArray,
            TargetsPos = testTargetArray,
            NearestPos = testNearestArray
        };

        JobHandle handle = findNearestJob.Schedule(testSeekerArray.Length, 1, sortJobHandle);

        handle.Complete();

        Debug.Log($"FINAL WITH JOB nearest found: pos={testNearestArray[0]})");

        testTargetArray.Dispose();
        testSeekerArray.Dispose();
        testNearestArray.Dispose();
    }

}
