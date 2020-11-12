#pragma warning disable CS0436
using System;
using Avatars;
using Xunit;


namespace Scenarios.MultipleInterfaces
{
    public class BaseType
    {
        public virtual void Run() { }
    }

    /// <summary>
    /// Can generate avatars for different interfaces implemented by 
    /// types, and there is no collision between them.
    /// </summary>
    public class Test : IRunnable
    {
        public void Run()
        {
            var a1 = Avatar.Of<BaseType>();
            var a2 = Avatar.Of<BaseType, IDisposable>();
            var a3 = Avatar.Of<BaseType, IDisposable, IServiceProvider>();

            Assert.False(a1 is IDisposable || a1 is IServiceProvider);
            Assert.True(a2 is IDisposable && !(a1 is IServiceProvider));
            Assert.True(a3 is IDisposable && a3 is IServiceProvider);
        }
    }
}
