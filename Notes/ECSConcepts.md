[Back to summary...](../)

# ECS Concepts

Summary:

- [Entity](#entity)
- [Component](#component)
- [Worlds](#world-collection-of-entities)
- [Entity Manager](#entity-manager)
- [Archetype and chunks](#archetype-and-chunks)
- [Entity metadata](#entity-metadata)
- [Query](#query)
- [Access entity and components with jobs](#access-entity-and-components-with-jobs)
- [Baking](#baking)
- [Aspects](#aspects)

Resources links:
- [EntityComponentSystemSamples github repository](https://github.com/Unity-Technologies/EntityComponentSystemSamples/tree/master?tab=readme-ov-file)
- [Document Unity Entities 101](https://docs.google.com/document/d/1R6E4IDpfLatwHITlCND0i5TuMVG0CMGsentFL-3RQT0/edit?tab=t.0)
- [Unity entities video](https://www.youtube.com/watch?v=jzCEzNoztzM)
- [Unity ECS concept documentation](https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/concepts-intro.html)

## [Entity](https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/concepts-entities.html)

- Lightweith unmanaged alternative to GameObject
- Has a unique int id number.
- Has components (but can only have one component of each type).
- No built in concept of parenting, instead the standard Parent component contains a reference to another entity allowing to create entity transform hierarchies.
- Stored in array which make it efficient to access in bulk with what is called a QUERY.

## [Component](https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/concepts-components.html)

- Must be a struct.
- Interface IComponentData has no method but mark struct as a component type.
- Component can contain only unmanaged data type and can reference to other entities in the form of an entity id.
- Component are meant to be purely data so they usually don't have any methods even if there no issue adding one.

>! It's actually possible to define managed component type that may contains other managed object by applying IComponentData to a class but they will have the same efficiency problems of gameObjects so it should be used unless strictly necessary.

[Detailed components notes are here.](Components.md)

## World (Collection of entities)
- To create an entity, we first need a world which is a container for entities.
- Every entity in a world has an ID that is unique in that world but an entity of another world might have the same ID.
- In most of the case one world is enough but creating several world can be interesting to create logical separation (ex: DOTS Net package create different world for server and clients)
- A world also owns [systems][systems], which are unit of codes that run on the main thread (usually once per frame). Generally, the entities of a world are only accessed by the world's systems and the jobs scheduled by them.

[systems]: Systems.md

## Entity manager
Manage the entities of a world, that what provide the methods to do changes on the entities. Here is a list of possible methods:

- Create an entity
- Destroy an entity
- Instantiate an entity (create a new entity with a copy of all the components of an existing entity)
- Add a component on an entity
- Remove a component on an entity
- Set a component on an entity
- Get a component on an entity

Those changes can be divided in two categories: Strutural and non-strutural changes.

- Structural change: operation that may modify the archetypes and chunks (Create, Instantiate and Destroy entity, Add and Remove component).
- Non-Structural change: operation that have no impact on the archetypes and chunks (Set and Get component).

> **!** Frequently moving many entities between archetypes may have a significant performance cost.

## Archetype and chunks

### Archetype

<p align="center"><img src="Images/archetypes.png" alt="visual representation of several archetype in a world"></p>  
Store all entities that have a specific set of component in a world.

Archetypes are automatically created by the entity manager when we create or modify entities, we don't have to create arfchetypes manually. If all entities are removed from an archetype, the archetype is not destroyed, an archetype is only destroyed when its world is destroyed.

>That's why adding or removing a component move the entity to a new archetype.

### Chunk

<p align="center"><img src="Images/chunk.png" alt="visual representation of a chunk"></p>  
Block of data with an uniform size that store entity and component inside an archetype (see https://youtu.be/jzCEzNoztzM?si=bqLii5e3EdTnh6bM&t=371).

- The number of entity contained in a chunk depends on the number and size its the components and a chunk can contain a maximum of 128 entities.
- A chunk is composed of an array for the entity ID and an array for each type of component.
> Example: for an entity with 3 components (A, B and C), the chunk will have an array for the entity ID and 3 arrays for the components (1 for A, 1 for B and 1 for C).
- Entities stored in a chunk are always thighly packed (=no empty slot in the array) at the beginning of the array so new entities are always placed at the first free slot available. If an entity is removed, the last entity is moved to fill the gap.
- Chunk creation and destruction is handled by the entity manager: a new chunk is created when an entity is added but all chunks are already full and a chunks is destroyed when its last entity is removed (this is a strutural change).

> Strutural changes on chunks can only be made on the main thread, the only way to do it with jobs is to use an `EntityCommandBuffer` as a workaround.

## Entity metadata

<p align="center"><img src="Images/entityMetadata.png" alt="visual representation of the entities metadata"></p>  
To allow to lookup entities by ID the world entity manager must maintain an array of entity metadata.

- each entity ID correspond to a slot in a metadata array.
- the slot contains:
    - a pointer to the chunk where the entity is stored, if no entity exist for a particular index the chunk pointer is null
    - the index of where the entity is stored within the chunk
    - a version ID, incremented everytime the entity at the index is destroyed to allow to reuse the entity index (if a the version ID doesn't match with the one already stored then the id must refers to an ID alreday destroyed or that may have never existed)

## Query

A request to efficiently find all entities with a specific set of component types. A query gather all the chunks which include the required component(s) regardless of the other components in chunk.

- In a query, we can require specific components and the query return all the chunks from the archetypes that contains the components.
- If we need to, we can also exclude specific components in the query to get the chunks only from archetypes that contains component(s) and doesn't contains other component(s).

> Example: With 3 Components (A, B and C) and 3 Archetypes (ABC, AB, AC).
>
>- If we query A and C -> we will get the chunks from archetypes ABC and AC (because they are the only one who contains A and C).
>- If we query A, C and we exclude B -> we will only get chunks from archetype AC (because it the only who contains A, C and doesn't contains B).

Archetypes matching a query are cached until a new archetypes is added to the world. Since the number of existing archetypes in world should stabilize early in the program lifetime, caching usually helps to make queries much faster.

## Access entity and components with jobs

To do that we can use 2 special jobs types:

- IJobEntity: iterate over entities matching a query (https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/iterating-data-ijobentity.html)
- IJobChunk: iterate over the chunk matching a query (https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/iterating-data-ijobchunk.html)

>In general IJobEntity is the most convenient way. IJobChunk will provide explicit low level control so it may be needed to fallback to it for some special cases not covered by IJobEntity.

[See here for detailed informations.](JobsWithECS.md)

## Baking

Entities cannot be directly included in unity scene so a build time process called baking convert the gameobjects into serialized entities.  
To add entities in a scene we create a subscene. One entity is created for each gameobject in a subscene and each component of each gameObject is processed by a Baker. The Baker is a class which add and set the component values of the entities.  
The result of the baking is serialized in a entity scene file which is loaded at runtime when the main scene is loaded.

>Baked entities can be further processed by a baking system before being serialized for more advanced use cases.

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


