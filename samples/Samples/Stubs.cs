using System.Linq;
using Avatars;
using Xunit;

namespace Samples
{
    public class Stubs
    {
        [Fact]
        public void StubObjects()
        {
            var calculator = Stub.Of<ICalculator>();

            // Default behavior would return the default value for an int.
            Assert.Equal(0, calculator.Add(2, 3));

            // Use introspection API to explore configured behaviors
            var avatar = (IAvatar)calculator;
            var recorder = avatar.Behaviors.OfType<RecordingBehavior>().Single();

            // Assert against the recorded invocations.
            Assert.Single(recorder.Invocations);
        }
    }

    public static class Stub
    {
        [AvatarGenerator]
        public static T Of<T>() => Avatar.Of<T>()
            .AddBehavior(new DefaultEqualityBehavior())
            .AddBehavior(new RecordingBehavior())
            .AddBehavior(new DefaultValueBehavior());
    }
}
