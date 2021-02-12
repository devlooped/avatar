#pragma warning disable IDE0079
#pragma warning disable CS0436
using System;
using Xunit;

namespace Avatars.Scenario.InterfaceBase
{
    public interface IBasicInterface
    {
        void Run();
    }

    /// <summary>
    /// Basic interface implementation and behaviors are correct.
    /// </summary>
    public class Test : IRunnable
    {
        public void Run()
        {
            BehaviorPipelineFactory.LocalDefault = new RecordingBehaviorPipelineFactory();
            var avatar = Avatar.Of<IBasicInterface>();

            Assert.NotNull(avatar);
            Assert.IsAssignableFrom<IAvatar>(avatar);

            // Recorder tracks call to constructor.
            Assert.Single(((IAvatar)avatar).Behaviors);
            Assert.Single(((RecordingBehavior)((IAvatar)avatar).Behaviors[0]).Invocations);

            // If no returning behavior is configured, invoking it throws.
            Assert.Throws<NotImplementedException>(() => avatar.Run());

            // When we add at least one matching behavior, invocations succeed.
            avatar.AddBehavior(new DefaultValueBehavior());
            avatar.Run();
        }

        class RecordingBehaviorPipelineFactory : IBehaviorPipelineFactory
        {
            public BehaviorPipeline CreatePipeline<TAvatar>() => new BehaviorPipeline(new RecordingBehavior());
        }
    }
}
