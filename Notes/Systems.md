[Back to summary...](../)

# Systems

Summary:

- [System(Isystem, SystemBase)](#system)
- [System groups and system organization](#system-groups-and-systems-organization)
- [System State](#system-state)
- [Time in world and system](#time-in-worlds-and-systems)
- [SystemAPI](#systemapi)
- [Iterate over components in Systems](#iterate-over-components-in-systems)
- [Store data in systems](#store-data-in-systems)
- [Optimize structural changes](#optimize-structural-changes)

Resources links:
- [EntityComponentSystemSamples github repository](https://github.com/Unity-Technologies/EntityComponentSystemSamples/tree/master?tab=readme-ov-file)
- [Document Unity Entities 101](https://docs.google.com/document/d/1R6E4IDpfLatwHITlCND0i5TuMVG0CMGsentFL-3RQT0/edit?tab=t.0)
- [Unity entities systems video](https://www.youtube.com/watch?v=k07I-DpCcvE)
- [Unity ECS Systems documentation](https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/systems-intro.html)

## System

A [System](https://docs.unity3d.com/Packages/com.unity.entities@1.2/manual/concepts-systems.html) is the code that provide the logic to modify the data of a component at runtime. There are different types of systems but they all implements these three callback: `OnCreate()`, `OnDestroy()` and `OnUpdate()`. To simplify, these callbacks are an equivalent of a monobehaviour Start(), OnDestroy() and Update() so for example  `OnUpdate()` is usually called every frame (contrary to monobehaviour, [it might not be the case depending on the group the system is part of](#override-onupdate-default-behaviour)).    

Each system belong to a world and generally the entities of a world are only accessed by a system that belong to him. However, it's not a strict limitation: systems, monobehaviour or any code can access the entities of any world.

There is two main type of systems:
- [`ISystem`][isystem]: an interface that can be implemented for unmanaged systems and is compatible with Burst.
- [`SystemBase`][systembase]: a class that can be inherited for managed systems but is not compatible for Burst.

**In general, `ISystem` should be used over `SystemBase`** to get the performance benefits of burst compilation. `SystemBase` has convenient features but at the comprise of using garbage collection allocations or increased sourgen compilation time.

Here is a table that show the compatibility of both types of systems:

| Features                                              | ISystem compatibility | SystemBase compatibility |
| ----------------------------------------------------- |:---------------------:|:------------------------:|
| Burst-compiled OnCreate(), OnDestroy() and OnUpdate() | Yes                   | No                       |
| Unmanaged memory allocations                          | Yes                   | No                       |
| GC allocated                                          | No                    | Yes                      |
| Can store managed data directly in system type        | No                    | Yes                      |
| [Idiomatic foreach][1]                                | Yes                   | Yes                      |
| [`Entities.Foreach`][2]                               | No                    | Yes                      |
| [`Job.WithCode`][3]                                   | No                    | Yes                      |
| [`IJobEntity`][4]                                     | Yes                   | Yes                      |
| [`IJobChunk`][5]                                      | Yes                   | Yes                      |
| Support inheritance                                   | No                    | Yes                      |

[isystem]: https://docs.unity3d.com/Packages/com.unity.entities@1.2/manual/systems-isystem.html
[systembase]: https://docs.unity3d.com/Packages/com.unity.entities@1.2/manual/systems-systembase.html

[1]: https://docs.unity3d.com/Packages/com.unity.entities@1.2/manual/systems-systemapi-query.html
[2]: https://docs.unity3d.com/Packages/com.unity.entities@1.2/api/Unity.Entities.SystemBase.Entities.html#Unity_Entities_SystemBase_Entities
[3]: https://docs.unity3d.com/Packages/com.unity.entities@1.2/api/Unity.Entities.SystemBase.Job.html#Unity_Entities_SystemBase_Job
[4]: https://docs.unity3d.com/Packages/com.unity.entities@1.2/api/Unity.Entities.IJobEntity.html
[5]: https://docs.unity3d.com/Packages/com.unity.entities@1.2/api/Unity.Entities.IJobChunk.html

### Systems callback

Both types of systems have access to `OnCreate()`, `OnDestroy()` and `OnUpdate()`, here is an detailed explanation of each callback.

> **! These methods have default do-nothing implementations, so they can be ommitted in the systems when we dont use them.**

#### OnCreate

Equivalent of Monobehaviour `Start()`.

```C#
OnCreate(ref SystemState state) // OnCreate() with SystemBase
```

- **Called:** Before the first update  
- **Purpose:** Set initial state and parameters of the system before its usage

#### OnDestroy

Equivalent of Monobehaviour `OnDestroy()`.

```C#
OnDestroy(ref SystemState state) // OnDestroy() with SystemBase
```

- **Called:** When the system instance is removed from its world or the world itself is disposed  
- **Purpose:** Do something when the system will be destroyed (ex: reset parameters or dispose something)

#### OnUpdate

Equivalent of Monobehaviour `Update()`.

```C#
OnUpdate(ref SystemState state) // OnUpdate() with SystemBase
```

- **Called:** Called once per frame in most of the case (it is triggered by the parent system group's OnUpdate, [so group with custom OnUpdate might lead to other behaviour]())
- **Purpose:** Do the work that the system should repeat every frame

> If a system `Enabled` property is set to `false` update will be skipped

#### OnStartRunning and OnStopRunning

These methods can be overrided by default in a `SystemBase` system but in a `ISystem` we need to implement the additionnal interface `ISystemStartStop` to add these 2 additional methods.

##### OnStartRunning

Equivalent of Monobehaviour `OnEnabled()`.

```C#
OnStartRunning(ref SystemState state) // OnStartRunning() with SystemBase
```

- **Called:** Before the first `OnUpdate()` call and everytime the system is re-enabled (`Enabled` property changed from `false` to `true`)  
- **Purpose:** Do something when the system is re-enabled.

##### OnStopRunning

Equivalent of Monobehaviour `OnDisabled()`.

```C#
OnStopRunning(ref SystemState state) // OnStopRunning() with SystemBase
```

- **Called:** Before `OnDestroy()` and everytime the system is disabled (`Enabled` property changed from `true` to `false`)  
- **Purpose:** Do something when the system is disabled.

### ISystem

A system of type [`ISystem`][isystem] is a partial struct implementing the interface `ISystem`.

The callbacks implemented with `ISystem` all takes a [`SystemState`][systemstate] parameter to access the system's world and its entity manager:
| Parameters              | Description                                                                                                    |
| ----------------------- | -------------------------------------------------------------------------------------------------------------- |
| `ref SystemState state` | A reference to the [SystemState][systemstate] that provice access to the system's world and its entity manager |

```c#
public partial struct MySystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Called before the first update, equivalent of Start() in monobehaviour
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        // Called when the system instance is removed from its world or the world  itself is disposed, equivalent of OnDestroy() in monobehaviour
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Normally called once per frame, equivalent of Update() in monobehaviour
    }
}
```

> **Currently, IntelliSense doesn't support implementing default interface methods so we need to add `OnCreate(ref SystemState state)`, `OnDestroy(ref SystemState state)` and `OnUpdate(ref SystemState state)` manually.**

> In a system the `[BurstCompile]` attribute is only needed on the method that need to use burst, it's not needed to also add it to the struct .

[systemstate]: #system-state

### SystemBase

A system of type [`SystemBase`][systembase] is a partial class inheriting the class `SystemBase`.

Contrary to `ISystem`, no parameter is passed in the callbacks with `SystemBase`, the systems data are directly inherited from the base class. So for example if we want to get the system dependencies we can just call `Dependency` instead of calling `state.Dependency`. 

```C#
public partial class MySystem : SystemBase
{
    protected override void OnCreate() { }

    protected override void OnDestroy() { }

    protected override void OnUpdate() { }
}
```

## System groups and systems organization

The systems of a world are organized into a hierarchy of [system groups][systemgroup]. Every system group can have child systems or child system groups, it works like the explorer: a folder can have files and other folders which can also contains files and folders.

### System groups

A System group is a parent group that can contains system and system group childrens. The `OnUpdate()` method of the group trigger the `OnUpdate()` method of all its children.

#### Update of a system group

A system group has a `OnUpdate()` callback that call the update of all its childrens in a sorted order. By default, the children are sorted in a *pseudorandom order*[^1] and every time a child is added or removed to the group the list of children is resorted. If necessary, it is possible to [change the sorting order of systems using attributes](#system-sorting-order).

The default behaviour of a group [`OnUpdate()` can be overrided to create a custom update behaviour](#override-onupdate-default-behaviour).

In the editor, the Systems window (*Window > Entities > Systems*) allow to see the hieriarchy of system groups and systems are sorted in their update order. **The systems and groups we created are only added in the hierarchy at runtime**.

[systemgroup]: https://docs.unity3d.com/Packages/com.unity.entities@1.2/manual/systems-update-order.html
[^1]: Pseudorandom order: a sequence of number that appears to be statistically random despite having been produced by a deterministic and repeatable process.

#### Declare a system group

A [system group] is a class that inherits from [`ComponentSystemGroup`](https://docs.unity3d.com/Packages/com.unity.entities@1.2/api/Unity.Entities.ComponentSystemGroup.html). 

Like systems, the system groups have `OnUpdate()` callback. **By default, it is not implemented in the class** but it can be overrided to customize their behaviour.

```c#
public partial class MySystemGroup : ComponentSystemGroup
{
    protected override void OnUpdate()
    {
        base.OnUpdate();
    }
}
```

> Actually, system groups also have a `OnCreate()` and `OnDestroy()` callback but it seems they are generally not used.

#### Standard system groups

If we check in the Systems window (*Window > Entities > Systems*) we can see that there is 3 standard system groups that are updated from the Unity main loop itself.

- **Initialization System Group**: used for setup word
- **Simulation System Group**: used for core game logic
- **Presentation System Group**: used for rendering

Those standard system groups have default children systems and system groups, for example the `Fixed Step Simulation System Group` is a children of the `Simulation System Group`.

#### Override OnUpdate() default behaviour

If we want to update a system group childrens selectively or update them more than once in a single frame we can override the default `OnUpdate()` and change it's behaviour.

For example [*Fixed Step Simulation System Group*](https://docs.unity3d.com/Packages/com.unity.entities@1.3/api/Unity.Entities.FixedStepSimulationSystemGroup.html) reproduce the behaviour of a *FixedUpdate()*, it attempts to update its childrens at a fixed rate per second so in some frames it can update its childrens multiple times but in other frames it doesn't update them at all.

### Runtime creation of systems

When entering the play mode in editor an automatic bootstrapping process create a default world and populate it with the standard set of system and system groups.

The system and system groups we created in our project will also be created and added to this default world. By default, they are all added to the *Simulation System Group* but [this can be overriden with the `[UpdateInGroup()]` attribute](#change-the-group-of-a-system).

When a system is created, its `OnCreate()` callback is triggered. By default, the order of creation of the systems does not respect system groups but [we can use `[CreateAfter()]` and  `[CreateBefore()]` attributes to make sure some systems are created before or after others systems](#system-creation-order).

> It's possible to disable this automatic bootstrapping entirely by adding **#UNITY_DISABLE_AUTOMATIC_SYSTEMS_BOOTSTRAP** to our scripting define. But using this mean we are now responsible for creating any world, creating and adding systems or system groups instances, Registering top-level system group (such as `SimulationSystemGroup`) to update in the unity player loop.
>  
>*#UNITY_DISABLE_AUTOMATIC_SYSTEM_BOOTSTRAP_RUNTIME_WORLD* does the same thing but only for the default world and *#UNITY_DISABLE_AUTOMATIC_SYSTEM_BOOTSTRAP_EDITOR_WORLD* fot the editor world.  
>
> Alternatively, we can customize the bootstrapping logic by creating a class that implements [`ICustomBootstrap`](https://docs.unity3d.com/Packages/com.unity.entities@1.3/api/Unity.Entities.ICustomBootstrap.html).

### Runtime destructions of systems

When we call `World.Dispose()`, the systems are destroyed in the reversed order of their creation. Unity follow this order even if it broke a `CreateBefore` or `CreateAfter` constraint (ex: system manually created out of order).

### Change a system creation and sorting order

#### System creation order

Changing the system creation order is mostly needed when we want to be sure a system has been created before another one. The main use case are:
- When we must refer to another system inside `OnCreate()` with `World.GetExistingSystem()`. We need to ensure our system will be created after the one it's refering to.
- When we want to access a singleton component or another resource that is created in another system's `OnCreate()` method.

We can control the order of creation of a system by applying `[CreateBefore()]` or `[CreateAfter()]` attributes on a system. 
In the example below `MySystem` will always be ordered after `OtherSystemTypeA` but before `OtherSystemTypeB`.

```c#
[CreateAfter(typeof(OtherSystemTypeA))] // Create this system after system OtherSystemTypeA
[CreateBefore(typeof(OtherSystemTypeB))] // Create this system before system OtherSystemTypeB
[BurstCompile]
public partial struct MySystem : ISystem
{
    // ... OnCreate(), OnDestroy() and OnUpdate()
}
```

#### System sorting order

We can control the sorting of a system group by applying `[UpdateBefore()]` or `[UpdateAfter()]` attributes on one of its systems. The parameter `OrderFirst` and `OrderLast` of `[UpdateInGroup()]` can also be used and take precedence over `[UpdateBefore()]` and `[UpdateAfter()]`.  
These attributes only apply relative to a children that is part of the same system group.

In the example below `MySystem` will always be ordered after `OtherSystemTypeA` but before `OtherSystemTypeB`.

```c#
[UpdateAfter(typeof(OtherSystemTypeA))] // Sort this system after system OtherSystemTypeA
[UpdateBefore(typeof(OtherSystemTypeB))] // Sort this system before system OtherSystemTypeB
[BurstCompile]
public partial struct MySystem : ISystem
{
    // ... OnCreate(), OnDestroy() and OnUpdate()
}
```

> At runtime, we can open the editor Systems window (*Window > Entities > Systems*) to check if the order of our systems has changed.

### Change the group of a system

Sometimes we may want a system to be part of a specific system group, to do that we can use the `[UpdateInGroup()]` attribute.
In the example below we put *MySystem* in the *Fixed Step Simulation System Group* instead of the default *Simulation System Group*.

```C#
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct MySystem : ISystem
{
    // ... OnCreate(), OnDestroy() and OnUpdate()
}
```

## System State

The `SystemState` is used to allow a system of type `ISystem` to access a system's world and entity manager since, contrary to `SystemBase` they are not inherited from a base class.

A `SystemState` allow to access a system's world and entity manager inside it's `OnCreate()`, `OnDestroy()` and `OnUpdate()` callbacks.  
The main methods and properties of `SystemState` are:
- **`World`:** this system's world.
- **`EntityManager`:** the entity manager of this system's world.
- **`Dependency`:** a `JobHandle` to pass job dependencies between systems.
- **`GetEntityQuery()`**: Get a query for entity
- **`GetComponentTypeHandle()`**: Get a Component type handle (which are used to acces a component array of chunks)
- **`GetComponentLookup<T>()`**

**Within a system we should always use these methods instead of their equivalent from EntityManager** because the system state method register component type with the system.

For example, if we get a query for entities with ComponentA and ComponentB by calling SystemState.GetEntityQuery(), ComponentA and ComponentB will be registered with the system which would not have been the case if we called EntityManager.GetEntityQuery().

### Why is it important to keep components accessed in a system registered ?

The best way to understand why it's important is by looking at one of the most feature of system state: the **`Dependency`** property.

Immediately before a system update starts, 2 things happens:

1. `.Complete()` method is invoked on the `JobHandle` stored in the `Dependency` property of `SystemState`
2. `Dependency` property is then assigned a combined handle with the `Dependency` properties of all other systems which accessed the same component type.

> **Example:**  
> If a component of type *ComponentA* is registered with my system then the `Dependency` property of all other systems which also have accessed *ComponentA* are combined into one `JobHandle` that is assigned to my system `Dependency` property immediately before each time it update.

**The purpose of this behaviour is to help us pass the needed job dependencies amongst our systems to ensure that all jobs scheduled in a system properly depend upon conflicting jobs scheduled in other systems.**

### Rules for system job dependencies

To be sure all jobs scheduled in a system properly depends on conflicting jobs started in other systems **we need to follow 2 rules**:

1. All jobs scheduled in a system update should directly or indirectly depend on the system's `Dependency` property (*indirectly meaning being a dependency of something that depends on the system's dependencies*).
2. Before a system `OnUpdate()` returns, `Dependency` should be assigned a `JobHandle` that include all jobs scheduled in `OnUpdate()`.

**As long as these 2 rules are followed, any job we schedule will correctly depend upon jobs scheduled in other systems** and the job scheduled in our system will be completed at the latest before the next system update.

Here is an example with 2 jobs scheduled inside a system's `OnUpdate()`:

```C#
[BurstCompile]
public partial struct MySystem : ISystem
{
    // ... OnCreate(), OnDestroy()

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // The dependency of any job scheduled in the update must be state.Dependency (Rule 1.)
        JobHandle handleJobA = new JobA().Schedule(state.Dependency);
        JobHandle handleJobB = new JobB().Schedule(state.Dependency);

        // Before leaving OnUpdate() we combine the handle of every jobs scheduled in it and assign the combined handles to state.Dependency (Rule 2.)
        state.Dependency = JobHandle.CombineDependencies(handleJobA, handleJobB);
    }
}
```

If a job depends on another scheduled in the same OnUpdate(), we can set state.Dependency to the last handle of the chain of dependency since it will automatically include the first handle indirectly.

```C#
// ... in a system

[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    // The dependency of any job scheduled in the update must be state.Dependency (Rule 1.)
    JobHandle handleFirstJob = new FirstJob().Schedule(state.Dependency);
    JobHandle handleSecondJob = new SecondJob().Schedule(handleFirstJob); // we still depends on state.Dependency since it's a dependency of handleFirstJob.

    // Before leaving OnUpdate() we only need to assign the last handle of our chain of dependency to state.Dependency since it include the other handles indirectly. (Rule 2.)
    state.Dependency = handleSecondJob;
}

```

## Time in worlds and systems

A world has a `Time` property that returns a `TimeData` struct which contains the **frame delta time** and the **elasped time**. The values are updated in the system `UpdateWorldTimeSystem`.

It's possible to manipulate the `Time` values with these `World` methods:
- **`SetTime()`:**: set the time value.
- **`PushTime()`**: temporarily change the time value.
- **`PopTime()`**: restore the time value before the last `PushTime()`.

> Some system groups such as `FixedStepSimulationSystemGroup`, push a value before updating its children and pop the value once done. These groups present false time value to children.

## SystemAPI

[`SystemAPI`](https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/systems-systemapi.html) is a class that contains many static methods covering the same functionnaly as `World`, `EntityManager` and `SystemState`. It works in systems and `IJobEntity` (it doesn't work in `IJobChunk`) since it relies upon source generators. It works in both `SystemBase` and `ISystem` systems.

**The main advantage of using `SystemAPI` is that the results of the methods will be same in both context** (systems and `IJobEntity`) which means `SystemAPI` will be easier to copy-paste between the two contexts.

The actions we can perform with `SystemAPI` are:
- **Iterate through data:** Retrieve data per entity that matches a query.
- **Query building:** Get a cached `EntityQuery`, which you can use to schedule jobs, or retrieve information about that query.
- **Access data:** Get component data, buffers, and `EntityStorageInfo`.
- **Access singletons:** Find single instances of data, also known as `singletons`.

> `SystemAPI` also provide a `Query()` method that create a foreach loop over the entities and components that match a query.

**When looking for a key Entities functionnality, the general rule is:**
1. **Check in `SystemAPI` first.**
2. If the functionnality is not part of `SystemAPI`, **then check in `SystemState`.**
3. If we still didn't found the functionnality, **check in `EntityManager` and `World`**.

## Store data in systems

The best way to [organize and store system data](https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/systems-data.html) is to store them in a component rather than adding public fields within the system type.

### Never store system data in public fields

**Using public data on systems is a very bad pratice** because we need a direct reference or pointer to the system instance to access them and it has consequences:

- It create dependencies between systems, which conflict with data-oriented approches.
- There is no guarantee for thread or lifetime safety while accessing the system instance.
- There is no guarantee for thread or lifetime safety while accessing the system's data, even when the system still exist and is accesed in a thread safe manner.

To prevent us to do this, the `world` methods that allows us to get or create a system (such as `GetExistingSystem<T>`) doesn't return us a direct reference or pointer to the instance of the system, it returns a `SystemHandle`. A `SystemHandle` is an identifier representing a system instance in a particular world.

### Store data in component associated to the system

**The system's data that need to be publicly accessible should be stored in components**. To do that we have two solutions:

#### System-associated entity component

The main solution is to put the components in a system-associated entity by creating a component on a `SystemHandle` instead of an `entity`. We can then access the data by getting the component from our `SystemHandle` (`SystemAPI.GetComponent<T>(SystemHandle)`).

The main advantage of this is that **the data lifetime is tied to the system lifetime**.

```C#
// A system that store input data
public partial struct InputSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Add a component on the system to hold the input data
        state.EntityManager.AddComponent<InputData>(state.SystemHandle);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Update the component data
        InputData updatedInputData = new InputData
        {
            UpKeyPressed = Input.GetKey(KeyCode.UpArrow),
            DownKeyPressed = Input.GetKey(KeyCode.DownArrow),
            LeftKeyPressed = Input.GetKey(KeyCode.LeftArrow),
            RightKeyPressed = Input.GetKey(KeyCode.RightArrow),
        };
        SystemAPI.SetComponent<InputData>(state.SystemHandle, updatedInputData);
    }

    // Most of component data is automatically destroyed when the system is destroyed, the main exception is Native Containers.
    // If a Native Container existed in the component, we must ensure the memory is disposed (usually, OnDestroy is the best place for this)
    public void OnDestroy(ref SystemState state)
    { 
    }
}

// Another system that use the input system data
[UpdateAfter(typeof(InputSystem))] // Make sure the data have been updated
public partial struct MoveSystem : ISystem
{
    SystemHandle inputSystemHandle;

    public void OnCreate(ref SystemState state)
    {
        inputSystemHandle = state.World.GetExistingSystem<InputSystem>(); // Not compatible with burst compile
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        InputData currentInputData = SystemAPI.GetComponent<InputData>(inputSystemHandle);

        // ... do something with the input data
    }
}
```

#### Singleton entity component

Another solution is to use a [singleton entity component](https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/components-singleton.html). We can then access the data by getting the singleton (`SystemAPI.GetSingleton<T>()`).

This main differences of this solution are:
- A singleton can only have one instance per world.  
- Singletons are not tied to the system lifetime.
- Singletons can only exist per system type, not per system instance.

For more info [see the notes on Singleton components](Components.md#singleton-components)

## Optimize structural changes

### Comparison of the different structural changes approach

The following table compares different approaches to structural changes, and the time in milliseconds that it takes to add one component to one million entities with each approach:

| Method | Description | Time in ms |
| :---: | ----- | :---: |
| **EntityManager and query with enableable components** | Don't add any components, and enable a component that implements `IEnableable`, which was previously disabled. For more information, refer to Enableable components | 0.03 |
| **EntityManager and query** | Pass an `EntityQuery` to the `EntityManager` with `AddComponent` to immediately add components in bulk on the main thread. | 3.5 |
| **EntityManager and NativeArray** | Pass a `NativeArray<Entity>` to the `EntityManager` to immediately add components on the main thread | 35 |
| **Entity command buffer and playback query** | Pass an `EntityQuery` to an `EntityCommandBuffer` on the main thread to queue components to add using the `EntityQueryCaptureMode.AtPlayback` flag. Then execute that entity command buffer (time includes the entity command buffer execution time). For more information, refer to Entity command buffers.| 3.5 |
| **Entity command buffer and NativeArray** | Pass a `NativeArray<Entity>` to an `EntityCommandBuffer` on the main thread to queue components to add, then execute that entity command buffer (time includes the entity command buffer execution time).| 35 |
| **Entity command buffer and job system with `IJobChunk`** | Use an `IJobChunkacross` multiple worker threads to pass a `NativeArray` per chunk to an `EntityCommandBuffer`, then execute that entity command buffer (time includes the entity command buffer execution time). | 17 |
| **Entity command buffer and job system with `IJobEntity`** | Use an `IJobEntity` across multiple worker threads to pass instructions to add components to entities one at a time to an `EntityCommandBuffer`, then execute that entity command buffer (time includes the entity command buffer execution time)| 170 |

### Tips for structural changes optimization

#### Optimize Native Array for chunks:

When building a `NativeArray` of entities to apply structural change, match the entity order in the array with their order in memory. The simplest way to do this is with an `IJobChunk`, it iterate over the entities in a chunk in order and can build a NativeArray of entities that need to have the structural change. The NativeArray can then be passed to an `EntityCommandBuffer.ParallelWriter` to queue up the required changes. Accessing the entities in order increase the chance of CPU cache hits.

#### Entity command buffers and entity queries:

With `EntityQuery` passed to an `EntityManager` method, the entities not processed one by one but at a chunk level. However, when we use an `EntityQuery` with an `EntityCommandBuffer`, the query content could be affected by other structural changes between the times it's recorded in the ECB and the time it's playback.

To avoid that and processing entities one by one, we can use [`EntityQueryCaptureMode.AtPlayback`](https://docs.unity3d.com/Packages/com.unity.entities@1.3/api/Unity.Entities.EntityQueryCaptureMode.html) to store the query and evaluate it when the buffer is executed.

#### Enable/Disable systems to avoid structural change:

To prevent a specific system to process the entities that match its `EntityQuery`, we can disable the system with `SystemState.Enabled` so we don't have to remove a component required from the entities.

Another good way to do this use a specific component on an entity to signal if the system should be enabled or disabled. Then, we can use `RequireForUpdate()` to tell what component is needed and the system only update when the component exist. By doing that we only have to add/remove one component instead of dozens.

#### Avoid adding one component at a time when creating a new entity:

Every time we call `AddComponent()`, a new archetype is created and the entity move to a whole new chunk. This archetype exist for the rest of the application runtime even if it's never needed anymore which contribute to performance overhead when an `EntityQuery` calculate which archetypes it references.

Creating the archetype that describe the entity we want and use this archetype to create the entity allow us to skip unnecessary archetype and chunk creation.

```c#
// Create one entity with a specific archetype
var newEntityArchetype = state.EntityManager.CreateArchetype(typeof(ComponentA), typeof(ComponentB), typeof(ComponentC)); // We can cache the archetype if we intend to use it again later    
var entity = EntityManager.CreateEntity(newEntityArchetype);

// If we need to create lots of entity with the same archetype
var entities = new NativeArray<Entity>(10000, Allocator.Temp);  
state.EntityManager.CreateEntity(newEntityArchetype, entities);
```

#### Use ComponentTypeSet to add/remove more than one component:

Instead of adding or removing component one by one we can use a [`ComponentTypeSet`](https://docs.unity3d.com/Packages/com.unity.entities@1.3/api/Unity.Entities.ComponentTypeSet.html). A `ComponentTypeSet` is a struct holding several components, it can be passed to `EntityManager` methods and replace the Component parameter (ex: `AddComponent(Entity, ComponentTypeSet)`, `RemoveComponent(Entity, ComponentTypeSet)`).



