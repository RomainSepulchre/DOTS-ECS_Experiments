# Unity Jobs

## What is a job

A job is a small unit of work that aim to complete a specific task and that can run on a worker thread. The goal of jobs is to takes advantage of the multiple cores of modern CPU to reduce the amount of operation runned on the main thread by running the jobs on worker threads.

### How works jobs?

**Only the main thread can schedule and complete jobs**. When we schedule a job, the main thread add the job to the global job queue. When an idle worker thread will look for a new work, it will pull the job off the queue to run it. Once the main thread need the result of the job, it can complete the job **(complete is used synchronise jobs with the main thread)**. The main thread will then check if the job is already finished and if that's not the case it will wait for the end of the job. When we complete a job, all records of the job are removed from the work queue, so a job that has been scheduled must always be completed by the main thread at some point to avoid resource leak.

While a job is running the **main thread cannot access it's content** and **two jobs cannot access the same content at the same time**. We can use jobs dependencies to make sure two jobs will access the same content but one after the other.

> **A job that has been scheduled must always be completed by the main thread at some point to avoid resource leak**

### Jobs parameters

Just like a method call, a job receives parameters and operates on data. However, jobs should compute unmanaged objects so we need to use specific types. For example instead of using a standard array of int (*int[]*), we will use native array (*NativeArray<int>*) from [Unity.Collections](https://docs.unity3d.com/Packages/com.unity.collections@2.5/manual/index.html) package.


> **What is an unmanaged objects?**  
> Managed objects: Pure .Net code managed by runtime and under its control, will be handled by the garbage collector.
> Unmanaged objects: Code not managed by the runtime, the garbage collector won't know how to manage it.

> **Is it possible to use managed object in a job?**  
> ! It's not strictly impossible to use managed objects in jobs but doing it safely require a special care so normally jobs show avoid accessing managed objects.

> **Use of delta time in a job**  
> Delta time must be copied to the job since jobs generally don't have concept of a frame. The main thread waits for the job on the same frame or the next frame, but the job should perform work in a deterministic and independent way when running on worker threads.

## Create and Instantiate a job

### Create a job

A job is a struct that implements the interface IJob. The interface add a method Execute(), it's the method that will be invoked when the job will run.

> **!** When we declare a job, it must always be a struct

```c#
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile] // Enable burst compilation
public struct MyJob : IJob
{
    // Declaration of the jobs variable
    public NativeArray<int> SomeInts;

    public void Execute()
    {
        // The code that will be executed by the job
        for (int i = 0; i < SomeInts.Length; i++)
        {
            SomeInts[i] *= SomeInts[i];
        }
    }
}
```

### Instantiate, schedule and complete a job

To run our job we need to instantiate it in the main unity loop (Start(), Update(), ...). We create a new variable using the type of the job we created (here *MyJob*) and we pass the data we want to process in the declaration of *MyJob*.

Once we instantiated our job, we can schedule it. To schedule it we just call the job *.Schedule()* method and assign it to a *JobHandle* variable. The *JobHandle* identify a job that has been scheduled and allow us to *.Complete()* it or to create job dependencies.

When the main thread need to get the result from our job, it need to calls *.Complete()*. This will check if the job has been completed and if it's not the case the main thread will wait for the job before continuing to execute the code.

```c#
// Instantiate the job
var job = new MyJob { SomeInts = myArray }; // myArray is a NativeArray of ints that should have been initialized before

// Schedule the job (Put the job in the global job queue)
JobHandle handle = job.Schedule();

// ... some other code that runs until we need the result of our job

// Complete the job
handle.Complete() // Complete() should always be called at some point after a job has been scheduled to avoid resource leak

// Use the data processed in the job
Debug.Log(job.SomeInts[0]);
// We can also do this since myArray and job.SomeInts points to the same array in the memory (i'm not sure if any of the solution is better than the other)
// Debug.Log(myArray[0]);
```

## Jobs dependencies

### Schedule several jobs

### Jobs chain dependency

### One dependency for several jobs

### One job with several dependencies

### Complex combination of dependcies

### Avoid cyclic dependencies (Should not be possible)

## Parralel jobs






