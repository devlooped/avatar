using System;
using System.Reflection;
using Sample;
using Stunts.Sdk;
using Xunit;

namespace Stunts.UnitTests
{
    public class DynamicProxyTests
    {
        public void test() => Console.WriteLine(typeof(DynamicProxyFactory).AssemblyQualifiedName);

        [Fact]
        public void TestFactory()
        {
            var factory = new DynamicProxyFactory();


            var calculator = (ICalculator)factory.CreateStunt(Assembly.GetExecutingAssembly(), 
                typeof(ICalculator), Array.Empty<Type>(), Array.Empty<object>());

            var recorder = new RecordingBehavior();
            calculator.AddBehavior(recorder);

            calculator.AddBehavior(
                (m, n) => new MethodReturn(m, "foo", null!),
                m => m.MethodBase.Name == "ToString",
                "ToString");

            calculator.AddBehavior(
                (m, n) => new MethodReturn(m, 42, null!),
                m => m.MethodBase.Name == "GetHashCode",
                "GetHashCode");

            calculator.AddBehavior(
                (m, n) => new MethodReturn(m, true, null!),
                m => m.MethodBase.Name == "Equals",
                "Equals");

            Assert.Equal("foo", calculator.ToString());
            Assert.Equal(42, calculator.GetHashCode());
            Assert.True(calculator.Equals("foo"));
            Assert.Throws<NotImplementedException>(() => calculator.Add(2, 3));

            Assert.Equal(4, recorder.Invocations.Count);
        }
    }
}
