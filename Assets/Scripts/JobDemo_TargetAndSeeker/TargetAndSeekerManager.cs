using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TargetAndSeekerManager : MonoBehaviour
{
    public static TargetAndSeekerManager instance;

    [SerializeField] private Target target;
    [SerializeField] private Seeker seeker;

    [SerializeField] private int numberOfTarget;
    [SerializeField] private int numberOfSeeker;

    [SerializeField] private float xAreaLimit;
    [SerializeField] private float zAreaLimit;

    public static Transform[] targetsTransform;

    private void Awake()
    {
        // Initialize singleton
        if (instance != null)
        {
            Destroy(instance);
        }
        instance = this;

        // Initialize targetsTransform Array
        targetsTransform = new Transform[numberOfTarget];

        // Spawn targets and seekers
        SpawnTargets();
        SpawnSeekers();
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
            Instantiate(seeker.gameObject, spawnPos, Quaternion.identity);
        }
    }
}
