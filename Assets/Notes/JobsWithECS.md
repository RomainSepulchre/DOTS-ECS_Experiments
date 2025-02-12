[Back to summary...](../../)

# Use jobs with ECS

To optimize further the performance of ECS we can use [jobs](jobs.md) to process entity data on worker threads.

## Entities Job interfaces

The entities package provide 2 interfaces that defines jobs accessing entities:
- **`IJobChunk`**: `Execute()` method is called once for each individual chunk that match the query.
- **`IJobEntity`**: `Execute()` method is called once for each entity matching the query.

`IJobEntity` is generally more convenient to write and use but `IJobChunk` provide more precise control. **The performance of both solutions are identical**.

> `IJobEntity` is not a real job type, source generation is used to extend `IJobEntity` struct with an implementation of `IJobChunk`. So, when we schedule a `IJobEntity`, it is scheduled as a `IJobChunk` and it will appear as a `IJobChunk` on the profiler.

[ijobchunk]: https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/iterating-data-ijobchunk-implement.html
[ijobentity]: https://docs.unity3d.com/Packages/com.unity.entities@1.2/manual/iterating-data-ijobentity.html

### IJobChunk

```c#
```

### IJobEntity

TODO: TEST THIS IN REAL CONDITION

```c#
public struct MyComponent : IComponentData // A simple component for the example
{
    public float Value;
}

public partial struct MyEntityJob : IJobEntity // partial keyword is needed because IJobEntity use source generation to implement IJobChunk in a separated file (project/Temp/GeneratedCode/.....)
{
    // We need to add Execute() manually
    public void Execute(ref MyComponent component)
    {
        // Operation do to on the component data
        component.Value += 1f;
    }
}

// The system that runs the job
[BurstCompile]
public partial struct MySystem : ISystem
{
    // ... Other system methods

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Schedule the job
        new MyEntityJob.Schedule(); // If we don't specify any query the job create it's own query that matches its parameters (so in this case we will run the job on any entity that has MyComponent) 
    }
}
```

#### Use a query

### Schedule job accross threads

Contrary to some jobs that need to implement a different interface, these two types of jobs can be split accross multiple threads very simply.

When you schedule a `IJobEntity` or a `IJobChunk` you can call two methods:
- `Schedule()`: schedule the job on one thread.
- `ScheduleParallel()`: schedule the job in parrallel to split the work accross multiple threads.

When using `ScheduleParallel()`, the chunks that match the query are seperated inside different batches and these batches will be dispatched on the worker threads. 

### Strutural change inside a job

It is not possible to do strutural changes inside a job, they must be done on the main thread. However, a job can record stuctural change command in an **[`EntityCommandBuffer`](#entity-command-buffer)** and the commands are played back later on the main thread.

See [this section to more info on EntityCommandBuffer](#entity-command-buffer).

## Entity Command Buffer