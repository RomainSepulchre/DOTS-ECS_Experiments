[Back to summary...](../)

# Baking

Summary:
- [What is baking ?](#what-is-baking-)
- [Baker](#baker)
- [Baking Systems](#baking-systems)
- [Baking Worlds](#baking-worlds)
- [Filter baking output](#filter-baking-output)

Resources links:
- [Baking Unity doucmentation](https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/baking.html)

## What is baking ?

The baking is the process that convert unity game objects to entities.

Classic game objects are composed of authoring data (the data created during application editing) and runtime data (the data used at runtime). This gives a lot of flexibity which is great for editing but not needed at runtime and furthermore imply a performance cost since unity process both types of data at once.

ECS is designed in a more efficient way, where we avoid to process any data that is not required and to be able to do that it separate the authoring and runtime data. That's why we need the baking, it's what convert the authoring data to runtime data.

In ECS, game objects are called authoring game objects, they have authoring components and are in contained in authoring scenes (Sub-scene). During the conversion to entity the authoring components are converted to ECS components. All the game object and data contained in an authoring scene are converted to ECS during baking.

The baking is too heavy to happen in game so it only happens in the editor. It is triggered whenever authoring data in an authoring scene changes.

### Baking phases

Baking have multiple phases but the the two main phases are [**Bakers**](#baker) and [**Baking systems**](#baking-systems).

When we bake a scene, here is what is happening:
1. Entity are created for every authoring gameObject in a subscene. The entity doesn't have any component yet.
2. Unity run the **Bakers**, a baker processes authoring component to convert them into ECS components. 
3. Unity runs the **Baking Systems**, which perform additional operations on the entities we process. 

### Types of baking

There is two types of baking: full baking and incremental baking. Depending on wether the scene is open or closed and to how much authoring data there is to process we use one or the other.

**Closed subscene** always use a full baking because the full scene will be imported.

**Open subscene** means we are doing live baking, unity bakes authoring data into ECS while we work on it. So depending on how much of authoring data unity needs to process it either does incremental baking or a full baking.

#### Full Baking

Unity process the entire scene and bakes it. The full baking is done asynchronously in the background.

#### Incremental Baking

Unity only bakes the data that is modified. We can directly access the result of the baking while editing an authoring scene.

## Baker

A baker process authoring component to convert them into ECS components.

Here are some rules for bakers:
- A baker process a specific authoring component type but multiple baker can process the same authoring component type.
- A baker can only change its own entity, trying to access and modify another entity at this stage breaks the logic.
- There is no guarantee for the order on which the baker will be processed so it's not possible to create interdependency between bakers.

Unity has default bakers included with the entities packages that automatically deal with some authoring component types. For example the Entities Graphics package comes with bakers for renderers and the Unity Physics package has baker for rigid bodies.

### Create a baker

```c#
// A simple component to add in the baker
public struct MyComponent : IComponentData
{
    public int Value;
}

// The authoring component class (By convention its name should always end by Authoring)
// To follow monobehaviour convention it must in a file called MyComponentAuthoring.cs
public class MyComponentAuthoring : MonoBehaviour
{
    public int value;
}

// The baker takes the authoring class as input and can access its data in the Bake method throught the authoring parameter
class MyComponentBaker : Baker<MyComponentAuthoring>
{
    public override void Bake(MyComponentAuthoring authoring)
    {
        Entity entity =  GetEntity(TransformUsageFlags.Dynamic);

        // We add component on the entity
        MyComponent myComponent = new MyComponent()
        {
            Value = authoring.value;
        };
        AddComponent(entity, myComponent);
    }
}
```

### Accessing other data sources in a baker

The incremental baking works because the baker automatically track the data we use when baking the game object. Any field in the authoring component is automatically tracked and the backer re-runs if the data changes. However, the baker does not track data from other sources such as other authoring components or assets.

So, for example if my authoring component has a field that point a `Mesh` component, the baker will track if I change the component linked in the field but it won't track any changes made directly on the linked component.

To fix this issue, in our baker we can use `DependsOn()` to assign a dependency to our baker to tell him he need to track this dependency data.

```c#
// A simple authoring component with a GameObject field
public class MyComponentAuthoring : MonoBehaviour
{
    public GameObject go;
}

// The baker takes the authoring class as input and can access its data in the Bake method throught the authoring parameter
class MyComponentBaker : Baker<MyComponentAuthoring>
{
    public override void Bake(MyComponentAuthoring authoring)
    {
        // We make the baker dependant on the gameObject linked in our field so the baker re-runs when the gameObject data is updated
        DependsOn(authoring.go); 

        // By doing this, when checking for missing component on the gameObject the baker will re-run every time I add or remove a component
        // Otherwise the baker would only have re-run if one of the authoring field has been updated
        var meshRenderer = GetComponent<MeshRenderer>(authoring.go)
        if(meshRenderer != null)
        {
            // Add a ComponentA when the GameObject has a MeshRenderer
            Entity entity =  GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<ComponentA>(entity);
        }
    }
}
```

>When using `DependsOn()` on an asset, if the asset goes missing while still being linked in a field, when it comes back the call to `DependsOn()` will automatically trigger the baker.

>`GetComponent<>()` also registers a dependency to the required component, so when the component is added or removed the baker is triggered.

## Baking Systems

A baking system is a system that perform additional operations on the entities processed during the baking.

A baking system is a classic ECS system, the only difference is a baking system is marked a specific baking system attribute (`[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]`) that tell to only run it during the baking. The features are similar to other ECS systems but their update is only called during a baking.

Like other systems, baking systems can be ordered with `[UpdateAfter()]`, `[UpdateBeffore()]` and `[UpdateInGroup()]` attributes. The default groups provided by Unity are:
- `PreBakingSystemGroup`: Execute before entity creation and bakers (it's the only group that runs before bakers)
- `TransformBakingSystemGroup`: Run just after the bakers but before `BakingSystemGroup`.
- `BakingSystemGroup `: Default baking system group 
- `PostBakingSystemGroup`: Run after `BakingSystemGroup`

Baking Systems can alter their world in any way, they can even create new entities. However, entities created in a baking system will not end up in a baked entity scene. The entities created in a baking system can be used to transfer data between several baking systems but if we want to keep the entity in our baked entity scene we must create it in a baker. In a baker `CreateAdditionalEntity` allow to create and configure an entity to make it work with baking and live baking.

### Create a baking system

```c#
[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)] // This attribute define the system as a baking system
public partial struct MyBakingSystem : ISystem
{
    public void OnUpdate(ref SystemState state) // in baking systems Update is called at every single baking pass 
    {
        // We can get entity created and add them a new component
        // Here we add ComponentB on every entity with the ComponentA
        EntityQuery query = SystemAPI.QueryBuilder().WithAll<ComponentA>().WithNone<ComponentB>().Build();
        state.EntityManager.AddComponent<ComponentB>(cubeQuery); 

        // We also need to add a way to clean ComponentB because when the ComponentA is removed the entity no longer should have ComponentB on the entity
        // Not doing this would lead to inconsistent results during live baking.
        EntityQuery cleanupQuery = SystemAPI.QueryBuilder().WithAll<ComponentB>().WithNone<ComponentA>().Build();
        state.EntityManager.RemoveComponent<ComponentB>(cleanQuery); // Remove the ComponentB on every entity without ComponentA
    }
}
```

### Dependencies with baking systems

Baking systems don't track depedencies and structural changes automatically and we have to declare dependencies explicitely. We also need manually track and revert changes when we add/remove ECS components to keep a coherent incremental baking.

That's the reason why, in the previous example, we have a *cleanupQuery* that ensure we remove the added component if the entity no longer meet the requirement.

## Baking Worlds

Entity scenes are baked in isolation and one scene is processed at a time. When entity scenes are opened in the editor and are live baked, unity use separate worlds to isolate entity scene from each other.

Each live baked subscene relies on 2 worlds:
- *Conversion world*: it's the world where the baking is happening, the bakers and the baking systems run here. To limit the work done by the live baking, the result of the baking stays in the conversion world as long as the subscene is openned.
- *Shadow world*: a world with a copy of the previous baking output. After a new baking, the shadow world is compared with the new baking output to see what changed and only the ECS components affected by the changes are copied to the main world.

## Filter baking output

By default, any game object in a subscene will be converted to entity in the conversion world and be part of the output baking. However, sometimes some game objects from the authoring scene are not relevant as entity in the baked scene (ex: control points for a spline).

> ***? When testing it seems that a gameObjects need to have at least one authoring component to be converted to entity, did I miss something ?***

### Filter entity

When a game object is not relevant in the baking output, it's possible to exclude its entity from the baking output by adding a `BakingOnlyEntity` tag component to an entity in a baker (or by adding `BakingOnlyEntityAuthoring` on the game object), the entity isn't stored in the entity scene and is never merged to the main world.

### Filter components

It's also possible to exclude components by using a baking type attribute:
    - `[BakingType]`: any component marked with this attribute is filtered out of the baking output.
    - `[TemporaryBakingType]`: any component marked with this attribute is destroyed from the baking output. This means the component will not remain from one baking pass to the next or to spell it differently the component marked with `[TemporaryBakingType]` will only exist when the baker that adds it has run in the same baking pass. So a baking system that require a temporary baked component will only run when something force the baker that add the temporary baked component to re-bake (ex: change in the inputs data).

## Prefab baking

During the baking, gameObjects prefabs are baked into entity prefabs. An entity prefab is an entity with the following components:
- `Prefab` component tag (identify the entity as a prefab and exclude it from queries by default),
- `LinkedEntityGroup` buffer (Stores the prefab childrens in a flat list, it allows to quickly create the whole set of entity without having to go through all the hierarchy).

By default, all entities with the `Prefab` tag are excluded from query but they can be included back by using the query option `EntityQueryOptions.IncludePrefab`:
```C#
EntityQuery queryWithPrefab = SystemAPI.QueryBuilder().WithAll<ComponentA>.WithOptions(EntityQueryOptions.IncludePrefab).Build();
```

Entity prefabs works like game objects prefabs. As long as they have been baked and are available in the entity scene, entity prefabs can be instantiated at runtime.

> Prefabs that are already in the subscene hierarchy are considered as normal game objects (they don't have the `Prefab` tag and the `LinkedEntityGroup` buffer).

### Bake a prefab

To ensure a prefab is baked and available in the entity scene thay must be registered into a baker. Registering a prefab only require to call `GetEntity(GameObject prefab, TransformUsageFlags)`, however to instantiate it later it's better to store the entity prefab in a component.

```c#
// A component to store the prefab
public struct EntityPrefabComponent : IComponentData
{
    public Entity Value;
}

// Authoring component class with input GameObject prefab
public class GetPrefabAuthoring : MonoBehaviour
{
    public GameObject Prefab;
}

// A baker where the prefab is registered
public class GetPrefabBaker : Baker<GetPrefabAuthoring>
{
    public override void Bake(GetPrefabAuthoring authoring)
    {
        // Register the Prefab in the Baker
        Entity entityPrefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic);

        // Store the entity in a component to instantiate it later
        Entity entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new EntityPrefabComponent() {Value = entityPrefab});
    }
}
```

When an entity prefab will be used in several subscene it can be stored in an `EntityPrefabReference` struct. By doing this, the ECS content of the prefab is serialized into a separate entity scene that can be loaded at runtime when we need to use the prefab. This allows us to avoid a duplication of the entity prefab in every subscene where it is used.

```c#
// A component to store the prefab reference
public struct EntityPrefabReferenceComponent : IComponentData
{
    public EntityPrefabReference Value;
}

// Authoring component class with input GameObject prefab
public class GetPrefabReferenceAuthoring : MonoBehaviour
{
    public GameObject Prefab;
}

// A baker that registers the EntityPrefabReference
public class GetPrefabReferenceBaker : Baker<GetPrefabReferenceAuthoring>
{
    public override void Bake(GetPrefabReferenceAuthoring authoring)
    {
        // When an EntityPrefabReference is created from a GameObject, the prefab is serialized in its own entity scene file to avoid prefab duplication
        EntityPrefabReference entityPrefab = new EntityPrefabReference(authoring.Prefab);

        // Store the EntityPrefabReference in a component to instantiate it later
        Entity entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new EntityPrefabReferenceComponent() {Value = entityPrefab});
    }
}
```

### Instantiate a prefab

To instantiate a prefab we use the `EntityManager` or an `EntityCommandBuffer`:

- `EntityManager`: use `EntityManager.Instantiate()`, can only be used on the main thread since it triggers a structural change.
- `EntityCommandBuffer`: record a `EntityCommandBuffer.Instantiate()` command in an `EntityCommandBuffer` to playback it later, can be used on the main thread or in a job. [See this fore more info on EntityCommandBuffer](JobsWithECS.md#entity-command-buffer).

> Instantiated prefabs contain a `SceneSection` component that could affect the lifetime of entities

#### Instantiate from an EntityPrefabReference

To instantiate a prefab by using a `EntityPrefabReference` we need to make sure the prefab is loaded before we can use it since it is serialized into a separate entity scene.

To do that we must add the `RequestEntityPrefabLoaded` component to the entities that contains a `EntityPrefabReference`. This component make sure the prefab is loaded and store the result of the loading into a `PrefabLoadResult` component (this component is automatically added to the entity that has `RequestEntityPrefabLoaded`).

=>  Check unity ECS samples project to see if there is a working example of this
```c#
//
// ?? WEIRD THINGS ARE HAPPENING WHEN I'M TRYING TO TEST THE CODE ??
// - It seems RequestEntityPrefabLoaded doesn't automatically get our EntityPrefabReference like the doc sample code suggest
// - When setting RequestEntityPrefabLoaded manually, the prefab scene loading fails (Exception: (Loading Entity Scene failed because the entity header file couldn't be resolved.))
// -> Its look like the doc is outdated or is incomplete
//
public partial struct InstantiatePrefabReferenceSystem : ISystem, ISystemStartStop
{
    public void OnStartRunning(ref SystemState state)
    {       
        // Query all entities with a component that store an EntityPrefabReference and doesn't have a PrefabLoadResult yet
        EntityQuery query = SystemAPI.QueryBuilder().WithAll<EntityPrefabComponent>().WithNone<PrefabLoadResult>().Build();

        
        // Add to all entity in the query a RequestEntityPrefabLoaded component to load the prefab
        state.EntityManager.AddComponent<RequestEntityPrefabLoaded>(query);
        // After doing this unity load the prefab and store them in a PrefabLoadResult component on the entity
        // Note: it might take a few frames for the prefab to be loaded
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // For every entities with a loaded prefab result instantiate the prefab
        foreach (var (prefab, entity) in SystemAPI.Query<RefRO<PrefabLoadResult>>().WithEntityAccess())
        {
            var instance = ecb.Instantiate(prefab.ValueRO.PrefabRoot);

            // Remove both RequestEntityPrefabLoaded and PrefabLoadResult to prevent the prefab being loaded and instantiated multiple times, respectively
            ecb.RemoveComponent<RequestEntityPrefabLoaded>(entity);
            ecb.RemoveComponent<PrefabLoadResult>(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
```

### Destroy prefab instances

Prefab instances are destroyed like any other entity, by using the `EntityManager` or an `EntityCOmmandBuffer` to call `.DestroyEntity()`