[Back to summary...](../)

# Components

Summary:

- [What is a component ?](#what-is-a-component-)
- [Types of components](#types-of-components)
- [Transform Components and Systems](#transform-components-and-systems)

Resources links:
- [EntityComponentSystemSamples github repository](https://github.com/Unity-Technologies/EntityComponentSystemSamples/tree/master?tab=readme-ov-file)
- [Document Unity Entities 101](https://docs.google.com/document/d/1R6E4IDpfLatwHITlCND0i5TuMVG0CMGsentFL-3RQT0/edit?tab=t.0)
- [Unity entities video](https://www.youtube.com/watch?v=jzCEzNoztzM)
- [Unity ECS concept documentation](https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/components-intro.html)

## What is a [component](https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/concepts-components.html) ?

- Must be a struct.
- Interface IComponentData has no method but mark struct as a component type.
- Component can contain only unmanaged data type and can reference to other entities in the form of an entity id.
- Component are meant to be purely data so they usually don't have any methods even if there no issue adding one.


```C#
public struct MyComponent : IComponentData
{
    // Data stored in the component
    public int PowerLevel;
    public int PowerDamage;
    public float PowerCooldown;
}
```

Allowed field types in a component:
- [Blittable types][1]
- `bool`
- `char`
- [`BlobAssetReference<T>`][2], a reference to a Blob data structure
- [`Collections.FixedString`][3], a fixed-sized character buffer
- [`Collections.FixedList`][4]
- Fixed array (only allowed in an unsafe context)
- [Unity.Mathematics][5] types
- Other struct types that conform to these same restrictions.

[1]: https://learn.microsoft.com/en-us/dotnet/framework/interop/blittable-and-non-blittable-types
[2]: https://docs.unity3d.com/Packages/com.unity.entities@1.3/api/Unity.Entities.BlobAssetReference-1.html
[3]: https://docs.unity3d.com/Packages/com.unity.collections@2.5/api/Unity.Collections.FixedString.html
[4]: https://docs.unity3d.com/Packages/com.unity.collections@2.5/api/Unity.Collections.FixedList32Bytes-1.html
[5]: https://docs.unity3d.com/Packages/com.unity.mathematics@1.3/manual/index.html

>! It's actually possible to define managed component type that may contains other managed object by applying IComponentData to a class but they will have the same efficiency problems of gameObjects so it should be used unless strictly necessary.

## Types of components

This section won't be exhaustive, there are many differents types of component with different purpose. The complete list of component types and how to use them can be found [here](https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/components-type.html).

### Tag component

A component with no data and only used to identify all entities that representing something. They are very useful for queries.

For example, if I want to access all the entities representing a tree I can add them a *Tree* tag component and query this *Tree* tag component to get them all.

```c#
// A tag component is just a normal component with no data
public struct MyTagComponent : IComponentData
{
}
```

### DynamicBuffer component

A dynamic buffer is a component type that is a resizable array.

It is a struct that implements the interface `IBufferElementData`.
```C#
public struct MyDynamicBufferComponent : IBufferElementData
{
    public float3 Value;
}
```

The buffer of each entities stores:
- **A length**: number of elements in the buffer, start at 0 and increments each time a value is added in the buffer.
- **A capacity**: amount of storage in the buffer, start by matching internal buffer capacity (`128/sizeof(T)` by default) but can be specified with `[InternalBufferCapacity()]` attribute. Setting `Capacity` resizes the buffer.
- **A pointer**: indicate the location of the buffer content. Initially set to `null` to tell the content is stored in the chunk itself. When the capacity exceed the internal buffer capacity, the content is copied into a new larger array allocated outside of the chunks and the pointer is set to point to this new array.

Internal buffer capacity and external capacity (if present) are deallocated when the `EntityManager` destroys the chunk itself.

> **Note:** when a dynamic buffer is stored outside of the chunk, the internal capacaity is wasted and acccessing the content require following an extra pointer. **These costs can be avoided by making sure the internal capacity is never exceeded** but staying within this limit may require an excessively large internal capacity. **Another options is to set the internal capacity to 0**, the buffer will always be stored outside of the chunk and we will still follow an extra pointer when accessing content but we won't waste unused sapce in the chunk.

The `EntityManager` has key methods for dynamic buffers:
- **`AddComponent<T>()`:** Add a component of type *T* to an entity, where *T* can be a dynamic buffer component type.
- **`RemoveComponent<T>()`:** Remove a component of type *T* to an entity, where *T* can be a dynamic buffer component type.
- **`AddBuffer<T>()`:** Adds a dynamic buffer component of type *T* to an entity and returns the new buffer as a `DynamicBuffer<T>`.
- **`HasBuffer<T>()`:** Returns `true` if an entity currently has a dynamic buffer component of type *T*.
- **`GetBuffer<T>()`:** Returns an entity's dynamic buffer component of type *T* as a `DynamicBuffer<T>`.

`DynamicBuffer<T>` represent the dynamic buffer component of type *T* of an individual entity. It has the following key properties and methods:
- **`Length`:** Gets or sets the length of the buffer.
- **`Capacity`:** Gets or sets the capacity of the buffer.
- **`Item[Int32]`:** Gets or sets the element at a specified index.
- **`Add()`:** Adds an element to the end of the buffer, resizing it if necessary.
- **`Insert()`:** Inserts an element at a specified index, resizing if necessary.
- **`RemoveAt()`:** Removes the element at a specified index.

### Managed Components

The advantage of managed components is they can store properties of any types even managed types, however it comes with downsides:

- They are resource intensive to store and access, they are not stored directly in chunk like unmanaged components (see note below).
- They can't be accessed in jobs
- They can't be burst-compiled
- They require garbage collection
- A property using a managed type might need to manually add the ability to clone, compare and serialize the property

**Due to the heavy cost they incur, managed component should only be used when really needed.**

> *Note:* Managed component aren't stored in chunks directly, unity store them directly in one big array for the whole world and store their index in this array in the chunk. This means when we access a managed component Unity does an extra index lookup which makes managed components less optimal than unmanged components.

To declare a managed component, we declare a class that implement the `IComponentData`. It must either not have a contructor or have a parameterless constructor.

```c#
public class ManagedComponent : IComponentData
{
    public int Value
}
```

#### Managing lifecycle of managed components external resources

When managed components reference external resources, it's a best pratice to implement `ICloneable` and `IDisposable`.

For example with managed component that reference a `ParticleSystem`, by default duplicating a managed component entity will create 2 managed components that both reference the same `ParticleSystem` and when destroying the managed component the `ParticleSystem` will not be destroyed. Implementing `ICloneable` allow to duplicate the particle system for the second managed component and implementing `IDisposable` allow to destroy the `ParticleSystem` when the component is destroyed.

```c#
public class ManagedComponentWithExternalResource : IComponentData, ICloneable, IDisposable
{
    public ParticleSystem ParticleSys;

    // Method from ICloneable interface
    public object Clone()
    {
        // Code to clone particle system
        return new ManagedComponentWithExternalResource
        {
            ParticleSys = UnityEngine.Object.Instantiate(ParticleSys)
        };

    }

    // Method from IDisposable interface
    public void Dispose()
    {
        // Code to destroy particle system
        UnityEngine.Object.Destroy(ParticleSys);

    }
}
```

### Shared Components

> *** TODO: I need to test this to better understand how to use it***

A shared component group the entities in chunks based on the value of the component they share. For example with a *ComponentA* that has a int value, all *componentA* where the value is 1 will be in the same chunk, all *componentA* where the value is 2 will be in the same chunk, and so on. This means that changing the value of a shared component value is a structural changes since it will move entities in a new chunk that correspond to the new value.

> Each world store the shared component value in arrays separated from the ECS chunks, the chunks only store their indexes into these array so every unique shared component value is only stored once in a world.

**The major utility of shared component is to filter entities that share a specific component value when doing a query**. For example, we can query for all *ComponentA* and filter to keep only the entities where *ComponentA* has a *x* value.

It's possible to create both managed and unmanaged shared components but managed shared components keep all the disadvantages of regular managed components.

Implement the `ISharedComponentData` interface to create a shared component:

```c#
// An unmanaged shared component
public struct MySharedComponent : ISharedComponentData
{
    public int Value;
}

// A managed shared component
// Still a struct (not a class like regular managed component), as soon as it hold any managed type the share component is treated as unmanaged
// We need to also implement IEquatable<T> to ensure comparison will not generate managed allocations unnecessarily
public struct MyManagedSharedComponent : ISharedComponentData, IEquatable<MyManagedSharedComponent>
{
    public string Value; // a managed type

    public bool Equals(MyManagedSharedComponent other)
    {
        return Value.Equals(other.Value);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}
```

> It's possible to change how ECS compares instances of a shared component by implementing `IEquatable<T>` *T* being a shared component type. The `Equals()` and `GetHashCode()` methods added by the interface can be burst-compiled with the `[BurstCompile]` attribute as long as the component is unmanaged.

#### Use EntityManager with shared components

`EntityManager` has key methods that can be used with shared components:

- `AddComponent<T>()`: add *T* component, *T* can be a shared component
- `RemoveComponent<T>()`: remove *T* component, *T* can be a shared component
- `HasComponent<T>()`: return a bool to tell if the entity has *T* component, *T* can be a shared component
- `AddSharedComponent()`: add an unmanaged shared component to an entity and sets its initial value
- `AddSharedComponentManaged()`: add a managed shared component to an entity and sets its initial value.
- `GetSharedComponent<T>()`: retrieves the value of an entity's unmanaged shared *T* component.
- `SetSharedComponent<T>()`: overwrite the value of an entity's unmanaged shared *T* component.
- `GetSharedComponentManaged<T>()`: retrieves the value of an entity's managed shared *T* component.
- `SetSharedComponentManaged<T>()`: overwrite the value of an entity's managed shared *T* component.

> **Using Entities API to change a shared component value is mandatory**, especially when it include referenced objects. When the shared component contains a reference type or a pointer never modify directly the referenced object without using Entities API.

#### Optimize Shared Component

**Share shared components accross world**:  
Managed object that are resource intensive (such as [blob assets]()) can use shared component to store one copy of the object accross every worlds. To do that, implement [`IRefCounted`](https://docs.unity3d.com/Packages/com.unity.entities@1.3/api/Unity.Entities.IRefCounted.html) interface and it's `Retain()` and `Release()` methods to manage the lifetime of the underlying resource. When the component is unmanaged, we can use the `[BurstCompile]` attribute to improve performance.

**Use unmanaged shared components**:  
Use unmanaged shared components when you can and only use managed shared components when there is no other choice. Unmanaged shared components are stored in a place that is accessible to burst-compiled code which provide performance benefit when using the unmanaged shared component API (ex: [`SetUnmanagedSharedComponentData`](https://docs.unity3d.com/Packages/com.unity.entities@1.3/api/Unity.Entities.EntityManager.SetUnmanagedSharedComponentData.html)).

**Avoid frequent update**:  
Updating a shared component value is a structural change so it should not be done frequently.

**Avoid lots of unique shared component values**:  
When we have lots of unique shared component values, we create chunk fragmentation: all entities in a chunk must share the same shared components values, so having a lot of unique shared component values means entities are fragmented over lots of chunks that are almost empty.    
> Chunks fragmentation means we waste the space in each chunks and we have to loop accross a high number of chunks to loop through all entities which negates the benefit of ECS chunk layout and reduce performance.

**Be careful with archetypes having multiple shared components**:  
All entities in an archetype chunk must have the same combination of shared component value, so archetypes with multiple shared component types might cause fragmentation.

### Enableable Components

An [enableable components]() is a component that can be enabled or disabled at runtime without any structural change. When we query enableable components, only the components that are enabled are returned by the query so we only do the work on the entity where the component is enabled. Since there is no strutural change, this also means we can enable or disable components on jobs running on worker threads without using an entity command buffer or creating a sync point.

Enableable components only can be used on `IComponentData` and `IBufferElementData`. To make them eneable, we just need to make them inherits from `IEnableableComponent`.

```C#
public struct MyEnableableComponent : IComponentData, IEnableableComponent
{
    // ... component data
}

public struct MyEnableableBuffer : IBufferElementData, IEnableableComponent
{
    // ... buffer data
}
```

### Singleton components

A singleton component is basically a component that only has one instance in a given world. To create it we either can use the `EntityManager` or we can bake an entity that will be the only entity to hold that component in the world.

Here is an example where we create a singleton with the EntityManager in system and we access it in another system.

```C#
public partial struct MySystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Since a singleton can only have one instance, I ensure it doesn't already exist before
        if (SystemAPI.HasSingleton<ComponentA>() == false) 
        {
           ComponentA singletonComponent = new ComponentA { /* Set component default data */ };
           state.EntityManager.CreateSingleton(singletonComponent, "MySingleton"); // the string is a debug friendly name associated with the singleton
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // We can update the singleton here
        ComponentA updatedSingleton = new ComponentA { /* Update component data */ };
        SystemAPI.SetSingleton<ComponentA>(updatedSingleton); // Update the component itself
    }
}

// Another System that access the singleton
public partial struct AnotherSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Get Singleton to do something with them
        ComponentA singletonData = SystemAPI.GetSingleton<ComponentA>();
    }
}
```

#### When to use enableable components ?

Enableable components **should be used to avoid structural changes** or to **replace a set of tag components to represent states** (it reduce the number of unique entity archetypes and reduce memory consumption witha better chunk usage).

Enableable components are perfect for **when we need to change the state of a component often and unpredictably** or **when the number of state permutations are high on a frame-by-frame basis**.

However, if we expect the state will change at a low frequency or if it will persist for many frames it's better to add/remove components.

> ! To prevent a race conditions, jobs with write access to an enableable component might block main-thread operation until the job complete even if the job doesn't enable/disable the component on any entities.

#### How to work with enableable components ?

`EntityManager`, `ComponentLookup<T>`, `EntityCommandBuffer` and `ArchetypeChunk` all have specific method for enableable components:

- `IsComponentEnabled<T>(Entity e)`:
    - return *true* if entity has the component and it is enabled.
    - return *false* if entity has the component and it is disabled.
    - Asserts if the entity doesn't have the component or if the component doesn't implement `IEnableableComponent`.
- `SetComponentEnabled<T>(Entity e, bool enable)`:
    - if the entity has the component it is enabled/disabled based on *enable* bool.
    - Asserts if the entity doesn't have the component or if the component doesn't implement `IEnableableComponent`.

> `ComponentLookup<T>.SetComponentEnabled<T>(Entity e, bool enable)` can be used to safely enable/disable entities from worker thread since no structural change will happen but the job need to have a write access to component *T*. Avoid enable/disable a component on an entity that might be processed by a job on another thread to prevent generating a race condition.

#### Query enableable components

**A query requiring for a component *T* will consider an entity with a *T* component disabled as if it doesn't have the component** and the entity won't match the query. All `EntityQuery` methods automatically handle enableable components this way. For example, `query.CalculateEntityCount()` calculate the number of entity that match the query so entity with disabled component won't be taken into account.

There is 2 exceptions to that:
- **Methods that end with *IgnoreFilter* or *WithoutFilter* treat all components as if they are enabled**. These are generally more efficient than their filtering equivalent and won't require a sync point.
- **Queries created with `EntityQueryOptions.IgnoreComponentEnabledState` ignore the state(enabled/disabled)** of enableable components when determining if the entity match the query.


## Transform Components and Systems

> ***Complete with more note based on the doc, this is not completely clear yet for me and maybe move directly in its own .md file***

[Transform in entities documentation](https://docs.unity3d.com/Packages/com.unity.entities@1.2/manual/transforms-concepts.html)

`LocalTransform` is the main standard component that represent the transform of an entity.

### Create a transform hierarchy

A transform hierarchy similar to the GameObject hierarchy can be introduced by adding 3 additional components:
- `Parent`: component that store the id of the entity's parent
- `Child`: dynamic buffer component that store the ids of the entity's children
- `PreviousParent`: component that stored a copy of the id of the entity's parent

> ***Those components are already provided by Unity, we don't need to create them ourself (? Get more info on this: are they added by default on an entity or do we need to add them ourself ?)***

### Modify the hierachy

If we want to modify the hierarchy, we just have to use the parent component:

- Parent an entity: Add the entity a parent component
- De-Parent an entity: Remove the parent component from the entity
- Change the parent: Set the entity Parent Component

The `ParentSystem` will do the rest of the job by ensuring that:
- Every entity with a `Parent` component also has a `PreviousParent` component that references the parent.
- Every entity with one or more children has a `Child` buffer component that references its childrens.

We can read from the `Child` and `PreviousParent` components but we should not modify them directly. We should only modify the `Parent` component when modying a hierarchy.

> ***What is the `ParentSystem` ? Is this a default system ? when does it update ? I need to know more on this.***

### LocalToWorldSystem

Every frame a system called `LocalToWorldSystem` computes each entity world space transform from the `LocalTransform` of the entity and its ancestors and then assign it to the entity `LocalToWorld` component.

> Entity.Graphics systems read `LocalToWorld` but it doesn't read any other transform components. It is the only component an entity needs to be rendered.