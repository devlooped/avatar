using Avatars;
using Samples;
using Xunit;

namespace Samples.TargetInvocation
{
    public class DynamicTargetTests
    {
        [Fact]
        public void InvokeReturningTarget()
        {
            var target = new Calculator();

            ICalculator calc = Avatar.Of<ICalculator>();
            var recorder = new RecordingBehavior();

            // By adding the recorder *after* the dynamic target, 
            // we can check if any calls where made to the avatar
            // instead of the target.
            calc.AddBehavior(new DynamicTargetBehavior(target))
                 .AddBehavior(recorder);

            var result = calc.Add(2, 3);

            // We recorded the call to the avatar, but the 
            // dynamic target behavior passed the call through
            // to the real calculator which did the math.
            Assert.Empty(recorder.Invocations);
            Assert.Equal(5, result);
        }

        [Fact]
        public void InvokeVoidTarget()
        {
            var target = new Calculator();
            var avatar = Avatar.Of<ICalculator>();
            var avatarRecorder = new RecordingBehavior();

            // By adding the recorder *after* the dynamic target, 
            // we can check if any calls where made to the avatar
            // instead of the target.
            avatar.AddBehavior(new DynamicTargetBehavior(target))
                 .AddBehavior(avatarRecorder);

            avatar.Store("m1", 42);
            Assert.Equal(42, avatar.Recall("m1"));

            avatar.Clear("m1");
            Assert.Null(avatar.Recall("m1"));
        }
    }
}
