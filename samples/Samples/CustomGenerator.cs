using System;
using System.Reflection;
using Avatars;
using Xunit;

namespace Samples
{
    /// <summary>
    /// Showcases how an arbitrary method can be annotated as a "avatar generator" that 
    /// will seamlessly extended default behaviors for new avatars.
    /// </summary>
    public class CustomGenerator
    {
        [Fact]
        public void RandomPing()
        {
            var ping = Randomizer.Of<IPing>();

            // Each execution results in a new random value.
            Assert.NotEqual(ping.Ping(), ping.Ping());
            Assert.NotEqual(ping.Ping(), ping.Ping());
        }
    }

    public interface IPing
    {
        int Ping();
    }

    public static class Randomizer
    {
        static readonly Random random = new Random();

        [AvatarGenerator]
        public static T Of<T>()
            => Avatar.Of<T>().AddBehavior(
                (invocation, next) => invocation.CreateValueReturn(random.Next()),
                invocation => invocation.MethodBase is MethodInfo info && info.ReturnType == typeof(int));
    }
}
