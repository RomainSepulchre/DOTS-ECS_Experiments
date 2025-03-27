[Back to summary...](../)

# Components

Summary:

- [What is a component ?](#what-is-a-component-)
- [Types of components](#types-of-components)
- [Transform Components and Systems](#transform-components-and-systems)
- [Aspects](#aspects)

Resources links:
- [EntityComponentSystemSamples github repository](https://github.com/Unity-Technologies/EntityComponentSystemSamples/tree/master?tab=readme-ov-file)
- [Document Unity Entities 101](https://docs.google.com/document/d/1R6E4IDpfLatwHITlCND0i5TuMVG0CMGsentFL-3RQT0/edit?tab=t.0)
- [Unity entities video](https://www.youtube.com/watch?v=jzCEzNoztzM)
- [Unity ECS concept documentation](https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/components-intro.html)
- [Reference component types with Burst](https://nagachiang.github.io/unity-dots-how-to-reference-types-with-unmanaged-code/)

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

### Cleanup component

Cleanup component are like regular component, the difference is how they behave when we try to destroy an entity. When you destroy an entity that contains a cleanup component, instead of destroying the entity and its component the regular component are destroyed but the entity still exist with its cleanup components until we remove all its cleanup components. Another specificity of cleanup components is that when the entity it belong to is copied to another world, copied in serialization, or copied by the `Instantiate` method of EntityManager, the cleanup components are not copied to the new entity.

> A consequence of the cleanup component not being copied with an entity is that cleanup component added at baking time will not be serialized.

The primary use case for cleanup components is to help initialize an entity after it's creation and cleanup them after their destruction:
- For initialization, we can easily find entities that have a specific component but still doesn't have an adequate cleanup component to initialize them and then add the cleanup component.
- For destruction cleanup, we can easily find entities that still have the cleanup component but don't have a specific component anymore to do cleanup and then removing the cleanup component.

To declare a cleanup component there are different possibilities depending on what we want to do with it:
- Unmanaged cleanup component: struct implementing `ICleanupComponentData`
- Managed cleanup component: class implementing `ICleanupComponentData`
- Dynamic buffer cleanup component: struct implementing `ICleanupBufferElementData`
- Shared cleanup component: struct implementing `ICleanupSharedComponentData`

```C#
// An unmanaged cleanup component
public struct MyCleanupComponent : ICleanupComponentData
{
    // Component data
}

// A managed cleanup component
public class MyManagedCleanupComponent : ICleanupComponentData
{
    // Component data
}

// A dynamic buffer cleanup component
public struct MyBufferCleanupComponent : ICleanupBufferElementData
{
    // Component data
}

// A shared cleanup component
public struct MySharedCleanupComponent : ICleanupSharedComponentData
{
    // Component data
}
```

#### Lifecycle of cleanup component

1. We create an entity that contains a cleanup component or add a cleanup component to an existing entity.
2. When we don't need the entity anymore, we destroy the entity but only non-cleanable component are destroyed and the entity still exist since there is a cleanup component on the entity.
3. We do our cleanup and we remove the cleanup component, the entity is destroyed automatically when we remove the cleanup component.

```c#
// Creates an entity that contains a cleanup component.
Entity e = EntityManager.CreateEntity(typeof(ComponentA), typeof(ComponentB), typeof(MyCleanupComp));

// Destroy the entity but because the entity has a cleanup component only non-cleanable component are removed, Unity doesn't actually destroy the entity.
EntityManager.DestroyEntity(e);

// If we check, the entity still exists.
bool entityExists = EntityManager.Exists(e); // return true

// ... Do the cleanup on the entity

// Remove the cleanup component and the entity is automatically destroyed.
EntityManager.RemoveComponent(e, new ComponentTypeSet(typeof(ExampleCleanup), typeof(Translation)));

// If we check, the entity no longer exists.
entityExists = EntityManager.Exists(e); // return false
```

### Chunk component

A chunk component is a component that store value per chunk instead of per entity. It is similar to shared component since they both store one value per chunk but contrary to the shared component, the chunk component doesn't belong to entities, it belong to chunks. Unlike shared component, chunk components are stored directly in the chunk which means setting a chunk component isn't a structural change.

The main goal of chunk component is for optimization since it allows to run code at a per-chunk level to check if we need to do something with the entities it contains.

> Chunk components are always unmanaged, it's not possible to create managed chunk components.

A chunk is declared just like a regular unmanaged component, the difference is on how we add the component on an entity:

```c#
// A chunk component is declared like a regular unmanaged component
public struct MyChunkComponent : IComponentData
{
    // Some component data
}

//... In a system, we call a specific EntityManager method to add the component as a chunk component
EntityManager.AddChunkComponentData<MyChunkComponent>(entity); // This is a strutural change, the entity is moved to the  new chunk that hold our chunk component
```

**When we call `AddChunkComponentData<T>()` on an entity, the chunk component is not added to the chunk the entity belong to. A new  chunk that has our chunk component is created and the entity is moved to this new chunk** or if an archetype already exist with this chunk component, the entity is directly moved in a chunk in this archetype. **This is why adding or removing a chunk component is a  strutural changes that trigger a sync point.**

> Newly created chunk component are created with default values.

#### Use chunk component

`EntityManager` provide a set of methods to work with chunk components:

- `AddChunkComponentData<T>(entity||entityQuery)`: Add a chunk component of type *T* to the specified `Entity` or to chunks identified by an `EntityQuery` *(strutural change)*.
- `RemoveChunkComponent<T>(entity)`: Remove a chunk component of type *T* from the specified `Entity` *(strutural change)*.
- `RemoveChunkComponentData<T>(entityQuery)`: Remove a chunk component of type *T* from chunks identified by an `EntityQuery` *(strutural change)*.
- `HasChunkComponent<T>(entity)`: Check if the chunk containing the `Entity` has a chunk component of type *T*.
- `GetChunkComponentData<T>(entity||archetypeChunk)`: Get a chunk component of type *T* on chunk the `Entity` we specified belong to or on the chunk specified with `ArchetypeChunk`.
- `SetChunkComponentData<T>(archetypeChunk)`: Set the value of chunk component of type *T* on the chunk specified with `ArchetypeChunk`.

> ***It's seems that before it was possible to set a chunk component from an entity but it no longer seems to work?***

For a more concrete example [check Unity documentation on chunk components](https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/components-chunk-use.html).

**Use chunk component with jobs**  
Since jobs can't use `EntityManager`, to access a chunk component in a job we need to use a `ComponentTypeHandle` that we pass as a job argument.

```c#
struct MyJob : IJobChunk
{
    public ComponentTypeHandle<MyChunkComponent> MyChunkComponentHandle;

    public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
    {
        // Get chunk component
        MyChunkComponent myChunkComponent = chunk.GetChunkComponentData(MyChunkComponentHandle);

        // Set chunk component
        chunk.SetChunkComponentData(MyChunkComponentHandle, new MyChunkComponent { /* new component data */} );
    }
}
```

#### Query with chunk components

```c#
// ... in a system
// Using systemState
// -> We need to precise the component is a chunk component instead if using a simple typeof()
state.GetEntityQuery(ComponentType.ChunkComponent<MyChunkComponent>());

// Using SystemAPI
// -> There is variant of WithAll(), WithNone(), etc for chunk components
SystemAPI.QueryBuilder().WithAllChunkComponent<MyChunkComponent>().Build();
```

> When we define a query for chunk component, [`ComponentType`](https://docs.unity3d.com/Packages/com.unity.entities@1.0/api/Unity.Entities.ComponentType.html) give several options like `ComponentType.ChunkComponentReadOnly<T>()` when we only want to read the chunk component.

## Transform Components and Systems

> ***Complete with more note based on the doc, this is not completely clear yet for me and maybe move directly in its own .md file***

[Transform in entities documentation](https://docs.unity3d.com/Packages/com.unity.entities@1.2/manual/transforms-concepts.html)

`LocalTransform` is the main standard component that represent the transform of an entity. It represent the entity relative position, rotation and scale.

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

> The `ParentSystem` is a built-in system, initialized in a default world. It maintains the `Child` component buffer based on the `Parent` component of every children. When we update a `Parent` component on a child, the `Child` component od the parent is only updated once the `ParentSystem` has run.

> Enabling the static flag on everything that will not move improve performance and reduce memory consuption.

### LocalToWorldSystem

Every frame a system called `LocalToWorldSystem` computes each entity world space transform from the `LocalTransform` of the entity and its ancestors and then assign it to the entity `LocalToWorld` component.

> Entity.Graphics systems read `LocalToWorld` but it doesn't read any other transform components. It is the only component an entity needs to be rendered.

### Transform API

**[The ECS Transform equivalence of Unity Engine Transform property and method can be found here](https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/transforms-comparison.html).**

When working with `LocalTransform` there is no API method to modify the component value, all methods return a new value that must be assigned to the component. The only way to modify a `LocalTransform` is by writing to the Position, Rotation, and Scale properties.

For example if we want to rotate a transform around Z with `RotateZ()`:
```c#
localTransform = localTransform.RotateZ(angle); // we assign the result of RotateZ to the localTransform
```

We can also directly modify a property of the component itself. For example if we want to modify the position:
```c#
myTransform.Position += math.up();
// is the equivalent of
myTransform = myTransform.Translate(math.up());
```

#### [TransformHelpers](https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/transforms-helpers.html)

[`TransformHelpers`](https://docs.unity3d.com/Packages/com.unity.entities@1.3/api/Unity.Transforms.TransformHelpers.html) extension methods can be useful to work with transformation matrices, in particular with the `float4x4` contained in the `LocalToWorld` component.

**TransformPoint and InverseTransformPoint**  
For example we can minimize the use of matrix math by using `TransformPoint()` to tranform a point from local to world space:
```c#
// Get a world position from a local position using TransformPoint
float3 aWorldPosition = myLocalToWorld.Value.TransformPoint(myLocalPoint);

// We can also use InverseTransformPoint to get a local position from a world position
float3 aLocalPoint = myLocalToWorld.Value.InverseTransformPoint(myWorldPoint);
```

> The same thing can be done with a direction instead of a point by using `TransformDirection()` and `InverseTransformDirection()`.

**LookAtRotation**
`TransformHelpers.LookAtRotation()` compute a rotation so its *forward* points to the target:
```c#
float3 eyeWorldPosition = new float3(1, 2, 3);
float3 targetWorldPosition = new float3(4, 5, 6);
quaternion lookRotation = TransformHelpers.LookAtRotation(eyeWorldPosition, targetWorldPosition, math.up());
```

**ComputeWorldTransformMatrix**  

`ComputeWorldTransformMatrix` can be used to immediately use an entity's precise world transformation matrix. This is useful when we want to:

- Perform a raycast from an entity which might be part of a hierarchy (ex: the wheel of a car). the ray origin must be in world space but the entity's `LocalTransform` might be relative to its parent.
- Track another entity's transform in world-space with another entity with at least one of the entities, the targeted or targeting entity being in a transform hierachy.
- To compute a new `LocalToWorld` value for an entity in the `LateSimulationSystenmGroup` (after the `TransformSystemGroup` has updated be before the `PresentationSystemGroup` runs).

### Transform usage flags

[Transform usage flags](https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/transforms-usage-flags.html) controls how Unity will convert MonoBehaviour components to entity data. They can be used to define which transform should be added on the entity during the baking process and help to reduce unnecessary transform component on the baked entities.

More than one flag can be used on an Entity and Unity combine all the flags before adding the transform components.

Here is a list of every transform usage flags and their purpose:

- **None**: No specific transform component are required (other bakers on the GameObject can still add `TransformUsageFlags` value to the entity).
- **Renderable**: Require the necessary component to be rendered but doesn't require the transform components necessary to move the entity at runtime.
- **Dynamic**: Require the necessary components to move the entity at runtime.
- **WorldSpace**: The entity must be in world space even if it has a dynamic entity as parent.
- **NonUniformScale**: Requires transform component that represent a non uniform scale.
- **ManualOveride**: Ignore all `TransformUsageFlags` values from other bakers on the same GameObject. No transform components are added to the entity.

> The baker for default GameObject component automatically add the equivalent ECS transform usage flags to the baked entity. For example, a game object with a `MeshRenderer` component will be baked into an entity with the `Renderable` transform usage flag.

### Custom transform systems

If we need to add a specific transform functionality, [it is possible to customize the built-in transform system](https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/transforms-custom.html).

First of all, the built-in transform system use [write groups](Queries.md#write-groups) internally and we can configure them to ignore the entities we want to process with our custom transform system.

To create a custom transform system we must follow these steps:

1. Substitute the `LocalTransform` component.
    - Create a .cs file that define a substitute class for `LocalTrnasform`. The best way to do it is to copy *LocalTransform.cs* from the entities package and rename it.
    - Change the property and method to suit our needs.
2. Create an authoring component to receive our custom transforms.
    - The entities that will be processed by the custom system must: have our custom local transform substitute, have a `LocalToWorld` component and if it has a parent it must also have a `Parent` component that points to it.
    - The best way to ensure we meet these requirement is by adding an authoring component to each entities and use the `TransformUsageFlags.ManualOverride` to be sure the entity won't receive any built-in transform components.
3. Replace `LocalToWorldSystem`
    - Same as the `LocalTransform` substitute, we can copy *LocalToWorldSystem.cs* and rename it to create a substitute system that fit our needs.
    - In the system we replace all the occurence to the built-in `LocalTransform` by our substitute transfrom component.
    - We also need to remove `WithOptions(EntityQueryOptions.FilterWriteGroup);` from the queries to be sure the system won't exclude the corresponding entities like the built-in transform system.

## Aspects

An aspects is an object that allows to define a subset of entity components. It is useful to simplify queries and component related code: including an aspect in a query is the same as including all the components that are declared in the aspect. An aspect instance is also accessible in an `IJobEntity` or a `SystemAPI.Query()` loop.

An aspect is defined as a readonly partial struct that implement the interface `IAspect`.

```C#
readonly partial struct MyAspect : IAspect
{
    // The field that compose the aspect, since the struct is readonly they must also be declared as readonly
    public readonly RefRW<ComponentA> componentA;
    public readonly EnabledRefRW<ComponentB> componentB;

    // A field can be optionnal if it is declared with [Optional] attribute
    [Optional] public readonly RefRO<ComponentC> componentC;

    // To declare DynamicBuffer or nested aspect as read-only we can use the [ReadOnly] attribute
    [ReadOnly] public readonly DynamicBuffer<ComponentD> bufferD;

    // It's a good practice to declare private field and use public properties to access them
    // This is mainly for readability to prevent long chains when accessing a value (ex: aspect.AnotherAspect.ComponentA.ValueRW.myValue)
    readonly RefRW<LocalTransform> Transform;
    public float3 Position
    {
        get => Transform.ValueRO.Position;
        set => Transform.ValueRW.Position = value;
    }
}
```

> Unity also provide predefined aspects for groups of related components.

### Allowed type in an aspect

Only some specific types are allowed in a `IAspect` struct:

- `Entity` (an entity)
- `RefRw<T>`, `RefRO<T>` (a reference to component of type *T*)
- `EnabledRefRW<T>`, `EnabledRefRO<T>` (a reference to the enabled state of a component of type *T*)
- `DynamicBuffer<T>` (a dynamic buffer component of type *T*)
- `ISharedComponent` (an access to shared component value as read only)
- Another aspect (all the field contained in the aspect will be part of the parent aspect)

### Create and use the instance of an aspect

The `SystemAPI` and `EntityManager` provide methods to create instance of an aspect:
- `SystemAPI.GetAspect<T>(Entity)`
- `EntityManager.GetAspect<T>(Entity)`

```C#
// This will throw if the entity is missing any required component of the aspect
MyAspect asp = SystemAPI.GetAspect<MyAspect>(myEntity);

// Access components declared in the aspect
int value = asp.componentA.ValueRO.aValue
```

> Generally, it's recommended to use `SystemAPI` methods over `EntityManager` methods (`SystemAPI` register the underlying components types of the aspect with the system which allow to keep  track of the dependencies needed when scheduling a job).

It's also possible to pass an aspect in a `System.Query()` foreach loop or in an `IJobEntity` job. In the latter case or when we want to reference an aspect in code, we need to use the `in` and `ref` keyword. `in` will make all the field from the aspect read-only and `ref` will respect the read-only and read-write access defined in the aspect.

```c#
// In a SystamAPI.Query foreach
foreach (var cannonball in SystemAPI.Query<CannonBallAspect>())
{
    // use cannonball aspect here
}
```

```C#
// In an IJobEntity
[BurstCompile]
partial struct MyJob : IJobEntity
{
    void Execute(ref MyAspect myAspect)
    {
        // Do work on the components that compose the aspect
    }
}
```