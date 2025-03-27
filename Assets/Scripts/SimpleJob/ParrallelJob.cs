using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Jobs.SimpleJob
{
    // To process the elements of a list or an array in parrallel, we can use IJobParallelFor
    // For example to square all the entry in the array in parrallel it's possible to split the work accross many jobs
    // But better solution is to use IJobParallelFor
    // -> With IJobParallelFor, Execute() is called once for every index of the array
    // -> Each call process a single element based on the index passed parameter
    // ! Jobs always must be a struct

    [BurstCompile]
    public struct ParrallelJob : IJobParallelFor // https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Unity.Jobs.IJobParallelFor.html
    {
        public NativeArray<int> Numbers;

        public void Execute(int index)
        {
            Numbers[index] *= Numbers[index];
        }
    } 
}
