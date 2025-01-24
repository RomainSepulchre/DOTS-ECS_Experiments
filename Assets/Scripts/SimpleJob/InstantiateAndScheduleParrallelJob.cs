using Unity.Collections;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using UnityEngine;

public class InstantiateAndScheduleParrallelJob : MonoBehaviour
{
    void Update()
    {
        // Create a native array for our job
        var intArray = new NativeArray<int>(10000, Allocator.TempJob); // Allocator see https://docs.unity3d.com/Packages/com.unity.collections@2.5/manual/allocator-overview.html
        var jobGenerateArray = new GenerateIntArrayJob { StartNumber = 1, IntArray = intArray };
        JobHandle handleGenerateArray = jobGenerateArray.Schedule();

        var job = new ParrallelJob { Numbers = intArray };

        // To schedule a IJobParallelFor we need to pass the length of the array and a batch size
        // First parameter = length of the array (how many for-each iterations to perform)
        // Second parameter = batch size, how many item will there be in one batch (every batch will be able to run on a different worker)
        // -> Ex: if we have 246 items, and set a batch size of 100, all items will be processed in 3 batches (batch 1: 0-99, batch 2: 100-199, batch 3: 200-246)
        // -> essentially the no - overhead innerloop that just invokes Execute(i) in a loop.
        // -> When there is a lot of work in each iteration then a value of 1 can be sensible.
        // -> When there is very little work values of 32 or 64 can make sense.
        // => It is needed to experiment and profile to find the best batch size in term of performance
        //    because with simple computation the bottleneck could be memory access (https://youtu.be/jdW66hA-Qu8?si=l12_EYkXmcGaDnl2&t=610)
        JobHandle handle = job.Schedule(intArray.Length, 64, handleGenerateArray);

        // Still need to complete job
        handle.Complete();

        // We need to dispose of intArray manually since it's a native Array
        intArray.Dispose();
    }

}
