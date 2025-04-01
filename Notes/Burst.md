[Back to summary...](../)

# Burst

Summary:
- [What is Burst compiler and how to use it?](#what-is-burst-compiler-and-how-to-use-it)
- [Get the most out of burst](#get-the-most-out-of-burst)
- [Other burst features](#other-burst-features)

Resources links:
- [Burst unity documentation](https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/index.html)
- [When, where, and why to put [BurstCompile]](https://discussions.unity.com/t/when-where-and-why-to-put-burstcompile-with-mild-under-the-hood-explanation/896228)
- [C# SIMD-accelerated types doc](https://learn.microsoft.com/en-us/dotnet/standard/simd)
- [Unity Learn: getting the most out of burst](https://learn.unity.com/tutorial/part-3-4-getting-the-most-out-of-burst?uv=2022.3&courseId=60132919edbc2a56f9d439c3&projectId=6013255bedbc2a2e590fbe60#)

## What is Burst compiler and how to use it?

Burst is a compiler than can used with DOTS (Data-Oriented Technology Stack) and especially the job system to improve our performance application. This compiler translate the IL/.Net code to an optimized CPU code that use the [LLVM compiler](https://llvm.org/).

To be able to correctly translate the code, the burst compiler use HPC# (a high performance subset of C#). Most C# expressions and statements are supported by HPC# but the code can only be related to unmanaged objects. Here is the full [list of C# supported and unsupported features in HPC#](https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/csharp-hpc-overview.html).

To enable the burst compiler we just need to add the `[BurstCompile]` attribute before the element that need to be burst-compiled, however there is some limitation to what can be burst-compiled.

### When and where we should we use burst compiler ?

There are 3 specific cases where the code can be burst compiled with `[BurstCompile]` attribute:

1. When defining a job (a struct implementing an interface `IJob...`), the attribute can be used before the job definition to burst compile the job.
2. When defining an unmanaged entity system (a struct implementing the `ISystem`), the attribute can be used before the definition of ISystem's `OnCreate()`, `OnUpdate()` and `OnDestroy()` methods to burst compile them.
3. When defining a static method, the attribute can used before the method definition to burst compile it.

> **Actually, Burst can only burst compile static methods** in the case of a job or an entity system some magic happen under the hood to make the code burst compilable, [see this developer post for more details](https://discussions.unity.com/t/when-where-and-why-to-put-burstcompile-with-mild-under-the-hood-explanation/896228).

### Burst compiled static method example

Here is an example of a static method using burst compilation:

```c#
[BurstCompile]
public static class MyBurstUtilityClass // a static class that keeps our burst-compiled static method the class must also have the [BurstCompile] attribute
{
    [BurstCompile] // the static method has the [BurstCompile] attribute. Since we use structs, they are passed as reference with in keyword
    public static void BurstCompiled_MultiplyAdd(in float3 mula, in float3 mulb, in float3 add, out float3 result)
    {
        result = mula * mulb + add;
    }
}
```

### What will be burst compiled ?

All the C# called from bursted code is bursted (unless we try really hard to do the opposite). That means that any method called inside burst compiled code will also be burst compiled even if the `[BurstCompile]` attribute was not used on its definition.

For example, in a job with `[BurstCompile]` attribute only the `Execute()` method is burst-compiled, so any method called inside `Execute()` will also be burst-compiled. However, any Non-Execute method in a job a that are not called from burst-compiled code will not use burst compilation even if `[BurstCompile]` is used on it (*the only exception is if the method is static since any static method can be burst compiled*).

## Get the most out of burst

https://learn.unity.com/tutorial/part-3-4-getting-the-most-out-of-burst?uv=2022.3&courseId=60132919edbc2a56f9d439c3&projectId=6013255bedbc2a2e590fbe60#

## Other burst features

### Check if code is burst compiled

If we need to check if some code is burst compiled we can use the following code (given by a dev working on Burst). However, this obsiously should not be used to change the behavior of a method since if we do turning burst off will change the behavior.

```C#
[BurstDiscard]
static void SetFalseIfUnBursted(ref bool val)
{
     val = false;
}

public static bool IsBursted()
{
    bool ret = true;
    SetFalseIfUnBursted(ref ret);
    return ret;
}
```

A unity user also proposed this shorter version which seems to have the same behaviour when testing it.

```c#
public static bool IsBursted => Unity.Burst.CompilerServices.Constant.IsConstantExpression(1);
```