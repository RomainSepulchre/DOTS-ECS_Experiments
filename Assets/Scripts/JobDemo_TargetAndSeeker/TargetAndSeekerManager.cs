using NUnit.Framework;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
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
                break;

            case DemoMode.ParrallelJobOptimized:
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
}
