[Back to summary...](../)

# Entity queries and filtering: iterate over components

Summary:

- [SystemAPI.Query](#systemapiquery)
- [IJobChunk and IJobEntity](#ijobchunk-and-ijobentity)
- [Entities.Foreach](#entitiesforeach)
- [Filtering queries](#filtering-queries)
- [Write groups](#write-groups)
- [Version number](#version-number)
- [Manually iterate other data](#manually-iterate-other-data)

Resources links:
- [EntityComponentSystemSamples github repository](https://github.com/Unity-Technologies/EntityComponentSystemSamples/tree/master?tab=readme-ov-file)
- [Document Unity Entities 101](https://docs.google.com/document/d/1R6E4IDpfLatwHITlCND0i5TuMVG0CMGsentFL-3RQT0/edit?tab=t.0)
- [Unity queries documentation](https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/systems-iterating-data-intro.html)
- [How to Use Write Groups the Right Way](https://www.youtube.com/watch?v=8j8K6IIL0tI)

## SystemAPI.Query

Use this method to iterate through a collection of components on the main thread.  
Unity documentation can be found [here](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/systems-systemapi-query.html).

### Simple query

In this example we use a simple query to iterate through all entity that has both a *LocalTransform* and a *Speed* components. Once we accesed the data, we modify the entity position to move it forward.

```C#
public partial struct MySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPi.Time.DeltaTime;

        // SystemAPI.Query can only be used in a foreach. The type used in the query need to be a RefRW<T> or a RefRO<T>, T being the component we want to access.
        // RefRW<T> give a read-write access if you need to modify data while RefRO<T> only give a read only access
        foreach((RefRW<LocalTransform> transform, RefRO<Speed> speed) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Speed>>())
        {
            // When accessing a value from a RefRO<T> we need to access them through ValueRO
            // From a RefRW<T> we must use ValueRW if we need to write data and ValueRO if we only need to read it.
            float forwardSpeed = speed.ValueRO.forwardSpeed;
            float3 nextPosition = transform.ValueRO.Position + (transform.ValueRO.Forward() * forwardSpeed * deltaTime)
            transform.ValueRW.Position = nextPosition;
        }
    }
}
```

> To simplify the foreach, instead of declaring the type of every variable we can also write:  
> `foreach( var (transform, speed) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Speed>>())`

### Complex query

It possible to make a more precise query by using additional methods on our query. [Check this to see all the methods](https://docs.unity3d.com/Packages/com.unity.entities@1.0/api/Unity.Entities.QueryEnumerable-1.html), here are some of the most useful:

- `.WithAll<Tcomponent>()`: Specify component(s) that must be present.
- `.WithAny<Tcomponent>()`: Specify optional component(s)
- `.WithNone<Tcomponent>()`: Specify component(s) that must be absent.
- `.WithAbsent<Tcomponent>()`: Specify component(s) that must not be present.

All of these methods can specify several components like this: `.WithAny<TComponent1, TComponent2, TComponent3>()`.

> ***Is there a difference between `.WithAbsent<Tcomponent>()` and `.WithNone<Tcomponent>()`, they shound pretty similar***
> ***I had a weird issue when using `.WithAbsent<Tcomponent>()` where my query returned an error when I tried to check the query in the editor, it seems better to use `.WithNone<Tcomponent>()`***

In this example we iterate through all entities that have a *ComponentA* but no *ComponentB*.

```C#
public partial struct MySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // WithNone<T> is used to exclude entity with component T from the query
        foreach((RefRW<ComponentA> compA) in SystemAPI.Query<RefRW<ComponentA>>().WithNone<ComponentB>())
        {
            // Do something with compA
        }
    }
}
```

It's also possible to use several methods in a row to get an even more complex query.  
In the example below we iterate through all entities that have a *componentA*, no *componentB* and any of *componentC* or *componentD*.

```c#
foreach((RefRW<ComponentA> compA) in SystemAPI.Query<RefRW<ComponentA>>().WithNone<ComponentB>().WithAny<ComponentC,ComponentD>())
{
    // Do something with compA
}
```

### Accessing entity in the query

By default `SystemAPI.Query` only give us access to the components of the entities that match our query, but it's possible to also access the Entity itself by using `.WithEntityAccess()`.

```c#
// The entity parameter must always be the latest parameter
foreach ((RefRW<ComponentA> compA, RefRW<ComponentB> compB, Entity entity) in SystemAPI.Query<RefRW<ComponentA>, RefRO<ComponentB>>().WithEntityAccess())
{
    // Do something;
}
```

## IJobChunk and IJobEntity

Use this method to use jobs iterate through a collection of components.

`IJobEntity` is generally more convenient to write and use but `IJobChunk` provide more precise control however **the performance of both solutions are identical**.

For more info, checkmy detailed notes on [Using Jobs with ECS](JobsWithECS.md).

Unity documentation links:
- [using `IJobEntity`](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/iterating-data-ijobentity.html)
- [using `IJobChunk`](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/iterating-data-ijobchunk-implement.html)

## Entities.Foreach

When using a system that inherits from `SystemBase`, it's possible to use [`Entities.Foreach`](https://docs.unity3d.com/Packages/com.unity.entities@1.0/api/Unity.Entities.SystemBase.Entities.html#Unity_Entities_SystemBase_Entities) to execute code on entities and their components. We pass `Entities.Foreach` a lambda expression and when compiled, it is translated into a generated job that calls the expression once for each entity match our query.

**This should only be used in a `SystemBase`. In `ISystem`, `Entities.Foreach` is 4 times slower to compile than `SystemAPI.Query` and `IJobEntity`**

A typical `Entities.Foreach` will look like this:

```C#
// When using the standard delegates, the parameters must follow this order: passed-by-value -> ref -> in
Entities.ForEach(
    (Entity entity,
        int entityInQueryIndex,
        ref ComponentA compA, // ref is for read-write parameters
        in ComponentB compB) // in is for read-only parameters
        => { /* The code to execute */ }
).Schedule();

// Or a simplified version of it that only access the components
Entities.ForEach(
    (ref ComponentA compA,
        in ComponentB compB) =>
    => { /* The code to execute */ }
    ).Schedule();

// Like in a SystemAPi.Query we can use additional methos to precise the query
// Here we exclude entity with ComponentC
Entities
.WithNone(ComponentC)
.ForEach(
    (ref ComponentA compA,
        in ComponentB compB) =>
    => { /* The code to execute */ }
    ).Schedule();
```

> We can pass up to 8 parameters to `Entities.Foreach`, if more are needed [we need to define custom delegates](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/iterating-entities-foreach-define.html#custom-delegates).

The full list of `Entities.Foreach` supported features is available [here](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/iterating-data-entities-foreach.html).

For more information on `Entities.Foreach` check:
- [Define and execute an Entities.ForEach lambda expression](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/iterating-entities-foreach-define.html)
- [Select and access data](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/iterating-entities-foreach-select-data.html)
- [Filtering data](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/iterating-entities-foreach-filtering.html)
- [Use entity command buffers in Entities.ForEach](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/iterating-entities-foreach-ecb.html)

## Filtering queries

When we do a query it's possible to further sort entities by using a filter to exclude entities from the queries. There is 3 main types of filters:
- **Shared component filter**: filter the entities based on a shared component value
- **Change filter**: filter the entities based on wether the value of a specific component has changed.
- **Write groups**: filter the entities based on the attribute `[WriteGroups()]` set on its component.
- **Enableable components**: filter the entities based on the enableable component status. By default it's always the case, a query only considerer an entity has an enableable component if it is enabled.

**When we set a filter on a query it remain active until we call `ResetFilter()` on it.**

> To ignore filtering `EntityQuery` methods usually have a variant that end with `IgnoreFilter` or `WithoutFilter` variant that ignore filter including the enableable component filter used by default. These methods are generally more efficient than their equivalent with filtering.

### Shared component filter

To use the shared component filter, the targeted shared component must be included in the query. Once the query is ready we call its `SetSharedComponentFilter()` method and pass a struct of the same shared component with the value to select. It's possible to add up to two different shared components to filter.

```c#
struct MySharedComponent : ISharedComponentData // A shared component for the example
{
    public int Group;
}

public partial struct MySystem : ISystem
{
    EntityQuery query;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Create the query and include the shared component in it
        query =  SystemAPI.QueryBuilder().WithAllRW<LocalTransform>().WithAll<MySharedComponent>().Build();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        query.ResetFilter(); // Reset filter in case filters where already applied to the query

        // Set the shared component filter, pass a MySharedComponent with the value we want to select
        query.SetSharedComponentFilter(new MySharedComponent {Group = 1} );

        // Now only entity where MySharedComponent.Group is equal to 1 will be returned by the query
        //  For example, if we calulate the query count, only entity where MySharedComponent.Group is equal to 1 are counted
        int filteredCount = query.CalculateEntityCount();

        // We can still get the count without the filter using WithoutFilter/Ignorefilter methods or if we reset the filter and calculate the count again
        int nonFilteredCount = query.CalculateEntityCountWithoutFiltering();
    }
}
```

> The filter can be changed at any time but the existing arrays of entities created from the query with `ToComponentDataArray()` or `ToEntityArray()` are not retroactively changed, they must be recreated.

### Change filter

When we only need to update entities if a component value has been changed, we can use the change filter. The change filter checks wether a system that has declared a write access to the component has already run this frame, if it's the case the filter consider the component has been changed and archetype chunk with this component will be included in the query. *This is one of the reason why it important to only declare as read-write a component that really need to be modified*.

Like with the shared component filter, the targeted component must be included in the query. Then, we can call the query `SetChangedVersionFilter()` method and pass the type of the component we want to check for value changes.

```c#
public partial struct MySystem : ISystem
{
    EntityQuery query;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Create the query and include the shared component in it
        query =  SystemAPI.QueryBuilder().WithAllRW<LocalTransform>().WithAll<ComponentA>().Build();

        // Set the change filter, pass the type of component we want  to check for value changes
        query.SetChangedVersionFilter(typeof(ComponentA));
    }
}
```

> For efficiency the change filter is applied to whole archetype chunk and not to individual entities.

### Enableable component filter

[See Query enableable components in Enableable component notes](Components.md#query-enableable-components).

## Write groups

When working with ECS, usually a system read a set of input component and write to another set of output component. However sometimes it might be needed that another system with different input component override the output of the first system. That's why [write groups](https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/systems-write-groups.html) are useful, we can use write groups to exclude entities that the system would otherwise select and process and then process them in another system. The advantage of write groups is that it allows a system to exclude entities based a type unknown for the system and that could be in a different package.

### Create a write group

To create a write group we simply need to add the `WriteGroup(typeof(Component))` attribute to a component. It take one parameter, the type of the component that will be used by the components in the write group to update.

> A component can be member of several write groups.

```c#
//... A system write in ComponentW whenever ComponentA or ComponentB is on an entity

public struct ComponentW : IComponentData
{
   public int Value;
}

// The component that are used to know when we will write in ComponentW can be added in ComponentW write group
[WriteGroup(typeof(ComponentW))]
public struct ComponentA : IComponentData
{
   public int Value;
}
[WriteGroup(typeof(ComponentW))]
public struct ComponentB : IComponentData
{
   public int Value;
}
```

### Enable write group filtering

To enable the write group enable we just need to enable it with the `EntityQueryOptions.FilterWriteGroups` flag in the query options.

When the filtering is enabled, the query doesn't not select the components that are in a write group of a component that is writable in the query except if they are explicitely added in the query with `WithAll` or `WithAny`.

```C#
// With SystemAPI.Query
foreach ( var ComponentW in SystemAPI.Query<RefRW<ComponentW>().WithAll<ComponentA>().WithOptions(EntityQueryOptions.FilterWriteGroup))
{
    // Here all the entity with ComponentB are excluded because ComponentW is writable and B is part of ComponentW write group
    // ComponentA is part of ComponentW write group but is not excluded since it is specified with WithAll in the query
}

// With SystemAPI.QueryBuilder
SystemAPI..WithAllRW<ComponentW>().WithAll<ComponentB>().WithOptions(EntityQueryOptions.FilterWriteGroup).Build();
// -> Here all the entity with ComponentA are excluded because ComponentW is writable and A is part of ComponentW write group
//    ComponentB is part of ComponentW write group but is not excluded since it is specified with WithAll in the query
```

### Write group example

If we have 2 systems that change a `float4` color value in a *Color* component:

1. *ChangeColorSystem*, that change the color every frame based on an int value in *ColorId* component.
2. *SetToBlueSystem*, that change the color to blue only if the entity has *Blue* tag component. *Blue* tag component is from another package and is not accessible by *ChangeColorSystem* for the sake of the example.

We could run both systems sequentially but there is no point calculating a value that will be overrided in the second system. Instead of doing this, we can put the `[WriteGroup(typeof(ColorId))]` attribute on component *Blue* and add the `EntityQueryOptions.FilterWriteGroup` option in the query of the first system. When the first system will execute it will check the write group and exclude every entity that has a component marked with the attribute `[WriteGroup(typeof(ColorId))]`.

```c#
public struct ColorId : IComponentData
{
   public int Value;
}

public struct Color : IComponentData
{
   public float4 Value;
}

// The first system that change color base on ColorId value
public partial struct ChangeColorSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Add the options to filter write group in the query
        foreach ( var (color, coolorId) in SystemAPI.Query<RefRW<Color>, RefRO<ColorId>>().WithOptions(EntityQueryOptions.FilterWriteGroup) )
        {
            // ...change color depending on ColorId value
        }
    }
}

// ...In another package
[WriteGroups(typeof(Color))]
public struct Blue : IComponentData
{
}

// ... Another system that change Color to blue when an entity with Color component also has a Blue component
```

> In this example, *Blue* component is in another package and the first system is not aware of it otherwise the best solution would have been to exclude the entity with *Blue* from the query.

## Version number

A world and most of the things that compose it like its systems, its chunks, its entities have a [version number](https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/systems-version-numbers.html). This version number can be used to detect changes and implement optimization strategies like skipping processing if the data didn't change since last frame.

**A version number is a 32 bits signed integer that always increase and wrap around when the max value is reached**. This means that when we want to compare version number, we should use equality (==) or inequality (!=) operator and if we want to know if a version is more recent we should substract the other version and check if the result is bigger than 0.

``` c#
// Check if 2 version number are the same
bool areTheSame = versionA == versionB;
bool areNotTheSame = versionA != versionB;

// Check if a version is more recent than the other
bool bIsMoreRecent = (versionB - versionA) > 0;
```

> When a version number increase, there is no guarantee of how much it will increase by.

Here is a list of useful version number:
- `Entity.Version`: the version of an entity, increased when the entity is destroyed and allow to reuse entity index.
- `World.Version`: version of the world, increased when the world adds or removes a system or system group.
- `EntityManager.GlobalSystemVersion`: increased before every system update in the world
- `SystemState.LastSystemVersion`: version of the system, after each time the system update its version is set to the current value of `GlobalSystemVersion`.
- `EntityManager.EntityOrderVersion`: increased every time a structural change is made
- `EntityManager.GetComponentOrderVersion()`: return version of the component type, increased by any operation that get a write access on the component.
- `EntityManager.GetSharedComponentOrderVersion()`: shared component version, increase when a strutural change happens in a chunk that reference the shared component
- `ArchetypeChunk.GetChangeVersion(ComponentTypeHandle<T>)`: version for each component type in the chunk, increased when a component type in a chunk is accessed for writing. To increase the version is set to current `GlobalSystemVersion`.
- `ArchetypeChunk.GetOrderVersion()`: chunk version number, increased every time a structural change affects the chunk.

[For more details check version number documentation.](https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/systems-looking-up-data.html)

## Manually iterate other data

If for any we need to manage chunks in a way that is not supported by any of the solutiuon before, its possible to manually request all the archetype chunks in a native array and pass it to a custom `IJobParallelFor`.

[Check the unity documentation for more info on this](https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/iterating-manually.html).