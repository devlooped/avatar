#pragma warning disable CS0436
using Xunit;
using Avatars;

namespace Scenarios.RefReturns
{
    public interface IMemory
    {
        ref int Get();
    }

    /// <summary>
    /// Ref returns works OOB with default value behaviors.
    /// </summary>
    public class Test : IRunnable
    {
        public void Run()
        {
            var avatar = Avatar.Of<IMemory>();
            avatar.AddBehavior(new DefaultValueBehavior());

            ref int value = ref avatar.Get();
            Assert.Equal(0, value);
        }
    }
}
