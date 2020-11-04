using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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

        [Fact]
        public async Task ReplaceFactoryLocally()
        {
            var factory1 = new TestFactory(new object());
            var factory2 = new TestFactory(new object());

            await Task.WhenAll(
                Task.Run(() =>
                {
                    AvatarFactory.LocalDefault = factory1;
                    Thread.Sleep(50);
                    Assert.Same(factory1, AvatarFactory.Default);
                }),
                Task.Run(() =>
                {
                    AvatarFactory.LocalDefault = factory2;
                    Thread.Sleep(50);
                    Assert.Same(factory2, AvatarFactory.Default);
                })
            );

            Assert.NotSame(factory1, AvatarFactory.Default);
            Assert.NotSame(factory2, AvatarFactory.Default);
        }



        public class TestFactory : IAvatarFactory
        {
            object instance;

            public TestFactory(object instance) => this.instance = instance;

            public object CreateAvatar(Assembly assembly, Type baseType, Type[] implementedInterfaces, object?[] construtorArguments)
                => instance;
        }
    }
}
