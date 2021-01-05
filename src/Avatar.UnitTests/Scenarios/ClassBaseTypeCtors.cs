#pragma warning disable CS0436
using Xunit;

namespace Avatars.Scenarios.ClassBaseTypeCtors
{
    public class BaseTypeCtor
    {
        public BaseTypeCtor(string name) : this(name, true) { }

        protected BaseTypeCtor(string name, bool enabled)
            => (Name, Enabled)
            = (name, enabled);

        public string Name { get; private set; }

        public bool Enabled { get; private set; }
    }

    public class Test : IRunnable
    {
        public void Run()
        {
            var avatar = Avatar.Of<BaseTypeCtor>("Foo");

            Assert.Equal("Foo", avatar.Name);
            Assert.True(avatar.Enabled);

            avatar = Avatar.Of<BaseTypeCtor>("Foo", false);

            Assert.Equal("Foo", avatar.Name);
            Assert.False(avatar.Enabled);
        }
    }
}