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

### What is it ?

A [`ComponentLookUp<T>`][componentlookup] is a native container that provide an array-like access to all instances of components of a specific *T* type. It can be used to look up the data associated with one entity while iterating over a different set of entities.

>If we want to get a dynamic buffer instead of a component we can use a [`BufferLookup<T>`][bufferlookup], it works exactly like `ComponentLookUp<T>` but with dynamic buffers.

[componentlookup]: https://docs.unity3d.com/Packages/com.unity.entities@1.3/api/Unity.Entities.ComponentLookup-1.html
[bufferlookup]: https://docs.unity3d.com/Packages/com.unity.entities@1.3/api/Unity.Entities.BufferLookup-1.html

### When should I use it ?

`EntityManager` can be used to access the components of individual entities but generally **it can't be used inside a job, that's why we use `ComponentLookUp<T>`** instead. **Outside of a job it's better to use the `EntityManager`** to avoid the overhead of creating the `ComponentLookUp<T>` object.

>In general looking up an entity by ID comes with the performance cost of chache misses, so it's always to avoid lookups when we can.

### How to use it ?

A `ComponentLookUp<T>` can be passed to a job by defining a public field for it in the job like we would have done with a Native Array.

Like any other naative container in a job, `ComponentLookUp<T>` or `BufferLookup<T>` should be marked as `[ReadOnly]` when we only need to read the components (or dynamic buffers).

It's possible to safely access read-only `ComponentLookUp<T>` in any job. However, by default, it's not possible to write in a `ComponentLookUp<T>` in parallel jobs (including `IJobEntity`, `Entities.Foreach` and `IJobChunk`). When we are sure two instances of the parallel job cannot write the same index, we can use `[NativeDisableParallelForRestriction]` attribute on the `ComponentLookUp<T>` field to remove the restriction.

To check if an entity has the component of type *T*, `ComponentLookUp<T>` has 2 methods:
- `HasComponent()`: return true if the specified entity has the component of type *T*.
- `TryGetComponent()`: return true if the specified entity has the component of type *T* and output the component value when it exists.

`BufferLookup<T>` has the equivalent methods: `HasBuffer()` and `TryGetBuffer()`.

## Entity Command Buffer

An [`EntityCommandBuffer`][entitycommandbuffer] allows to queue up changes on entities (from either a job or the main thread) and to perform these actions later on the main thread by calling the `EntityCommandBuffer` `Playback()` method.

The `EntityCommandBuffer` solve two problems:
1. The impossibility to access `EntityManager` from a job.
    - &rarr; Since we can't perform structural changes inside a job, we can instead record commands in an `EntityCommandBuffer` and call `Playback()` on the main thread once the job has been completed.
2. Performing a structural change (ex: creating a new entity) create a [sync point](#synchronization-points-sync-points) which force to wait the completion of all jobs.
    - &rarr; Instead of creating an unnecessary sync point we defer the structural changes to a consilated point later in the frame.

[entitycommandbuffer]: https://docs.unity3d.com/Packages/com.unity.entities@0.7/manual/entity_command_buffer.html

### Entity Command Buffer methods

An `EntityCommandBuffer` has most of the command of the `EntityManager`.

- `CreateEntity()`
- `DestroyEntity()`
- `AddComponent<T>()`
- `RemoveComponent<T>()`
- `SetComponent<T>()`
- `AppendToBuffer()`: Records a command that will append an individual value to the end of an existing buffer component.
- `AddBuffer()`: Returns a `DynamicBuffer` which is stored in the recorded command, we can then write in the returned buffer. During `Playback()` the contents of the returned buffer will be copied to the entity's actual buffer.
- `SetBuffer()`: Like `AddBuffer()`, but it assumes the entity already has a buffer of the component type. In playback, the entity's already existing buffer content is overwritten by the contents of the returned buffer.

>Some `EntityManager` methods have no equivalent in `EntityCommandBuffer` because using them is not possible or simply because it makes no sense. For example, there is no `GetComponent<T>()` method because it makes no sense to defer data reading.

Once we called `Playback()` on an `EntityCommandBuffer` instance it cannot be used anymore. If we need to record more command we have to create a new `EntityCommandBuffer` instance.

### Entity Command Buffer safety handle

Every `EntityCommandBuffer` has a job safety handle so it's not possible to (an exception is thrown if we try to do it):
- Invoke an `EntityCommandBuffer`'s method on the main thread while it is currently used by a scheduled job.
- Schedule a job that access an `EntityCommandBuffer` that is already used by another job, jobs that need to access the same `EntityCommandBuffer` must use dependencies.

> **Sharing a single `EntityCommandBuffer` instance accross multiple jobs is not recommended. It might work fine but in many case it won't**.  
> For example: using the same `EntityCommandBuffer.ParallelWriter` accross multiple parrallel jobs might lead to unexpected playback order of the commands.
> **&rarr; It's always best to create one `EntityCommandBuffer` per job and anyway there not much performance difference.**