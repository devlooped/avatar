using Avatars;
using Xunit;

namespace Samples
{
    public class Debugging
    {
        /// <summary>
        /// Set a breakpoint in the last line of the method and inspect the 
        /// avatar instance to see how Avatar optimizes and improves the 
        /// troubleshooting and inspection of running avatars and their 
        /// invocations.
        /// </summary>
        [Fact]
        public void DebuggerDisplay()
        {
            var calc = Avatar.Of<ICalculator>();
            var recorder = new RecordingBehavior();

            calc.AddBehavior(recorder);
            calc.AddBehavior(new DefaultEqualityBehavior());

            calc.AddBehavior(
                (invocation, next) => invocation.CreateValueReturn((int)invocation.Arguments[0]! + (int)invocation.Arguments[1]!),
                invocation => invocation.MethodBase.Name == nameof(ICalculator.Add),
                "Add");

            calc.AddBehavior(new DefaultValueBehavior());
            calc.GetHashCode();

            Assert.False(calc.Equals(new object()));
            Assert.Equal(5, calc.Add(3, 2));
            Assert.Equal(10, calc.Add(5, 5));

            calc.Store("Mem1", 50);
            calc.Recall("Mem1");

            var calls = recorder.Invocations;
        }
    }
}
