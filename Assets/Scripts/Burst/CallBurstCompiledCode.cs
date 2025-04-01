using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;

namespace Burst.Experiments
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

            BurstCompiledUtilities.BurstCompiled_MultiplyAddLoop(posA, posB, posC, out float3 burstResult);
            BurstCompiledUtilities.NonBurstCompiled_MultiplyAddLoop(posA, posB, posC, out float3 nonBurstResult);
            
            BurstCompiledUtilities.BurstCompiled_MultiplyAdd(in posA, in posB, in posC, out float3 multAddResultBurst);
            BurstCompiledUtilities.NonBurstCompiled_MultiplyAdd(in posA, in posB, in posC, out float3 multAddResult);

            BurstCompiledUtilities.NonBurstCompiled_MultiplySub(in posA, in posB, in posC, out float3 multSubResult);
            BurstCompiledUtilities.BurstCompiled_MultiplySub(in posA, in posB, in posC, out float3 multSubResultBurst);


            Debug.Log($"Loop burst : {burstResult}, non burst: {nonBurstResult} ; Burst MultiplyAdd result:{multAddResultBurst}, MultiplySub result:{multSubResultBurst}; NonBurst MultiplyAdd result:{multAddResult}, MultiplySub result:{multSubResult}");
        }

        private void Matrix4x4AndFloat4x4Tests()
        {
            Matrix4x4 matrix4X4_A = Matrix4x4.zero;
            matrix4X4_A[0, 0] = 3;
            matrix4X4_A[0, 1] = 5;
            matrix4X4_A[0, 2] = 6;
            matrix4X4_A[0, 3] = 7;
            matrix4X4_A[1, 0] = 2;
            matrix4X4_A[1, 1] = 6;
            matrix4X4_A[1, 2] = 1;
            matrix4X4_A[1, 3] = 2;
            matrix4X4_A[2, 0] = 6;
            matrix4X4_A[2, 1] = 9;
            matrix4X4_A[2, 2] = 1;
            matrix4X4_A[2, 3] = 3;
            matrix4X4_A[3, 0] = 4;
            matrix4X4_A[3, 1] = 5;
            matrix4X4_A[3, 2] = 2;
            matrix4X4_A[3, 3] = 4;

            Matrix4x4 matrix4X4_B = Matrix4x4.zero;
            matrix4X4_B[0, 0] = 1;
            matrix4X4_B[0, 1] = 4;
            matrix4X4_B[0, 2] = 9;
            matrix4X4_B[0, 3] = 5;
            matrix4X4_B[1, 0] = 6;
            matrix4X4_B[1, 1] = 3;
            matrix4X4_B[1, 2] = 2;
            matrix4X4_B[1, 3] = 2;
            matrix4X4_B[2, 0] = 1;
            matrix4X4_B[2, 1] = 4;
            matrix4X4_B[2, 2] = 5;
            matrix4X4_B[2, 3] = 6;
            matrix4X4_B[3, 0] = 7;
            matrix4X4_B[3, 1] = 8;
            matrix4X4_B[3, 2] = 4;
            matrix4X4_B[3, 3] = 1;

            float4x4 float4X4_A = float4x4.identity;
            float4X4_A[0] = new float4(3, 5, 6, 7);
            float4X4_A[1] = new float4(2, 6, 1, 2);
            float4X4_A[2] = new float4(6, 9, 1, 3);
            float4X4_A[3] = new float4(4, 5, 2, 4);

            float4x4 float4X4_B = float4x4.identity;
            float4X4_B[0] = new float4(1, 4, 9, 5);
            float4X4_B[1] = new float4(6, 3, 2, 2);
            float4X4_B[2] = new float4(1, 4, 5, 6);
            float4X4_B[3] = new float4(7, 8, 4, 1);

            Debug.Log($"A: {matrix4X4_A} \n.......\n {float4X4_A}");
            Debug.Log($"B: {matrix4X4_B} \n.......\n {float4X4_B}");

            Matrix4x4 maxtrix4X4_C = matrix4X4_A * matrix4X4_B;
            float4x4 float4X4_C = float4X4_A * float4X4_B;
            float4x4 float4X4_D = math.mul(float4X4_B, float4X4_A); // mul(B,A) == A * B

            Debug.Log($"C: {maxtrix4X4_C} \n.......\n {float4X4_C}");
            Debug.Log($"D: {float4X4_D}");
        }
    } 
}
