using System;
using Avatars;
using Xunit;

namespace Sample
{
    public class Tests
    {
        [Fact]
        public void CanConfigureDefaultValues()
        {
            var calculator = Avatar.Of<ICalculator, IDisposable>();

            Assert.IsNotType<StaticAvatarFactory>(AvatarFactory.Default);

            var recorder = new RecordingBehavior();

            calculator.AddBehavior(recorder);
            calculator.AddBehavior(new DefaultValueBehavior());

            Assert.IsAssignableFrom<IDisposable>(calculator);

            Assert.Equal(0, calculator.Add(5, 10));
            Assert.Single(recorder.Invocations);

            Console.WriteLine(recorder.ToString());
        }
    }
}
