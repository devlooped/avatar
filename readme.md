<h1 id="avatar"><img src="https://github.com/kzu/avatar/raw/main/docs/images/icon.png" alt="Icon" height="48" width="48" style="vertical-align: text-top; border: 0px; padding: 0px; margin: 0px">  Avatar</h1>

Avatar is a modern interception library which implements the [proxy pattern](https://en.wikipedia.org/wiki/Proxy_pattern) and runs everywhere, even where run-time code generation (Reflection.Emit) is forbidden or limitted, like physical iOS devices and game consoles, through compile-time code generation. The proxy behavior is configured in code using what we call a *behavior pipeline*. 

> *Avatars blend in with the Na'vi seamlessly, and you can control their behavior precisely by 'driving' them through a psionic link. Just like a [proxy](https://en.wikipedia.org/wiki/Proxy_pattern), with behavior driven through  code.*

![Avatar Overloads](https://github.com/kzu/avatar/raw/main/docs/images/AvatarIncubation.png)

[![Version](https://img.shields.io/nuget/vpre/Avatar.svg?color=royalblue)](https://www.nuget.org/packages/Avatar)
[![Downloads](https://img.shields.io/nuget/dt/Avatar?color=darkmagenta)](https://www.nuget.org/packages/Avatar)
[![License](https://img.shields.io/github/license/devlooped/avatar.svg?color=blue)](https://github.com/devlooped/avatar/blob/main/LICENSE)
[![Discord Chat](https://img.shields.io/badge/chat-on%20discord-7289DA.svg)](https://discord.gg/AfGsdRa)
[![GitHub](https://img.shields.io/badge/-source-181717.svg?logo=GitHub)](https://github.com/devlooped/avatar)

[![CI Version](https://img.shields.io/endpoint?url=https://shields.kzu.io/vpre/devlooped/main&label=nuget.ci&color=brightgreen)](https://pkg.kzu.io/index.json)
[![GH CI Status](https://github.com/kzu/avatar/workflows/build/badge.svg?branch=main)](https://github.com/devlooped/avatar/actions?query=branch%3Amain+workflow%3Abuild+)


> NOTE: Avatar provides a fairly low-level API with just the essential building blocks on top of which higher-level APIs can be built, such as the upcoming Moq vNext API.

## Requirements

Avatar is a .NET Standard 2.0 library and runs on any runtime that supports that. 

Compile-time proxy generation leverages [Roslyn source generators](https://github.com/dotnet/roslyn/blob/master/docs/features/source-generators.cookbook.md) and therefore requires C# 9.0, which at this time is included in Visual Studio 16.8 (preview or later) and the .NET 5.0 SDK (RC or later). Compile-time generated proxies support the broadest possible run-time platforms since they don't require any Reflection.Emit, and also don't pay that performance cost either.

Whenever compile-time proxy generation is not available, a fallback generation strategy is used instead, which leverages [Castle DynamicProxy](https://github.com/castleproject/Core/blob/master/docs/dynamicproxy-introduction.md) to provide the run-time code generation.

The client API for configuring proxy behaviors in either case is exactly the same. 

> NOTE: even though generated proxies is the main usage for Avatar, the API was designed so that you can also consume the behavior pipeline easily from hand-coded proxies too.

## Usage

```csharp
ICalculator calc = Avatar.Of<ICalculator>();

calc.AddBehavior((invocation, next) => ...);
```

`AddBehavior`/`InsertBehavior` overloads allow granular control of the avatar's behavior pipeline, which is basically a [chain of responsibility](https://en.wikipedia.org/wiki/Chain-of-responsibility_pattern) that invokes all configured behaviors that apply to the current invocation. Individual behaviors can determine whether to short-circuit the call or call the next behavior in the chain. 

![Avatar Overloads](https://github.com/kzu/avatar/raw/main/docs/images/AddInsertBehavior.png)

Behaviors can also dynamically determine whether they apply to a given invocation by providing the optional `appliesTo` argument. In addition to the  delegate-based overloads (called *anonymous behaviors*), you can also create behaviors by implementing the `IAvatarBehavior` interface:

```csharp
public interface IAvatarBehavior
{
    bool AppliesTo(IMethodInvocation invocation);
    IMethodReturn Execute(IMethodInvocation invocation, ExecuteHandler next);
}
```

## Common Behaviors

Some commonly used behaviors that are generally useful are provided in the library and can be added to avatars as needed:

* `DefaultValueBehavior`: sets default values for method return and *out* arguments. In addition to the built-in supported default values, additional default value factories can be registered for any type.

* `DefaultEqualityBehavior`: implements the *Object.Equals* and *Object.GetHashCode* members just like *System.Object* implements them.

* `RecordingBehavior`: simple behavior that keeps track of all invocations, for troubleshooting or reporting.

## Customizing Avatar Creation

If you want to centrally configure all your avatars, the easiest way is to simply provide your own factory method (i.e. `Stub.Of<T>`), which in turn calls the `Avatar.Of<T>` provided. For example:

```csharp
    public static class Stub
    {
        [AvatarGenerator]
        public static T Of<T>() => Avatar.Of<T>()
            .AddBehavior(new RecordingBehavior())
            .AddBehavior(new DefaultEqualityBehavior())
            .AddBehavior(new DefaultValueBehavior());
    }
```

The `[AvatarGenerator]` attribute is required if you want to leverage the built-in compile-time code generation, since that signals to the source generator that calls to your API end up creating an avatar at run-time and therefore a generated type will be needed for it during compile-time. You can actually explore how this very same behavior is implemented in the built-in Avatar API which is provided as a content file:

![avatar API source](https://github.com/kzu/avatar/raw/main/docs/images/AvatarApi.png)

The `Avatar.cs` contains, for example:

```csharp
[AvatarGenerator]
public static T Of<T>(params object[] constructorArgs) => Create<T>(constructorArgs);

[AvatarGenerator]
public static T Of<T, T1>(params object[] constructorArgs) => Create<T>(constructorArgs, typeof(T1));
```

As you can see, the Avatar API itself uses the same extensibility mechanism that your own custom factory methods can use.

### Static vs Dynamic Avatars

Depending on the project and platform, Avatars will automatically choose whether to use run-time proxies or compile-time ones (powered by Roslyn source generators). The latter are only supported when building C# 9.0+ projects.

You can opt out of the static avatars by setting `EnableCompiledAvatars=false` in your project file:

```xml
<PropertyGroup>
    <EnableCompiledAvatars>false</EnableCompiledAvatars>
</PropertyGroup>
```

This will switch the project to run-time proxies based on Castle.Core.

## Debugging Optimizations

There is nothing more frustrating than a proxy you have carefully configured that doesn't behave the way you expect it to. In order to make this a less frustrating experience, avatars are carefully optimized for debugger display and inspection, so that it's clear what behaviors are configured, and invocations and results are displayed clearly and concisely. Here's the debugging display of the `RecordingBehavior` that just keeps track of invocations and their return values for example:

![debugging display](https://github.com/kzu/avatar/raw/main/docs/images/DebuggerDisplay.png)

And here's the invocation debugger display from an anonymous behavior:

![behavior debugging](https://github.com/kzu/avatar/raw/main/docs/images/DebuggingBehavior.png)

## Samples

The `samples` folder in the repository contains a few interesting examples of how *Avatar* can be used to implement some fancy use cases. For example:

* Forwarding calls to matching interface methods/properties (by signature) to a static class. The example uses this to wrap calls to *System.Console* via an *IConsole* interface.

* Forwarding calls to a target object using the DLR (that backs the *dynamic* keyword in C#) API for high-performance late binding. 

* Custom `Stub.Of<T>` factory that creates avatars that have common behaviors configured automatically.

* Custom avatar factory method that adds an int return value randomizer.

* Configuring the built-in *DefaultValueBehavior* so that every time a string property is retrieved, it gets a random lorem ipsum value.

* Logging all calls to an avatar to the Xunit output helper.



## Sponsors

<h3 style="vertical-align: text-top" id="by-clarius">
<img src="https://raw.githubusercontent.com/devlooped/oss/main/assets/images/sponsors.svg" alt="sponsors" height="36" width="36" style="vertical-align: text-top; border: 0px; padding: 0px; margin: 0px">&nbsp;&nbsp;by&nbsp;<a href="https://github.com/clarius">@clarius</a>&nbsp;<img src="https://raw.githubusercontent.com/clarius/branding/main/logo/logo.svg" alt="sponsors" height="36" width="36" style="vertical-align: text-top; border: 0px; padding: 0px; margin: 0px">
</h3>

*[get mentioned here too](https://github.com/sponsors/devlooped)!*
