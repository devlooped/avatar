#pragma warning disable CS0436
using Xunit;

namespace Avatars.Scenarios.ClassBaseTypeInternalCtor
{
    public class BaseTypeInternalCtor
    {
        internal BaseTypeInternalCtor(string name) => Name = name;

        public string Name { get; private set; }
    }

    public class Test : IRunnable
    {
        public void Run()
        {
            var avatar = Avatar.Of<BaseTypeInternalCtor>("Foo");

            Assert.Equal("Foo", avatar.Name);
        }
    }
}