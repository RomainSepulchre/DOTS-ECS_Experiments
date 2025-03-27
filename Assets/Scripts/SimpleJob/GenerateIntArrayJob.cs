using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Jobs.SimpleJob
{
    [BurstCompile]
    public struct GenerateIntArrayJob : IJob // https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Unity.Jobs.IJob.html
    {
        public int StartNumber;
        public NativeArray<int> IntArray;

        public void Execute()
        {
            for (int i = 0; i < IntArray.Length; i++)
            {
                IntArray[i] = StartNumber;
                StartNumber++;
            }
        }
    }
}