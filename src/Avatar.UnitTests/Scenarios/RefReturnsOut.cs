#pragma warning disable CS0436
using Avatars;
using Xunit;

namespace Scenarios.RefReturnsOut
{
    public interface IMemory
    {
        ref int Get(ref string name, out int count);
    }

    /// <summary>
    /// Ref returns works OOB with out parameters and default value behaviors.
    /// </summary>
    public class Test : IRunnable
    {
        public void Run()
        {
            var avatar = Avatar.Of<IMemory>();
            avatar.AddBehavior(new DefaultValueBehavior());

            var name = "foo";
            ref int value = ref avatar.Get(ref name, out var _);

            Assert.Equal(0, value);
        }
    }
}
