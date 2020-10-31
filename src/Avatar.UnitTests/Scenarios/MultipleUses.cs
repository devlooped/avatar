using System;
using Xunit;

namespace Avatars.Scenarios
{
    /// <summary>
    /// Multiple uses of the avatar factory method only result in one 
    /// such type being generated (IWO, no compilation errors because 
    /// of duplicate types.
    /// </summary>
    public class MultipleUses : IRunnable
    {
        public void Run()
        {
            var disposable = Avatar.Of<IDisposable>();
            var services = Avatar.Of<IServiceProvider>();

            Assert.NotNull(disposable);
            Assert.NotNull(services);

            Do();
        }
        
        public void Do()
        {
            var disposable2 = Avatar.Of<IDisposable>();
            var services2 = Avatar.Of<IServiceProvider>();

            Assert.NotNull(disposable2);
            Assert.NotNull(services2);
        }
    }
}
