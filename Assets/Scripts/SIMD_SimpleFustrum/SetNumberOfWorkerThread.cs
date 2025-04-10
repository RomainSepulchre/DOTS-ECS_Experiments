using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace Burst.SIMD.SimpleFustrum
{
    public class SetNumberOfWorkerThread : MonoBehaviour
    {
        public bool SetToOneWorkerThread = false;

        void Awake()
        {
            if(SetToOneWorkerThread) JobsUtility.JobWorkerCount = 1;
        }
    }
}
