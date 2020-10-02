using System;
using System.Collections.Generic;
using Sample;
using Xunit;

namespace Stunts.UnitTests
{
    public class StuntNamingTests
    {
        [Fact]
        public void SimpleName()
        {
            var name = StuntNaming.GetName(typeof(ICalculator));

            Assert.Equal(nameof(ICalculator) + StuntNaming.DefaultSuffix, name);
        }

        [Fact]
        public void FullName()
        {
            var name = StuntNaming.GetFullName(typeof(ICalculator));

            Assert.Equal(StuntNaming.DefaultNamespace + "." + nameof(ICalculator) + StuntNaming.DefaultSuffix, name);
        }

        [Fact]
        public void FullNameWithNamespaceAndInterfaces()
        {
            var name = StuntNaming.GetFullName("Test", typeof(ICalculator), typeof(IDisposable), typeof(IServiceProvider));

            Assert.Equal("Test.ICalculatorIDisposableIServiceProvider" + StuntNaming.DefaultSuffix, name);
        }

        [Fact]
        public void FullNameWithInterfaces()
        {
            var name = StuntNaming.GetFullName(typeof(ICalculator), typeof(IDisposable), typeof(IServiceProvider));

            Assert.Equal(StuntNaming.DefaultNamespace + ".ICalculatorIDisposableIServiceProvider" + StuntNaming.DefaultSuffix, name);
        }

        [Fact]
        public void GenericConstructedName()
        {
            var name = StuntNaming.GetName(typeof(HashSet<ICalculator>));

            Assert.Equal($"HashSetOf{nameof(ICalculator)}{StuntNaming.DefaultSuffix}", name);
        }

        [Fact]
        public void GenericName()
        {
            var name = StuntNaming.GetName(typeof(IDictionary<,>));

            Assert.Equal($"IDictionaryOfTKeyTValue{StuntNaming.DefaultSuffix}", name);
        }

        [Fact]
        public void TwoGenericNames()
        {
            var name = StuntNaming.GetName(typeof(KeyValuePair<string, ICalculator>));

            Assert.Equal($"KeyValuePairOfString{nameof(ICalculator)}{StuntNaming.DefaultSuffix}", name);
        }

        [Fact]
        public void GenericOfGenericName()
        {
            var name = StuntNaming.GetName(typeof(ICollection<HashSet<ICalculator>>));

            Assert.Equal($"ICollectionOfHashSetOf{nameof(ICalculator)}{StuntNaming.DefaultSuffix}", name);
        }

        [Fact]
        public void GenericOfTwoGenericNames()
        {
            var name = StuntNaming.GetName(typeof(ICollection<KeyValuePair<string, ICalculator>>));

            Assert.Equal($"ICollectionOfKeyValuePairOfString{nameof(ICalculator)}{StuntNaming.DefaultSuffix}", name);
        }
    }
}
