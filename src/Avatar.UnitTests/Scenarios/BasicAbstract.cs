using System;
using Xunit;
using Avatars;

namespace Scenarios.BasicAbstract
{
    public abstract class BaseType
    {
        public abstract void Run();
    }

    /// <summary>
    /// Basic abstract class implementation and behaviors are correct.
    /// </summary>
    public class Test : IRunnable
    {
        public void Run()
        {
            var avatar = Avatar.Of<BaseType>();

            Assert.NotNull(avatar);
            Assert.IsAssignableFrom<IAvatar>(avatar);

            // If no behavior is configured, invoking it throws.
            Assert.Throws<NotImplementedException>(() => avatar.Run());

            // When we add at least one matching behavior, invocations succeed.
            avatar.AddBehavior(new DefaultValueBehavior());
            avatar.Run();

            // The IAvatar interface is properly implemented.
            Assert.Single(((IAvatar)avatar).Behaviors);
        }
    }
}
