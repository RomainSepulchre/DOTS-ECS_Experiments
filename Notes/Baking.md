[Back to summary...](../)

# Baking

Summary:
- [What is baking ?](#what-is-baking-)
- [Baker](#baker)
- [Baking Systems](#baking-systems)

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

## Baking Systems

A baking system is a system that perform additional operations on the entities processed during the baking.

A baking system is a classic ECS system, the only difference is a baking system is marked a specific baking system attribute (`[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]`) that tell they only run during the baking. The features are similar to other ECS systems but their update is only called during a baking.

Like other systems, baking systems can be ordered with `[UpdateAfter()]`, `[UpdateBeffore()]` and `[UpdateInGroup()]` attributes. The default groups provided by Unity are:
- `PreBakingSystemGroup`: Execute before entity creation and bakers (it's the only group that runs before bakers)
- `TransformBakingSystemGroup`: Run just after the bakers but before `BakingSystemGroup`.
- `BakingSystemGroup `: Default baking system group 
- `PostBakingSystemGroup`: Run after `BakingSystemGroup`

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
        EntityQuery cleanQuery = SystemAPI.QueryBuilder().WithAll<ComponentB>().WithNone<ComponentA>().Build();
        state.EntityManager.RemoveComponent<ComponentB>(cleanQuery); // Remove the ComponentB on every entity without ComponentA
    }
}
```
