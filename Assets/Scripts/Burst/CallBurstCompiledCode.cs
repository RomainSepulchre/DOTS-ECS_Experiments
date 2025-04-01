using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;

namespace BurstExperience
{
    public class CallBurstCompiledCode : MonoBehaviour
    {
        [SerializeField] Transform aTransform;
        [SerializeField] Transform bTransform;
        [SerializeField] Transform cTransform;


        // Call a simple BurstCompiled method from managed code
        void Update()
        {
            
            float3 posA = aTransform.position;
            float3 posB = bTransform.position;
            float3 posC = cTransform.position;
            
            BurstCompiledUtilities.BurstCompiled_MultiplyAdd(in posA, in posB, in posC, out float3 multAddResultBurst);
            BurstCompiledUtilities.NonBurstCompiled_MultiplyAdd(in posA, in posB, in posC, out float3 multAddResult);

            BurstCompiledUtilities.NonBurstCompiled_MultiplySub(in posA, in posB, in posC, out float3 multSubResult);
            BurstCompiledUtilities.BurstCompiled_MultiplySub(in posA, in posB, in posC, out float3 multSubResultBurst);


            Debug.Log($"Burst MultiplyAdd result:{multAddResultBurst}, MultiplySub result:{multSubResultBurst}; NonBurst MultiplyAdd result:{multAddResult}, MultiplySub result:{multSubResult}");
        }
    } 
}
