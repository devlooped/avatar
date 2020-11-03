using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;
using Avatars;

namespace Scenarios.CompilerGenerated
{
    public class BaseType { }

    /// <summary>
    /// Generated types have the [CompilerGenerated] attribute.
    /// </summary>
    public class Test : IRunnable
    {
        public void Run()
        {
            Assert.NotNull(Avatar.Of<BaseType>().GetType().GetCustomAttribute<CompilerGeneratedAttribute>());
            Assert.NotNull(Avatar.Of<IDisposable>().GetType().GetCustomAttribute<CompilerGeneratedAttribute>());
        }
    }
}
