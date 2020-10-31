using System;
using System.Reflection;
using Xunit;

namespace Avatars.UnitTests
{
    public class AvatarFactoryTests
    {
        [Fact]
        public void NotImplementedFactoryThrows()
            => Assert.Throws<NotImplementedException>(() => AvatarFactory.NotImplemented.CreateAvatar(Assembly.GetExecutingAssembly(), typeof(IDisposable), Array.Empty<Type>(), Array.Empty<object>()));

        [Fact]
        public void ReplaceDefaultFactory()
        {
            var instance = new object();
            var factory = new TestFactory(instance);

            AvatarFactory.Default = factory;

            var actual = AvatarFactory.Default.CreateAvatar(
                Assembly.GetExecutingAssembly(),
                typeof(object),
                new[] { typeof(IFormatProvider) },
                Array.Empty<object>());

            Assert.Same(instance, actual);
        }

        public class TestFactory : IAvatarFactory
        {
            object instance;

            public TestFactory(object instance) => this.instance = instance;

            public object CreateAvatar(Assembly stuntsAssembly, Type baseType, Type[] implementedInterfaces, object?[] construtorArguments)
                => instance;
        }
    }
}
