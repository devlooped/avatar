#pragma warning disable CS0436
using Xunit;
using Avatars;

namespace Scenarios.OverrideObject
{
    public class BaseType { }

    /// <summary>
    /// Generated code overrides the virtual System.Object base type members, 
    /// so they can be intercepted too.
    /// </summary>
    public class Test : IRunnable
    {
        // Run just this test using the TD.NET ad-hoc runner
        public void RunTest() => new Avatars.UnitTests.Scenarios().Run(ThisAssembly.Constants.Scenarios.OverrideObject);

        public void Run()
        {
            var avatar = Avatar.Of<BaseType>();
            var recorder = new RecordingBehavior();
            avatar.AddBehavior(recorder);

            avatar.GetHashCode();
            Assert.Single(recorder.Invocations);
            Assert.Equal(nameof(GetHashCode), recorder.Invocations[0].Invocation.MethodBase.Name);

            avatar.ToString();
            Assert.Equal(2, recorder.Invocations.Count);
            Assert.Equal(nameof(ToString), recorder.Invocations[1].Invocation.MethodBase.Name);

            avatar.Equals(new object());
            Assert.Equal(3, recorder.Invocations.Count);
            Assert.Equal(nameof(Equals), recorder.Invocations[2].Invocation.MethodBase.Name);
        }
    }
}
