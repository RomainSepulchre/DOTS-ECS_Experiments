[Back to summary...](../)

# Baking

Summary:
- [What is baking ?](#what-is-baking-)
- [Baker](#baker)
- [Baking Systems](#baking-systems)
- [Baking Worlds](#baking-worlds)
- [Filter baking output](#filter-baking-output)
- [Prefab baking](#prefab-baking)
- [Scenes](#scenes)

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

```c#
// The component that store our EntityPrefabReference, the value is set in a dedicated authoring script like shown previously in Bake a prefab section
public struct PrefabReference : IComponentData
{
    public EntityPrefabReference Value;
}

// A system to load prefab reference 
public partial struct LoadPrefabReferenceSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {       
        // Only run thsi system update once
        state.Enabled = false;

        // Query the entities with an EntityPrefabReference to load
        EntityQuery query = SystemAPI.QueryBuilder().WithAll<PrefabReference>().WithNone<PrefabLoadResult>().Build();
    
        //  When adding RequestEntityPrefabLoaded we set its prefab value with the EntityPrefabReference to load
        NativeArray<PrefabReference> prefabReferences = query.ToComponentDataArray<PrefabReference>(Allocator.Temp);
        NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);

        for (int i = 0; i < entities.Length; i++)
        {
            // The Prefab field of RequestEntityPrefabLoaded must be set with the EntityPrefabReference stored in PrefabReference
            RequestEntityPrefabLoaded requestEntityPrefabLoaded = new RequestEntityPrefabLoaded()
            {
                Prefab = prefabReferences[i].Value
            };
            state.EntityManager.AddComponentData(entities[i], requestEntityPrefabLoaded);
        }

        // After doing this unity load the prefab and store them in a PrefabLoadResult component on the entity
        // Note: it might take a few frames for the prefab to be loaded
        // -> The loading is abnormally longer when done in OnStartRunning() compared to when it's done in dedicated like we do here
    }
}

// The system that instantiate the loaded entity prefab
public partial struct InstantiatePrefabReferenceSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Only update this system when there is at least one PrefabLoadResult
        state.RequireForUpdate<PrefabLoadResult>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Instantiate every prefab loaded from the PrefabReference component
        foreach (var (prefab, entity) in SystemAPI.Query<RefRO<PrefabLoadResult>>().WithAll<PrefabReference>().WithEntityAccess())
        {
            var instance = ecb.Instantiate(prefab.ValueRO.PrefabRoot);

            // Remove both RequestEntityPrefabLoaded and PrefabLoadResult to load and instantiate the prefab only once
            ecb.RemoveComponent<RequestEntityPrefabLoaded>(entity);
            ecb.RemoveComponent<PrefabLoadResult>(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
```
> **Common error faced when trying this**:
> - *System.InvalidOperationException: Invalid Entity.Null passed* and *LoadSceneAsync - Invalid sceneGUID* => the `EntityPrefabReference` in RequestEntityPrefabLoaded doesn't point to a valid entity, it probably because the Prefab field in RequestEntityPrefabLoaded was not correctly set when adding the component.
> - *Loading Entity Scene failed because the entity header file couldn't be resolved.* => Clear the entity cache to fix the issue (Preferences > entities > Clear Entity Cache)

### Destroy prefab instances

Prefab instances are destroyed like any other entity, by using the `EntityManager` or an `EntityCOmmandBuffer` to call `.DestroyEntity()`

### LinkedEntityGroup

A `LinkedEntityGroup` is a Dynamic Buffer with a special semantic:
- Instantiating (`EntityManager.Instantiate`): Instantiate all the entities in the `LinkedEntityGroup`.
- Destroying (`EntityManager.Instantiate`): Destroys all the entities in the `LinkedEntityGroup`.
- Enabling/Disabling (`EntityManager.SetEnabled`): Add/Remove the `Disabled` component tag on all the entities in the `LinkedEntityGroup`.

> The first element of a `LinkedEntityGroup` must alway be the entity with the `LinkedEntityGroup` buffer.

A `LinkedEntityGroup` and a transform hierarchy are two different concepts:
- Adding a children on an entity with a `LinkedEntityGroup` will not add it in the `LinkedEntityGroup`
- Removing an entity from a `LinkedEntityGroup` doesn't remove it from the childrens off the parent entity.

`LinkedEntityGroup` are not processed recursively, when processing a `LinkedEntityGroup` if it contains an entity with another `LinkedEntityGroup`, this second buffer will not be processed. Only the content of the processed `LinkedEntityGroup` is actually processed. It's recommended to avoid nested group to prevent any confusion.

#### Add an entity to a LinkedEntityGroup

To add a new entity in a `LinkedEntityGroup` we first need to add the corresponding scene tag shared component (from the entity that has the LinkedEntityGroup), if needed we can then add a Parent component to make the new entity a children in the transform hierarchy. Finally, we can add our entity in the `LinkedEntityGroup` buffer.

```c#
// In a ISystem
public void OnUpdate(ref SystemState state)
{
    // Get the entity with the LinkedEntityGroup we want to modify
    EntityQuery query = SystemAPI.QueryBuilder().WithAll<MyComponent>().WithAll<LinkedEntityGroup>().Build();
    NativeArray<Entity> entity = query.ToEntityArray(Allocator.Temp);

    // Create the new entity
    Entity newChild = state.EntityManager.CreateEntity();

    // Get the SceneTag from the entity with the LinkedEntityGroup and add ot to our new children
    SceneTag sceneTag = state.EntityManager.GetSharedComponent<SceneTag>(entity[0]); 
    state.EntityManager.AddSharedComponent<SceneTag>(child, sceneTag);

    // If needed, add a Parent component to make the new entity a child in the transform hierarchy 
    state.EntityManager.AddComponentData(child, new Parent { Value = entity[0] });

    // Get the LinkedEntityGroup and add the new child entity
    LinkedEntityGroup leg = SystemAPI.GetBuffer<LinkedEntityGroup>(entity[0]);
    leg.Add(child);

    // Disable system to only do this once 
    state.Enabled = false;
}
```

> Making sure the new entity has the correct scene tag is really important because unity use it when it unloads an entity scene to identify the entity that should be destroyed. [See the next section about LinkedEntityGroup destruction for more info](#destroy-entities-from-a-linkedentitygroup).

#### Destroy entities from a LinkedEntityGroup

A `LinkedEntityGroup` must only contains valid entities, if an entity part of a `LinkedEntityGroup` is destroyed individually it must also be manually removed from the group.

When using a query to destroy entities, the content of a `LinkedEntityGroup` can't partially match the query: all the entities in the `LinkedEntityGroup` must match the query otherwise it's like none of them match. That's especially relevant when using entity scenes: Unity use the scene tag shared component to identify entities that must be destroyed when a scene is unloaded. If the scene tag of an entity that is part of a `LinkedEntityGroup` has not been set correctly, the `LinkedEntityGroup` will not a full match with the query and the entities in the `LinkedEntityGroup` will not be destroyed.

## Scenes

When working with ECS, we use 3 differents types of scenes:
- **Authoring scene**: A scene that we can open and edit (like a normal scene) that is destined to be baked. It contains gameObjects and monobehaviour components (authoring components) that will be converted into entities and entity components during the baking.
- **Entity scene**: A scene that is the result of the baking of an authoring scene. It only contains ECS Data (entities and entity components).
- **Subscene**: A subscene is a reference to an authoring or entity scene. It's a gameObject component that allows to load a scene either in its gameObject authoring representation (for editing) or as its ECS representation (read-only but performant). Subscenes can be confused with entity scenes but they are just an attachment point to easily load an entity scene.

### Scene Streaming

Since loading large scenes can take several frames, the scene loading is done asynchronously to avoid stall. This is called **streaming**.

**Streaming advantages and disadvantage:**
- (+) Application stays responsive since scene are streamed in the background.
- (+) Scene can be dynamically loaded/unloaded in large seamless worlds (larger than what the memory can actually fit) with no gameplay interruptions.
- (+) In play mode, even if an entity scene file is missing or outdated the editor remain responsive since the baking an loading happen asynchronously in a different process.
- (-) The app can't assume some scene is present especially at startup which makes a code a bit mre complicated.
- (-) Scene are loaded from the scene system group (that is part of Initialization group), so only systems that updateds later in the same frame will the loaded data this frame, systems updated earlier will only receive the data next frame. Our code might need to take this into consideration.

### Load a scene

When we need to load a scene we can use a subscene placed in the main scene or use the [`SceneSystem`](https://docs.unity3d.com/Packages/com.unity.entities@1.3/api/Unity.Scenes.SceneSystem.html) API to load the scene from code.

The static method to load a scene asynchronously is `SceneSystem.LoadSceneAsync()`. This can only be done in the `OnUpdate()` method of a system.

All the versions of that method need to receive a unique identifier as parameter to know which scene must be loaded.  
This unique identifier must be one of the following:
- An [`EntitySceneReference`](https://docs.unity3d.com/Packages/com.unity.entities@1.3/api/Unity.Entities.Serialization.EntitySceneReference.html).
- **NOT RECOMMENDED**, a [`Hash128`](https://docs.unity3d.com/Packages/com.unity.entities@1.3/api/Unity.Entities.Hash128.html) GUID.
- A `scene meta Entity`.

> Using a GUID is not recommended because the build process only detects authoring scene referenced by subscene or `EntitySceneReference`, meaning their entity scenes will be missing from the builds.

When an `EntitySceneReference` or a `Hash128` GUID is used as parameter to load a scene, `SceneSystem.LoadSceneAsync()` returns the  `scene meta Entity`. This can be in subsequent calls to refer to the scene and is very useful to unload and reload the content of scene for example.

### Advice: Split big scene in several smaller scenes

When a project has a large amount of data it may be hard for Unity to process it if all the data is within a single authoring scene. The issue isn't the number of entities, Unity can handle millions of them, it is their GameObjects representation that could force the editor to stall. With large amount of data, it's more efficient to keep authoring data into several smaller authoring scenes.

For example, in the [Megacity project sample](https://github.com/Unity-Technologies/Megacity-2019), each building is in a separate subscene to efficiently manage their gameObject representation but we can still load the whole city.

In some case , when working in a small environment, a single authoring scene is enough but in general multiple authoring scene can easily be baked independantly and loaded together.

