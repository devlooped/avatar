using System;
using System.Reflection;
using Xunit;

namespace Stunts.UnitTests
{
    public class StuntFactoryTests
    {
        [Fact]
        public void NotImplementedFactoryThrows()
            => Assert.Throws<NotImplementedException>(() => StuntFactory.NotImplemented.CreateStunt(Assembly.GetExecutingAssembly(), typeof(IDisposable), Array.Empty<Type>(), Array.Empty<object>()));

        [Fact]
        public void ReplaceDefaultFactory()
        {
            var instance = new object();
            var factory = new TestFactory(instance);

            StuntFactory.Default = factory;

            var actual = StuntFactory.Default.CreateStunt(
                Assembly.GetExecutingAssembly(),
                typeof(object),
                new[] { typeof(IFormatProvider) },
                Array.Empty<object>());

            Assert.Same(instance, actual);
        }

        public class TestFactory : IStuntFactory
        {
            object instance;

            public TestFactory(object instance) => this.instance = instance;

            public object CreateStunt(Assembly stuntsAssembly, Type baseType, Type[] implementedInterfaces, object?[] construtorArguments)
                => instance;
        }
    }
}
