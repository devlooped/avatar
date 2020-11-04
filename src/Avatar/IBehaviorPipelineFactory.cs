namespace Avatars
{
    /// <summary>
    /// Creates and optionally initializes the <see cref="BehaviorPipeline"/> 
    /// for every avatar instantiation.
    /// </summary>
    public interface IBehaviorPipelineFactory
    {
        /// <summary>
        /// Creates the pipeline for avatars of type <typeparamref name="TAvatar"/>.
        /// </summary>
        /// <typeparam name="TAvatar">The type of avatar being instantiated.</typeparam>
        /// <returns>The configured pipeline to use.</returns>
        BehaviorPipeline CreatePipeline<TAvatar>();
    }
}
