[Back to summary...](../../)

# Use jobs with ECS

To optimize further the performance of ECS we can use [jobs](jobs.md) to process entity data on worker threads.

## Entities Job interfaces

The entities package provide 2 interfaces that defines jobs accessing entities:
- [**`IJobChunk`**](#ijobchunk): `Execute()` method is called once for each individual chunk that match the query.
- [**`IJobEntity`**](#ijobentity): `Execute()` method is called once for each entity matching the query.

`IJobEntity` is generally more convenient to write and use but `IJobChunk` provide more precise control. **The performance of both solutions are identical**.

> `IJobEntity` is not a real job type, source generation is used to extend `IJobEntity` struct with an implementation of `IJobChunk`. So, when we schedule a `IJobEntity`, it is scheduled as a `IJobChunk` and it will appear as a `IJobChunk` on the profiler.

[ijobchunk]: https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/iterating-data-ijobchunk-implement.html
[ijobentity]: https://docs.unity3d.com/Packages/com.unity.entities@1.2/manual/iterating-data-ijobentity.html

### Schedule job accross threads

Contrary to some jobs that need to implement a different interface, these two types of jobs can be split accross multiple threads very simply.

When you schedule a `IJobEntity` or a `IJobChunk` you can call two methods:
- `Schedule()`: schedule the job on one thread.
- `ScheduleParallel()`: schedule the job in parrallel to split the work accross multiple threads.

When using `ScheduleParallel()`, the chunks that match the query are seperated inside different batches and these batches will be dispatched on the worker threads. 

### Strutural change inside a job

It is not possible to do strutural changes inside a job, they must be done on the main thread. However, a job can record stuctural change command in an **[`EntityCommandBuffer`](#entity-command-buffer)** and the commands are played back later on the main thread.

See [this section to more info on EntityCommandBuffer](#entity-command-buffer).

### [IJobChunk][ijobchunk]

TODO: TEST THIS IN REAL CONDITION

```c#
```

### [IJobEntity][ijobentity]

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



## Synchronization points (Sync points)

Some operations done on the main thread will trigger a "synchronisation point": some or all currently scheduled jobs are completed to prevent conflicts.

Examples:
- `EntityManager.AddComponent<T>()`: All currently scheduled jobs that access any *T* components must be completed before executing the method.
- `EntityQuery` `ToComponentDataArray<T>()`,`ToEntityArray()`and `ToArchetypeChunkArray()` methods: They must complete any currently scheduled jobs that access one of the components accessed in the query.

In many cases a synchronisation point can also invalidate existing instance of `DynamicBuffer` and `ComponentLookup<T>`. An invalidated instance will throw safety check exceptions when trying to call its methods. **If an invalidated instance is still needed, a new instance must be retrieved to replace it**.

## Component safety handles and system job dependency

### Component safety handles

In every worlds, each component type has an associated job safety handle like a [native collection](Jobs.md#data-access-general-rules). That means two jobs cannot access the same component type at the same time in the same world, if they do a safety check exception will be thrown.

The solutions to prevent this exception are the same we have with Native collections:
- Completing one of the jobs before scheduling the other.
- Set the job already scheduled as a dependency of the new job we schedule.
- If we only need a read-only access, we can use the `[ReadOnly]` attribute to access the component type concurrently on several jobs since nothing will be modified on it.

A `DynamicBuffer<T>` instance also has a safety handle so the same rules apply to it:
- The content of the `DynamicBuffer<T>` cannot be accessed while any scheduled jobs access the same buffer component type and is not completed yet.
- If the uncompleted jobs all have only a read-only access to the buffer component type, then the main thread can read the buffer.

### SystemState Dependency

When we schedule a job in a system we always want to make sure it will depend upon any currently scheduled jobs that could conflict with it, **even jobs scheduled in other systems**.

To get the correct dependencies through differents systems we can get a `JobHandle` by using:
- For `SystemBase` systems: `this.Dependency` 
- For `ISystem` systems: `state.Dependency` from the `SystemState` passed as methods argument.

The following Dependency sections [have more detailed explanation here](EntitiesSystems.md#why-is-it-important-to-keep-components-accessed-in-a-system-registered-)

#### Dependency changes before a system updates

Immediately before a system update starts, 2 things happens:

1. The current system `Dependency` property is completed.
2. The current system `Dependency` property is assigned a combination of the `Dependency` job handles of all others systems which access any of the same component types as this systems.

This make sure all the jobs scheduled later in the system's update will have the correct dependencies if we make them dependent of the `Dependency` property.

#### Dependency rules to follow when scheduling jobs in a system

**To ensure any future jobs started in this or another system will have the correct dependencies we need to follow 2 rules:**

1. Any jobs scheduled in a system update should depend upon this system `Dependency` property directly or indirectly (depend upon something that depend on it).
2. Before a system update return, `Dependency` must be assigned a handle that includes all the jobs started in the update.

If we follow these 2 rules, every jobs scheduled in a system update will correctly depend upon all jobs scheduled previously in other systems that might access any of the same component types.

**Dependency and native collections access**

A System `Dependency` property only accounts for the components types and not the native collections. So, if two systems both schedule jobs that access the same native collection, their `JobHandle` will not necessarily be included in the job handle assigned to `Dependency` and they might not depend upon each other as they should.

To prevent that:
- Generally, the best solution is to store the native collection in a component, that way the native collections needed dependencies are the same as the component dependencies.
- Alternatively, another solution is to arrange dependencies by manually sharing `JobHandle` between the systems.

## ComponentLookUp<T>

## Entity Command Buffer