using Xunit;

namespace Avatars.Scenarios.RefReturnsValue
{
    public interface IMemory
    {
        ref int Get();
    }

    /// <summary>
    /// Can assign ref value and change it.
    /// </summary>
    public class Test : IRunnable
    {
        public void Run()
        {
            var avatar = Avatar.Of<IMemory>();
            Ref<int> original = 12;

            avatar.AddBehavior((invocation, next) => invocation.CreateValueReturn(original));

            ref int value = ref avatar.Get();
            value = 42;

            // Original value changes too :)
            // Can implicitly assign the Ref<T> to a T 
            int actual = original;
            Assert.Equal(42, actual);
            Assert.Equal(42, original.Value);
        }
    }
}
