using UnityEngine;

public class Notes_Entities
{

    // Entity
    // - has an int id number
    // - has components (but can only have one component of each type)
    // - No built in concept of parenting
    // - Stored in array which make it efficient to access in bulk with what is called a QUERY

    // Component
    // Must be a struct
    // interface IComponentData has no method but mark struct as a component type
    // Component can contain only unmanaged data type and can reference to other entities in the form of an entity id
    // Component are meant to be purely data so they usually don't have any methods even if there no issue adding one
    // ! It's actually possible to define managed component type that may contains other managed object by applying IComponentData to a class
    //   but they will have the same efficiency problems of gameObjects so it should be used unless strictly necessary

    // World (Collection of entities)
    // To create entity we first need a world which is a container for entity
    // Every entity in a world has an ID that is unique in that world but an entity of another world might have the same ID
    // In most of the case one world is enough but creating several world can be interesting to create logical separation (ex: DOTS Net package create different world for server and clients)

    // Entity manager
    // Manage the entity of a world
    // -> Strutural change: Create and Destroy entity, Add and Remove component
    // -> Non Stuctural change: Set, Get component

    // Archetype and chunks
    // Archetype: Store all entities that have a specific set of component in a world
    // -> So adding or removing a component move the entity to a new archetype
    // Chunk: Block of data with an uniform size that store entity and component inside an archetype (https://youtu.be/jzCEzNoztzM?si=bqLii5e3EdTnh6bM&t=371)
    // -> the number of entity contained in a chunk depends on the number and size its the components and a chunk can contain a maximum of 128 entities.
    // -> a chunk is composed of an array for the entity ID and an array for each type of component
    //   (Ex: For entity with 3 components A,B,C, the chunk will have an array for the entity ID and 3 arrays for the components (1 for A, 1 for B and 1 for C)
    // -> Entities stored in a chunk are always thighly packed (=no empty slot in the array) at the beginning of the array so new entities are always placed at the first free slot available
    //   and if an entity is removed, the last entity is moved to fill the gap

    // Entity metadata
    // To allow to lookup entities by ID the world entity manager must maintain an array of entity metadata
    // -> each entity ID correspond to a slot in a metadata array
    // -> the slot contains:
    //    - a pointer to the chunk where the entity is stored, if no entity exist for a particular index the chunk pointer is null
    //    - the index of where the entity is stored within the chunk
    //    - a version ID, incremented everytime the entity at the index is destroyed to allow to reuse the entity index
    //      -> if a the version ID doesn't match with the one already stored then the id must refers to an ID alreday destroyed or that may have never existed

    // Query
    // Request to efficiently find all entities with a specific set of component types
    // -> We can query for specific components and the query return all the archetypes that contains the components
    // -> Query can require specific components but can also exclude components
    // -> Ex: With 3 Components (A, B and C) and 3 Archetypes (ABC, AB, AC)
    //    - If we query A and C -> we will get archetypes ABC and AC (because they are the only one who contains A and C)
    //    - If we query A, C and we exclude B -> we will only get archetype AC (because it the only who contains A, C and doesn't contains B

    // Access entity and components with jobs 
    // To do that we can use 2 special jobs types:
    // - IJobChunk, iterate over the chunk matching a query (https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/iterating-data-ijobchunk.html)
    // - IJobEntity, iterate over entities matching a query (https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/iterating-data-ijobentity.html)
    // In general IJobEntity is the most convenient way. IJobChunk will provide explicit low level control so it may be needed to fallback to it for some special cases not covered by IJobEntity.

    // Baking
    // Entities cannot be directly included in unity scene so a build time process called baking convert the gameobjects into serialized entities
    // To add entities in a scene we create a subscene. One entity is created for each gameobject in a subscene and each component of each gameObject is processed by a Baker.
    // The Baker is a class which add and set the component values of the entities.
    // The result in serialized in a entity scene file which is loaded at runtime when the main scene is loaded.
    // Baked entities can be further process by a beking system before being serialized for more advanced use cases.
}
