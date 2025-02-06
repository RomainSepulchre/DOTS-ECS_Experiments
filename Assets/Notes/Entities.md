# Unity Entities

## [Entity](https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/concepts-entities.html)

- Has an int id number.
- Has components (but can only have one component of each type).
- No built in concept of parenting.
- Stored in array which make it efficient to access in bulk with what is called a QUERY.

## [Component](https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/concepts-components.html)

- Must be a struct.
- Interface IComponentData has no method but mark struct as a component type.
- Component can contain only unmanaged data type and can reference to other entities in the form of an entity id.
- Component are meant to be purely data so they usually don't have any methods even if there no issue adding one.

There are differents types of component with different purpose, we can find all the types of component and how to use them [here](https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/components-type.html).

>! It's actually possible to define managed component type that may contains other managed object by applying IComponentData to a class but they will have the same efficiency problems of gameObjects so it should be used unless strictly necessary.

## World (Collection of entities)
- To create an entity, we first need a world which is a container for entities.
- Every entity in a world has an ID that is unique in that world but an entity of another world might have the same ID.
- In most of the case one world is enough but creating several world can be interesting to create logical separation (ex: DOTS Net package create different world for server and clients)

## Entity manager
Manage the entities of a world, that what provide the methods to do changes on the entities. Here is a list of possible methods:

- Create an entity
- Destroy an entity
- Add a component on an entity
- Remove a component on an entity
- Set a component on an entity
- Get a component on an entity

Those changes can be divided in two categories: Strutural and non-strutural changes.

- Structural change: operation that may modify the archetypes and chunks (Create and Destroy entity, Add and Remove component).
- Non-Structural change: operation that have no impact on the archetypes and chunks (Set and Get component).

## Archetype and chunks

### Archetype

<p align="center"><img src="Images/archetypes.png" alt="visual representation of several archetype in a world"></p>  
Store all entities that have a specific set of component in a world.

>That's why adding or removing a component move the entity to a new archetype.

### Chunk

<p align="center"><img src="Images/chunk.png" alt="visual representation of a chunk"></p>  
Block of data with an uniform size that store entity and component inside an archetype (see https://youtu.be/jzCEzNoztzM?si=bqLii5e3EdTnh6bM&t=371).

- The number of entity contained in a chunk depends on the number and size its the components and a chunk can contain a maximum of 128 entities.
- A chunk is composed of an array for the entity ID and an array for each type of component.
> Example: for an entity with 3 components (A, B and C), the chunk will have an array for the entity ID and 3 arrays for the components (1 for A, 1 for B and 1 for C).
- Entities stored in a chunk are always thighly packed (=no empty slot in the array) at the beginning of the array so new entities are always placed at the first free slot available. If an entity is removed, the last entity is moved to fill the gap.

## Entity metadata

<p align="center"><img src="Images/entityMetadata.png" alt="visual representation of the entities metadata"></p>  
To allow to lookup entities by ID the world entity manager must maintain an array of entity metadata.

- each entity ID correspond to a slot in a metadata array.
- the slot contains:
    - a pointer to the chunk where the entity is stored, if no entity exist for a particular index the chunk pointer is null
    - the index of where the entity is stored within the chunk
    - a version ID, incremented everytime the entity at the index is destroyed to allow to reuse the entity index (if a the version ID doesn't match with the one already stored then the id must refers to an ID alreday destroyed or that may have never existed)

## Query

A request to efficiently find all entities with a specific set of component types.

- We can query for specific components and the query return all the archetypes that contains the components.
- Query can require specific components but can also exclude some components.

> Example: With 3 Components (A, B and C) and 3 Archetypes (ABC, AB, AC).
>
>- If we query A and C -> we will get archetypes ABC and AC (because they are the only one who contains A and C).
>- If we query A, C and we exclude B -> we will only get archetype AC (because it the only who contains A, C and doesn't contains B).

## Access entity and components with jobs

To do that we can use 2 special jobs types:

- IJobEntity: iterate over entities matching a query (https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/iterating-data-ijobentity.html)
- IJobChunk: iterate over the chunk matching a query (https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/iterating-data-ijobchunk.html)

>In general IJobEntity is the most convenient way. IJobChunk will provide explicit low level control so it may be needed to fallback to it for some special cases not covered by IJobEntity.


## Baking

Entities cannot be directly included in unity scene so a build time process called baking convert the gameobjects into serialized entities.  
To add entities in a scene we create a subscene. One entity is created for each gameobject in a subscene and each component of each gameObject is processed by a Baker. The Baker is a class which add and set the component values of the entities.  
The result of the baking is serialized in a entity scene file which is loaded at runtime when the main scene is loaded.  
>Baked entities can be further processed by a baking system before being serialized for more advanced use cases.

