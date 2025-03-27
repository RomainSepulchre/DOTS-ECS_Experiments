using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Jobs.SimpleJob
{
    // ! Jobs always must be a struct
    [BurstCompile]
    public struct SimpleJob : IJob // https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Unity.Jobs.IJob.html
    {
        // Jobs need to use unmanaged objects
        // Managed objects = Pure .Net code managed by runtime and under its controls, will be handled by the garbage collector
        // Unmanaged objects = Code that the garbage collector won't know how to manage
        // ex: here we use NativeArray (from Unity.Collections) instead of the classic System Array

        public NativeArray<int> Numbers;

        // Part of the IJob interface, function called to execute the job
        public void Execute()
        {
            // Square each entry of the array
            for (int i = 0; i < Numbers.Length; i++)
            {
                Numbers[i] *= Numbers[i];
            }
        }
    } 
}
