using System.Threading;

namespace Avatars
{
    /// <summary>
    /// Provides the global <see cref="Default"/> and <see cref="LocalDefault"/> 
    /// behavior pipeline factory used when creating new avatars.
    /// </summary>
    /// <remarks>
    /// Avatars will use <see cref="Default"/>.<see cref="IBehaviorPipelineFactory.CreatePipeline{TAvatar}"/> 
    /// whenever a new avatar is instantiated, to initialize a behavior pipeline that is invoked in the 
    /// constructor itself, even before further configuration can be performed on the created instance. 
    /// This is typically only needed for advanced scenarios.
    /// </remarks>
    public static class BehaviorPipelineFactory
    {
        static readonly AsyncLocal<IBehaviorPipelineFactory?> localFactory = new();
        static IBehaviorPipelineFactory defaultFactory = new DefaultBehaviorPipelineFactory();

        /// <summary>
        /// Gets or sets the global default <see cref="IBehaviorPipelineFactory"/> to use 
        /// to create avatars.
        /// </summary>
        /// <remarks>
        /// A <see cref="LocalDefault"/> can override the value of this global 
        /// default, if assigned to a non-null value.
        /// </remarks>
        public static IBehaviorPipelineFactory Default
        {
            get => localFactory.Value ?? defaultFactory;
            set => defaultFactory = value;
        }

        /// <summary>
        /// Gets or sets the <see cref="IBehaviorPipelineFactory"/> to use 
        /// in the current (async) flow, so it does not affect other threads/flows.
        /// This is typically used in tests to isolate the pipeline configurations.
        /// </summary>
        public static IBehaviorPipelineFactory? LocalDefault
        {
            get => localFactory.Value;
            set => localFactory.Value = value;
        }

        class DefaultBehaviorPipelineFactory : IBehaviorPipelineFactory
        {
            public BehaviorPipeline CreatePipeline<TAvatar>() => new BehaviorPipeline();
        }
    }
}
