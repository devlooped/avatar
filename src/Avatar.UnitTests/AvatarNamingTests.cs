using System;
using System.Collections.Generic;
using Sample;
using Xunit;

public interface IGlobal { }

namespace Avatars.UnitTests
{
    public class AvatarNamingTests
    {
        [Fact]
        public void SimpleName()
        {
            var name = AvatarNaming.GetName(typeof(ICalculator));

            Assert.Equal(nameof(ICalculator) + AvatarNaming.DefaultSuffix, name);
        }

        [Fact]
        public void FullName()
        {
            var name = AvatarNaming.GetFullName(typeof(ICalculator));

            Assert.Equal($"{AvatarNaming.DefaultRootNamespace}.{typeof(ICalculator).Namespace}.{nameof(ICalculator)}{AvatarNaming.DefaultSuffix}", name);
        }

        [Fact]
        public void FullNameWithNamespaceAndInterfaces()
        {
            var name = AvatarNaming.GetFullName("Test", typeof(ICalculator), typeof(IDisposable), typeof(IServiceProvider));

            Assert.Equal($"Test.{typeof(ICalculator).Namespace}.ICalculatorIDisposableIServiceProvider" + AvatarNaming.DefaultSuffix, name);
        }

        [Fact]
        public void FullNameWithInterfaces()
        {
            var name = AvatarNaming.GetFullName(typeof(ICalculator), typeof(IDisposable), typeof(IServiceProvider));
            
            Assert.Equal($"{AvatarNaming.DefaultRootNamespace}.{typeof(ICalculator).Namespace}.ICalculatorIDisposableIServiceProvider{AvatarNaming.DefaultSuffix}", name);
        }

        [Fact]
        public void NamespaceForGlobalType()
        {
            var name = AvatarNaming.GetFullName(typeof(IGlobal));

            Assert.Equal($"{AvatarNaming.DefaultRootNamespace}.{nameof(IGlobal)}{AvatarNaming.DefaultSuffix}", name);
        }

        [Fact]
        public void GenericConstructedName()
        {
            var name = AvatarNaming.GetName(typeof(HashSet<ICalculator>));

            Assert.Equal($"HashSetOf{nameof(ICalculator)}{AvatarNaming.DefaultSuffix}", name);
        }

        [Fact]
        public void GenericName()
        {
            var name = AvatarNaming.GetName(typeof(IDictionary<,>));

            Assert.Equal($"IDictionaryOfTKeyTValue{AvatarNaming.DefaultSuffix}", name);
        }

        [Fact]
        public void TwoGenericNames()
        {
            var name = AvatarNaming.GetName(typeof(KeyValuePair<string, ICalculator>));

            Assert.Equal($"KeyValuePairOfString{nameof(ICalculator)}{AvatarNaming.DefaultSuffix}", name);
        }

        [Fact]
        public void GenericOfGenericName()
        {
            var name = AvatarNaming.GetName(typeof(ICollection<HashSet<ICalculator>>));

            Assert.Equal($"ICollectionOfHashSetOf{nameof(ICalculator)}{AvatarNaming.DefaultSuffix}", name);
        }

        [Fact]
        public void GenericOfTwoGenericNames()
        {
            var name = AvatarNaming.GetName(typeof(ICollection<KeyValuePair<string, ICalculator>>));

            Assert.Equal($"ICollectionOfKeyValuePairOfString{nameof(ICalculator)}{AvatarNaming.DefaultSuffix}", name);
        }
    }
}
