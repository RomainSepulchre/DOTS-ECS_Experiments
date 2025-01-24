using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class InstantiateAndScheduleSimpleJob : MonoBehaviour
{

    void Update()
    {
        // Create a native array for our job
        var intArray = new NativeArray<int>(10000, Allocator.TempJob); // Allocator see https://docs.unity3d.com/Packages/com.unity.collections@2.5/manual/allocator-overview.html
        var job = new GenerateIntArrayJob { StartNumber = 1, IntArray = intArray };
        JobHandle handle = job.Schedule();
        handle.Complete();

        // All examples that can be runned to check what happening on the profiler

        ScheduleOneJob(intArray);

        ScheduleSeveralJobs(intArray);

        JobsChainDependency(intArray);

        SeveralJobsWithSameDependency(intArray);

        OneJobWithMultipleDependencies(intArray);

        ComplexCombinationOfJobDependencies(intArray);

        // We need to dispose of intArray manually since it's a native Array
        intArray.Dispose();
    }

    private void ScheduleOneJob(NativeArray<int> intArray)
    {
        //LogIntNativeArrayItems(intArray, $"BEFORE SINGLE JOB");

        // Instantiate the job
        var job = new SimpleJob { Numbers = intArray };

        // Schedule job (Put the job in the global job queue)
        JobHandle handle = job.Schedule();

        // Sleep to simulate other operation going on before .Complete();
        //Thread.Sleep( 2000 );

        // .Complete() should be called at some point after the job has been schedule
        // -> if the job hasn't finished it's execution, .Complete() wait for the job to finish to continue (.Complete() will not return until the job has finished it's execution)
        // -> .Complete() is used to synchronise jobs with the main thread.
        // => Main thread call .Complete() on a schedule job when he need the work to be finished
        // => .Complete() remove all records of the jobs from the work queue, so it is needed to avoid resource leak
        // ! Only main thread can schedule and complete jobs
        // ! main thread should not access data that is used by a currently scheduled job (ex: in this case we cannot access intArray between .Schedule() and .Complete())
        handle.Complete();

        //LogIntNativeArrayItems(job.Numbers, $"AFTER SINGLE JOB");
    }

    private void ScheduleSeveralJobs(NativeArray<int> intArray)
    {
        // ! Several jobs cannot access the same data conccurrently
        // Ex: Starting several jobs that access intArray trigger an exception
        if(false)
        {
            var firstJob = new SimpleJob { Numbers = intArray };
            var secondJob = new SimpleJob { Numbers = intArray };

            JobHandle handleFirstJob = firstJob.Schedule();
            JobHandle handleSecondJob = secondJob.Schedule();

            handleFirstJob.Complete();
            handleSecondJob.Complete();
        }

        // Unoptimal solution: Wait first job to complete before scheduling second job
        // ! Require main thread to wait for first job to schedule second job which is a waste of the miahn thread
        if(false)
        {
            var firstJob = new SimpleJob { Numbers = intArray };
            var secondJob = new SimpleJob { Numbers = intArray };

            JobHandle handleFirstJob = firstJob.Schedule();
            handleFirstJob.Complete();

            JobHandle handleSecondJob = secondJob.Schedule();
            handleSecondJob.Complete();
        }

        // Better solution: make the first job a dependency of the second by passing it's handle to the second job
        // => second will not be pulled off the queue until its job dependency has been finished
        // => Job dependency allow to set an execution order when its needed
        if(true)
        {
            var firstJob = new SimpleJob { Numbers = intArray };
            var secondJob = new SimpleJob { Numbers = intArray };

            JobHandle handleFirstJob = firstJob.Schedule();
            JobHandle handleSecondJob = secondJob.Schedule(handleFirstJob); // Pass the handle of the first job when scheduling second job

            // Calling .Complete() is only needed on the second job because calling .Complete() on a job calls it on all its dependencies
            handleSecondJob.Complete();
        }
    }

    private void JobsChainDependency(NativeArray<int> intArray)
    {
        // Chain dependencies
        // A dependency can also have a dependency so we it's possible to create dependency chains
        // Ex: A <- B <- C <- D (D depends on C which depends on B which depends on A so the execution order is A => B => C => D)
        var aJob = new SimpleJob { Numbers = intArray };
        var bJob = new SimpleJob { Numbers = intArray };
        var cJob = new SimpleJob { Numbers = intArray };
        var dJob = new SimpleJob { Numbers = intArray };

        JobHandle handleAJob = aJob.Schedule();
        JobHandle handleBJob = bJob.Schedule(handleAJob);
        JobHandle handleCJob = cJob.Schedule(handleBJob);
        JobHandle handleDJob = dJob.Schedule(handleCJob);

        // Calling .Complete() is only needed on D job because calling .Complete() on a job calls it on all its dependencies
        // Here D.Complete() calls C.Complete(), which calls B.Complete(), which calls A.Complete()
        handleDJob.Complete();
    }

    private void SeveralJobsWithSameDependency(NativeArray<int> intArray)
    {
        // Generate different Native Array for parralel jobs
        GenerateOtherIntArray(out NativeArray<int> secondIntArray, out NativeArray<int> thirdIntArray);

        // Several Jobs with the same dependency
        // Several jobs can have the same dependency, so A can be the dependency for B, C and D at the same time
        // Ex: B, C, D all depends from A (A will be executed first, then B, C and D will run conccurently)
        var aJob = new SimpleJob { Numbers = intArray };
        // ! B, C and D cannot all use intArray since they could be scheduled conccurently, so for this example I created different array for C and D
        var bJob = new SimpleJob { Numbers = intArray }; 
        var cJob = new SimpleJob { Numbers = secondIntArray }; 
        var dJob = new SimpleJob { Numbers = thirdIntArray }; 

        JobHandle handleAJob = aJob.Schedule();
        JobHandle handleBJob = bJob.Schedule(handleAJob);
        JobHandle handleCJob = cJob.Schedule(handleAJob);
        JobHandle handleDJob = dJob.Schedule(handleAJob);

        // A.Complete() is not needed since it's a dependency of B,C and D but B.Complete(), C.Complete() and D.Complete() are needed otherwise they will never be called
        // -> Calling .Complete several time on A (once for each of its dependencies in this case) is harmless.
        handleBJob.Complete();
        handleCJob.Complete();
        handleDJob.Complete();

        // Dispose generated array
        secondIntArray.Dispose();
        thirdIntArray.Dispose();
    }

    private void OneJobWithMultipleDependencies(NativeArray<int> intArray)
    {
        // Generate differents Native Array for parralel jobs
        GenerateOtherIntArray(out NativeArray<int> secondIntArray, out NativeArray<int> thirdIntArray);

        // One job with multiple dependencies
        // Dependencies can be combined with JobHandle.CombineDependencies()
        // Ex: A depends from B, C and D
        var aJob = new SimpleJob { Numbers = intArray };
        // ! B, C and D cannot all use intArray since they could be scheduled conccurently, so for this example I created different array for C and D
        var bJob = new SimpleJob { Numbers = intArray };
        var cJob = new SimpleJob { Numbers = secondIntArray };
        var dJob = new SimpleJob { Numbers = thirdIntArray };

        JobHandle handleBJob = bJob.Schedule();
        JobHandle handleCJob = cJob.Schedule();
        JobHandle handleDJob = dJob.Schedule();

        // Combine handle of B, C and D
        JobHandle combinedHandle = JobHandle.CombineDependencies(handleBJob, handleCJob, handleDJob);
        JobHandle handleAJob = aJob.Schedule(combinedHandle);

        // We only need to call A.Complete() because all others jobs are its dependecies
        handleAJob.Complete();

        // Dispose generated array
        secondIntArray.Dispose();
        thirdIntArray.Dispose();
    }

    private void ComplexCombinationOfJobDependencies(NativeArray<int> intArray)
    {
        GenerateOtherIntArray(out NativeArray<int> secondIntArray, out NativeArray<int> thirdIntArray);

        // We can combine all the types of dependencies explained before to create a complex graph of dependencies

        // Ex:
        // A <- B <-\ /<- E     
        //           D <- F
        //      C <-/ \<- G <- H <- I

        var aJob = new SimpleJob { Numbers = intArray };
        var bJob = new SimpleJob { Numbers = secondIntArray };
        var cJob = new SimpleJob { Numbers = thirdIntArray };
        var dJob = new SimpleJob { Numbers = intArray };
        var eJob = new SimpleJob { Numbers = secondIntArray };
        var fJob = new SimpleJob { Numbers = thirdIntArray };
        var gJob = new SimpleJob { Numbers = intArray };
        var hJob = new SimpleJob { Numbers = intArray };
        var iJob = new SimpleJob { Numbers = intArray };

        // Start A and C
        JobHandle handleAJob = aJob.Schedule();
        JobHandle handleCJob = cJob.Schedule();

        // B depends from A
        JobHandle handleBJob = bJob.Schedule(handleAJob);

        // D depends from B and C
        JobHandle HandleBCJob = JobHandle.CombineDependencies(handleBJob, handleCJob);
        JobHandle handleDJob = dJob.Schedule(HandleBCJob);

        // E,F,G depends from D
        JobHandle handleEJob = eJob.Schedule(handleDJob);
        JobHandle handleFJob = fJob.Schedule(handleDJob);
        JobHandle handleGJob = gJob.Schedule(handleDJob);

        // H depends from G
        JobHandle handleHJob = hJob.Schedule(handleGJob);

        // I depends from H
        JobHandle handleIJob = iJob.Schedule(handleHJob);

        // The tip of the graph are jobs E, F and I so we need to call .Complete() on them to call complete on all the jobs
        handleEJob.Complete();
        handleFJob.Complete();
        handleIJob.Complete();

        // Dispose generated array
        secondIntArray.Dispose();
        thirdIntArray.Dispose();
    }

    private void CyclicDependencies()
    {
        // Never created a cyclic dependency (A depends from B and B depends From A)
        // One of the job would be stuck forever waiting for the other job to end

        //    /<-\
        //   A    B
        //    \->/

        // Should not be possible since a job can only depends from a job that already has been scheduled and once schedule a job cannot change its dependencies
    }

    private void GenerateOtherIntArray(out NativeArray<int> secondArray, out NativeArray<int> thirdArray)
    {
        secondArray = new NativeArray<int>(10000, Allocator.TempJob);
        var jobFillSecondArray = new GenerateIntArrayJob { StartNumber = 1, IntArray = secondArray };
        thirdArray = new NativeArray<int>(10000, Allocator.TempJob);
        var jobFillThirdArray = new GenerateIntArrayJob { StartNumber = 1, IntArray = thirdArray };

        JobHandle handleFillSecondArray = jobFillSecondArray.Schedule();
        JobHandle handleFillThirdArray = jobFillThirdArray.Schedule();

        handleFillSecondArray.Complete();
        handleFillThirdArray.Complete();

        // Not necessary to do this since the job array is already pointing to the array passed at initialization. Should I keep it as a security or just to make the code clear ?
        // Note: NativeArray.Equals(OtherNativeArray) return true, however ReferenceEquals() return false -> I guess it's related to NativeArray being unmanaged objects
        //secondArray = jobFillSecondArray.IntArray;
        //thirdArray = jobFillThirdArray.IntArray;
    }

    private void LogIntNativeArrayItems(NativeArray<int> intArray, string context="")
    {
        string logMessage = $"{context} | Log IntNativeArray Items : [";
        for (int i = 0; i < intArray.Length; i++)
        {
            logMessage += $" {intArray[i].ToString()}";
        }
        logMessage += " ]";
        Debug.Log(logMessage);
    }
}

