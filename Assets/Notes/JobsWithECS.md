[Back to summary...](../../)

# Use jobs with ECS

Summary:

- [Entities Job interfaces (IJobChunk, IJobEntity)](#entities-job-interfaces)
- [Synchronization points (sync points)](#synchronization-points-sync-points)
- [Component safety handles and system job dependency](#component-safety-handles-and-system-job-dependency)
- [ComponentLookUp<T>](#componentlookup)
- [Entity command buffer](#entity-command-buffer)
- [Job scheduling overhead](#job-scheduling-overhead)

Resources links:
- [EntityComponentSystemSamples github repository](https://github.com/Unity-Technologies/EntityComponentSystemSamples/tree/master?tab=readme-ov-file)
- [Document Unity Entities 101](https://docs.google.com/document/d/1R6E4IDpfLatwHITlCND0i5TuMVG0CMGsentFL-3RQT0/edit?tab=t.0)
- [Unity ECS Jobs with entities documentation](https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/job-system.html)

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

### [IJobEntity][ijobentity]

Here is how to use an `IJobEntity` that iterate over every entity that has a *MyComponent* component.

```c#
public struct MyComponent : IComponentData // A simple component for the example
{
    public float Value;
}

[BurstCompile]
public partial struct MyEntityJob : IJobEntity // partial keyword is needed because IJobEntity use source generation to implement IJobChunk in a separated file (project/Temp/GeneratedCode/.....)
{
    // We need to add Execute() manually
    public void Execute(ref MyComponent component) // ref is used for component that will be read and write, for read only component we should use in
    {
        // Operation do to on the component data
        component.Value += 1f;
    }
}

// The system that runs the job
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

We are not limited to only one component in our job, we can pass several components and a query that match all those component will be created.  
In the example below we declare an `IJobEntity` that will iterate over every entity that has the component *ComponentA* and *ComponentB*.  
Note that *ComponentA* is declared with the keyword a `ref` and *ComponentB* with the keyword `in` to show the difference read-write and read-only component: `ref` must be used for component where we will need a read-write access and `in` must be used for component where a read-only access is enough.

```C#
[BurstCompile]
public partial struct MyEntityJob : IJobEntity
{
    public void Execute(ref ComponentA compA, in ComponentB compB) // ref is used for components where read-write is needed, in is used for components where read-only is enough
    {
        //... Operation do to on the component data
    }
}
```

#### IJobEntity supported parameters

To get the full list of parameters supported by in an `IJobEntity` `Execute()` [checks this section of unity documentation](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/iterating-data-ijobentity.html#execute-parameters).

Here is a few of short list of the main parameters:
- `IComponentData`: give access to com,ponent data, marked as `ref` for read-write access and `in` for read-only access.
- `Entity`: get the current entity. It's a value copy only, so it don't mark with `ref` or `in`.
- `DynamicBuffer<T>`: get a dynamic buffer, marked as `ref` for read-write access and `in` for read-only access.
- 3 `int` are supported but must be marked with an attribute:
    - `[EntityIndexInQuery]`: Set on a `int` parameter in `Execute()`, the int parameter marked by the attribute will return the current index in the query for the current entity iteration. It the equivalent of `entityInQueryIndex` in `Entities.ForEach`. **This parameter internally use `EntityQuery.CalculateBaseEntityIndexArray[Async]` which negatively affects performance**.
    - `[ChunkIndexInQuery]`: Set on a `int` parameter in `Execute()`, the int parameter marked by the attribute will return the current archetype chunk index in a query.
    - `[EntityIndexInChunk]`: Set on a `int` parameter in `Execute()`, the int parameter marked by the attribute will return the current entity index in the current archetype chunk. Associated with [ChunkIndexInQuery] it give us a unique identifier per entity.



#### Specify a query for our job

If we need to do a more complex query we can set an `EntityQuery` and pass as a parameter when scheduling our job.

> To build a query, using `SystemAPI.QueryBuilder()` is better than `state.GetEntityQuery()` since it does not allocate GC and is burst-compatible.

In the example below we create a query to execute our job only on the entity that have *ComponentA* but doesn't have *ComponentB*.

```c#
[BurstCompile]
public partial struct MyEntityJob : IJobEntity
{
    public void Execute(ref ComponentA compA) // Our job still pass the component it will need to access
    {
        //... Operation do to on the component data
    }
}

// The system that runs the job
public partial struct MySystem : ISystem
{
    EntityQuery jobQuery; // It's be better to assign the query once in OnCreate and keep it cached rather than recreating a query at every update.

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Assign EntityQuery, here the query get all the entity that has ComponentA and don't have ComponentB
        // Obviously, the query must include the component we want to access in the job (ComponentA in this case)
        jobQuery = SystemAPI.QueryBuilder().WithAll<ComponentA>().WithNone<ComponentB>().Build();
        // Using SystemAPI.QueryBuilder() is better than state.GetEntityQuery() since it does not allocaate GC and is burst-compatible
    }

    // ... Other system methods

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Schedule the job with the query as parameter
        new MyEntityJob.Schedule(jobQuery);
    }
}
```

#### IJobEntity attributes

Another way to narrow a query is to use the built-in attributes that comes with `IJobEntity`:

- `[WithAll(params Type[])]`: set on the job struct, the entity must have the component types passed in the attributes to be in the query.
- `[WithAny(params Type[])]`: set on the job struct, the entity must have any of the component types passed in the attributes to be in the query.
- `[WithNone(params Type[])]`: set on the job struct, the entity must not have the component types passed in the attributes to be in the query.
- `[WithChangeFilter(params Type[])]`: set on the job struct or attached to an argument in `Execute()`, narrows the query so that the entities have to have had changes in the archetype chunk for the given components.
- `[WithOptions(params Type[])]`: set on the job struct, changes the scope of the query to use the `EntityQueryOptions` described.

Here is a quick example of `[WithAny()]` used to specify the entity must have any of *ComponentB* and *ComponentC* components
```C#
[BurstCompile]
[WithAny(typeof(ComponentB), typeof(ComponentC))]
public partial struct MyEntityJob : IJobEntity
{
    // Our job will iterate through every entity with ComponentA that also has ComponentB or ComponentC
    public void Execute(ref ComponentA compA) 
    {
        //... Operation do to on the component data
    }
}
```

### [IJobChunk][ijobchunk]

TODO: TEST THIS IN REAL CONDITION

```c#
```

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

## Entity command buffer

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
> For example: using the same [`EntityCommandBuffer.ParallelWriter`](#entitycommandbufferparallelwriter) accross multiple parrallel jobs might lead to unexpected playback order of the commands.  
> &rarr; It's always best to **create one `EntityCommandBuffer` per job** and anyway there not much performance difference.

### Temporary entity ID

When we record a command to create a new entity like `CreateEntity()` or `Instantiate()` in a `EntityCommandBuffer` it return a *temporary entity ID* since no entity is created until `Playback()` is called.

The temporary ID is a negative index number, it is used to record changes on an entity that will be created by the `EntityCommandBuffer`. After we recorded the creation of an entity, the subsequent `AddComponent()`, `SetComponent()` and `SetBuffer()` methods recorded in the same `EntityCommandBuffer` can use the temporary ID to make change on the future entity that will be created. Once `Playback()` is called every temporary ID in the command will be remapped to the actual ID of the created entity.

> A temporary ID has only a meaning inside the `EntityCommandBuffer` from which it was returned, so it must only be used in it.

### EntityCommandBuffer.ParallelWriter

If we need to record command inside a parallel job we need to use a [`EntityCommandBuffer.ParallelWriter`](https://docs.unity3d.com/Packages/com.unity.entities@1.3/api/Unity.Entities.EntityCommandBuffer.ParallelWriter.html). Most of the methods of the parallel writer are the same as a normal `EntityCommandBuffer` but they all takes an additional *sort key* argument to keep the commands deterministic order.

In a parallel job, the work will be split amongst several threads that runs in parallel so the order in which the commands will be recorded depends on the thread scheduling, making it non-deterministic. This is problematic for 2 reasons: non-deterministic code is harder to debug and some netcode solutions rely on determinism to produce a consistent result accross different machines.

It will never be possible to record the command in a deterministic order in a parallel job, however it's possible to playback them in order which solve the problem. That's why we need to pass the additional *sort key* argument:
1. Commands are recorded with a *sort key*.
2. When `Playback()` is called, commands are sorted using their sort key.
3. Commands are executed in their deterministic order.

Of course, the sort key passed must determiniscally correspond to the command recorded to keep the playback order deterministic.

> **Sort key with `IJobEntity`**:   
> Generally we want to use `ChunkIndexInQuery`. It's a unique value for every chunk but it's not an issue since all entities of a chunk are processed together on the same thread and the order inside a chunk is stable.

> **Sort key with `IJobChunk`**:
> the `unfilteredChunkIndex` of the `Execute()` method should be used.

### Multi-Playback

The option `PlaybackPolicy.MultiPlayback` can be used when creating an `EntityCommandBuffer` to allow the `Playback()` method to be called more than once.

Otherwise, calling several time `Playback()` on the same `EntityCommandBuffer` will provoke an exception.

> This is mainly useful to spawn the same set of entities repetetively.

### Entity Command Buffer Systems

> *** I need to understand better how to use this in code *** 

An [`EntityCommandBufferSystem`](https://docs.unity3d.com/Packages/com.unity.entities@0.7/api/Unity.Entities.EntityCommandBufferSystem.html) is a specific type of system that allow to play back the commands recorded in an `EntityCommandBuffer` at a clearly defined point in the frame.

In most of the case, it's not needed an `EntityCommandBufferSystem` ourself. The 3 systems groups generated in a default world (initialization, simulation, presentation) each already provide 2 `EntityCommandBufferSystem` (one that runs before the other systems of the group and one that run after all systems of the group):
- `BeginInitializationEntityCommandBufferSystem`
- `EndInitializationEntityCommandBufferSystem`
- `BeginSimulationEntityCommandBufferSystem`
- `EndSimulationEntityCommandBufferSystem`
- `BeginPresentationEntityCommandBufferSystem`
- There is no `EndPresentationEntityCommandBufferSystem` because it the same as using `BeginInitializationEntityCommandBufferSystem` (the end of a frame and the beginning of the next frame is the same point in time)

> An `EntityCommandBuffer` instance created from a `EntityCommandBufferSystem` will be automatically played back and disposed at the next `EntityCommandBufferSystem` update. It should never be manually played back or disposed.

## Job scheduling overhead

When a job is scheduled, there is always a small CPU overhead because Unity need to allocate thread memory and copy data for the job to be able to access those data. This overhead is almost never noticeable except if our application schedule many jobs for a short amount of time.

To check if the CPU takes more time to schedule a job than the job takes to execute, we can check in the profiler if the system that schedule the job marker is longer than the job's marker then we might have a scheduling overhead issue.

### Reduce scheduling overhead

If there is a scheduling overhead the best way to reduce it is to **increase the amount of useful work a job does**.

For example:
- Combine jobs that operate on similar sets of data into a single larger job.
- If a parallel job perform a small amount of work on each thread, consider running it on a single thread.

**Avoid moving back the overhead on the main thread**, it might introduce a sync point if other jobs need to access the same data and accepting the scheduling overhead might be more effective.

Running code on the main thread is only recommended in theses cases:
- For prototyping without the inconvienence of jobs
- When we manipulate tiny amount of data, that is not used by another job so we don't generate sync point and running on the main thread is more effective than accepting the scheduling overhead.
- When we're doing that can't be done outside of the main thread (ex: structural changes, interactions with Gameobject based code, calling main thread only core Unity engine API)

If one of these situations applies, avoid using the job system by using an idiomatic foreach.

### Configure job worker count

The number of worker used by the application can be configured by setting `JobUtility.JobWorkerCount`. The number of worker thread used must enough to perform the work required without introducing CPU bottlenecks and less than introducing thread spending a lot of time idle. Use the profiler to test the change.